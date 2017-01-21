using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RippleInput: MonoBehaviour {

   class Ripple {

      public Vector2 pos;
      public float   startTime;
      public float   amplitude, pitch;
   }

   public float minAmplitude, heightAtMinAmplitude, midAmplitude, heightAtMidAmplitude;

   internal float amplitudeInput, pitchInput;

   Ripple        currentRipple;
   List< float > recentAmplitudes = new List< float >();
   List< float > recentPitches    = new List< float >();
   
   void Update() {

      
   }
}
