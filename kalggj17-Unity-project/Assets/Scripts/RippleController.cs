using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ripple {

   public Vector2        pos;
   public float          startTime;
   public float          height, width;
   public int            trailNum = -1;
   public AnimationCurve curve;
   public float          lastKeyframeTime;
   public bool           down = false;

   public bool live {
      get {
         if (RippleController.instance.useCurves) {
            //return true;
            return Time.time - startTime < curve.keys.Last().time + width;
         }
         return Time.time - startTime < width / RippleController.instance.speed;
      }
   }
   public bool visible {
      get {
         if (RippleController.instance.useCurves) {
            //return true;
            return Time.time - startTime < curve.keys.Last().time + (WaveTerrain.instance.size * Mathf.Sqrt( 2 )) / RippleController.instance.speed;
         }
         return (Time.time - startTime) * RippleController.instance.speed - width
                   < WaveTerrain.instance.size * Mathf.Sqrt( 2 );
      }
   }
}

public class RippleController: MonoBehaviour {

   public static RippleController instance;

   public float          minAmplitude, heightAtMinAmplitude, midAmplitude, heightAtMidAmplitude,
                         midPitch, widthAtMidPitch, pitchScale, speed, trailProportion, keyframeInterval,
                         trailOffDistance, speedDamping, heightGrowthDuration, rippleSplitTimeGap = 0.2f;
   public int            trailOutRipples, maxRipples;
   public bool           useCurves, useDownPin;
   public Transform      upPin, downPin;
   public AnimationCurve growthCurve;

   internal float          amplitudeInput, pitchInput;
   internal List< Ripple > ripples = new List<Ripple>();
   internal int numRipples;
   internal AnimationCurve upCurve;

   bool          inputActive;
   Ripple        currentRipple, currentDownRipple;
   int           currentTrailNum;
   float         lastInputTime;
   List< float > recentAmplitudes = new List< float >();
   List< float > recentPitches    = new List< float >();
   Vector2       lastInputPoint;

   float PitchToWidth( float pitch ) {

      return ((pitchScale * (pitch - midPitch) + midPitch) / midPitch) * widthAtMidPitch;
   }
   
   float AmplitudeToHeight( float amplitude ) {

      float t = Mathf.InverseLerp( minAmplitude, midAmplitude, amplitude );
      return Mathf.LerpUnclamped( heightAtMinAmplitude, heightAtMidAmplitude, t );
   }

   void Awake() {

      instance = this;
      if (!useDownPin) { downPin.gameObject.SetActive( false ); }
   }

   void MakeNewRipple( bool down=false ) {
      
      Debug.Log( "new ripple, down: " + down );
      var ripple = new Ripple();
      ripple.pos = lastInputPoint;
      if (useCurves) { ripple.pos = down ? downPin.position.xz() : upPin.position.xz(); }
      ripple.startTime = Time.time;
      ripple.height    = AmplitudeToHeight( amplitudeInput );
      ripple.width     = PitchToWidth( pitchInput );
      ripple.down      = down;
      recentAmplitudes.Clear();
      recentPitches.Clear();
      recentAmplitudes.Add( amplitudeInput );
      recentPitches.Add( pitchInput );
      if (down) { currentDownRipple = ripple; }
      else { currentRipple = ripple; }
      currentTrailNum = 0;

      if (useCurves) {

         ripple.curve            = new AnimationCurve( new Keyframe( 0.0f, ripple.height * growthCurve.Evaluate( 0.0f ) ) );
         ripple.lastKeyframeTime = Time.time;
         if (!down) { upCurve = ripple.curve; }
      }
      ripples.Add( ripple );
      if (ripples.Count > maxRipples) { ripples.RemoveAt( 0 ); }
   }

   void MakeNewTrailRipple( Ripple parent ) {
      
      Ripple newTrailRipple    = new Ripple();
      newTrailRipple.pos       = parent.pos;
      newTrailRipple.startTime = Time.time;
      newTrailRipple.height    = parent.height * trailProportion;
      newTrailRipple.width     = parent.width;
      newTrailRipple.trailNum  = parent.trailNum + 1;
      parent.trailNum          = -1;
      ripples.Add( newTrailRipple );
   }
   
   void Update() {
      
      RaycastHit hit;

      recentAmplitudes.Add( amplitudeInput );
      recentPitches.Add( pitchInput );

      if (amplitudeInput > minAmplitude) { lastInputTime = Time.time; }

      foreach (Ripple ripple in ripples.ToArray()) {

         if (!ripple.visible) {

            ripples.Remove( ripple );
            Debug.Log( "removed a ripple" );
            continue;
         }
         if (!ripple.live && ripple.trailNum >= 0 && ripple.trailNum < trailOutRipples) {
            MakeNewTrailRipple( ripple );
         }
      }

      if (Physics.Raycast( Camera.main.ScreenPointToRay( Input.mousePosition ), out hit, Mathf.Infinity,
                           LayerMask.NameToLayer( "cursorPlane" ) )) {

         lastInputPoint = new Vector2( hit.point.x, hit.point.z );
         if (Input.GetMouseButtonDown( 0 )) {
            upPin.position= WaveTerrain.instance.PositionForCoord( lastInputPoint ).ZeroY();
            currentRipple = null;
         }
         if (Input.GetMouseButtonDown( 1 )) { downPin.position = lastInputPoint; }
      }

      if (!inputActive) {

         if (amplitudeInput > minAmplitude) {

            inputActive   = true;
            MakeNewRipple();
            if (useCurves && useDownPin) { MakeNewRipple( true ); }
         }
      }
      else {

         if (inputActive && amplitudeInput < minAmplitude && Time.time > lastInputTime + rippleSplitTimeGap) {

            inputActive = false;

            if (useCurves) {

               currentRipple     = null;
               currentDownRipple = null;
            }
            else if (currentRipple != null) { currentRipple.trailNum = 0; }  // make new trails
         }
         else {
            if (currentRipple != null && currentRipple.live) {

               if (useCurves) {
                  if (Time.time > currentRipple.lastKeyframeTime + keyframeInterval) {
                     currentRipple.lastKeyframeTime = Time.time;
                     float multiplier = growthCurve.Evaluate( (Time.time - currentRipple.startTime) / heightGrowthDuration );
                     currentRipple.curve.AddKey( Time.time - currentRipple.startTime, AmplitudeToHeight( recentAmplitudes.Average() ) * multiplier );
                     if (useDownPin) {
                        currentDownRipple.lastKeyframeTime = Time.time;
                        currentDownRipple.curve.AddKey( Time.time - currentDownRipple.startTime, -AmplitudeToHeight( recentAmplitudes.Average() ) );
                     }
                     recentAmplitudes.Clear();
                     recentPitches.Clear();
                     //Debug.Log( "keys: " + (Time.time - currentDownRipple.startTime) + " - " + AmplitudeToHeight( amplitudeInput ) + ", " +  -AmplitudeToHeight( amplitudeInput ) );
                     //Debug.Log( Utils.PrintVals( currentRipple.curve.keys ) );
                  }
               }
               else {
                  currentRipple.height = AmplitudeToHeight( recentAmplitudes.Average() );
                  currentRipple.width  = PitchToWidth( recentPitches.Average() );
               }
            }
            else {
               MakeNewRipple();
               if (useCurves && useDownPin) { MakeNewRipple( true ); }
            }
         }
      }
      numRipples = ripples.Count;
   }
}
