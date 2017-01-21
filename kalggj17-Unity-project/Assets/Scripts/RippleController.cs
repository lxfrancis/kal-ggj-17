using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ripple {

   public Vector2 pos;
   public float   startTime;
   public float   height, width;
   public int     trailNum = -1;

   public bool live { get { return Time.time - startTime < width / RippleController.instance.speed; } }
   public bool visible {
      get {
         return (Time.time - startTime) * RippleController.instance.speed - width
                   < WaveTerrain.instance.size * Mathf.Sqrt( 2 );
      }
   }
}

public class RippleController: MonoBehaviour {

   public static RippleController instance;

   public float minAmplitude, heightAtMinAmplitude, midAmplitude, heightAtMidAmplitude,
                midPitch, widthAtMidPitch, pitchScale, speed, trailProportion;
   public int   trailOutRipples;

   internal float          amplitudeInput, pitchInput;
   internal List< Ripple > ripples = new List<Ripple>();

   bool          inputActive;
   Ripple        currentRipple;
   int           currentTrailNum;
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
   }

   void MakeNewRipple() {
      
      currentRipple           = new Ripple();
      currentRipple.pos       = lastInputPoint;
      currentRipple.startTime = Time.time;
      currentRipple.height    = AmplitudeToHeight( amplitudeInput );
      currentRipple.width     = PitchToWidth( pitchInput );
      recentAmplitudes.Clear();
      recentPitches.Clear();
      recentAmplitudes.Add( amplitudeInput );
      recentPitches.Add( pitchInput );
      currentTrailNum = 0;
      ripples.Add( currentRipple );
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

      foreach (Ripple ripple in ripples.ToArray()) {

         if (!ripple.visible) {

            ripples.Remove( ripple );
            continue;
         }
         if (!ripple.live && ripple.trailNum >= 0 && ripple.trailNum < trailOutRipples) {
            MakeNewTrailRipple( ripple );
         }
      }

      if (Physics.Raycast( Camera.main.ScreenPointToRay( Input.mousePosition ), out hit, Mathf.Infinity,
                           LayerMask.NameToLayer( "cursorPlane" ) )) {

         lastInputPoint = new Vector2( hit.point.x, hit.point.z );
      }

      if (!inputActive) {

         if (amplitudeInput > minAmplitude) {

            inputActive = true;
            MakeNewRipple();
         }
      }
      else {

         if (inputActive && amplitudeInput < minAmplitude) {

            inputActive = false;
            if (currentRipple != null) { currentRipple.trailNum = 0; }  // make new trails
         }
         else {
            if (currentRipple != null && currentRipple.live) {

               recentAmplitudes.Add( amplitudeInput );
               recentPitches.Add( pitchInput );

               currentRipple.height = AmplitudeToHeight( recentAmplitudes.Average() );
               currentRipple.width  = PitchToWidth( recentPitches.Average() );
            }
            else {
               MakeNewRipple();
            }
         }
      }
   }
}
