using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundInput: MonoBehaviour {

   public float mouseAmplitude, mousePitch;
   public bool  mouseMode, lockPitch;

   public PitchTracker pitchTracker;

   void Awake() {

      pitchTracker = FindObjectOfType< PitchTracker >();
   }
   
   void Update() {

      if (mouseMode) {

         RippleController.instance.amplitudeInput = Input.GetMouseButton( 0 ) ? mouseAmplitude : 0.0f;
         RippleController.instance.pitchInput     = mousePitch;
      }
      else {

         RippleController.instance.amplitudeInput = pitchTracker.singValue;
         if (lockPitch) { RippleController.instance.pitchInput = mousePitch; }
      }
   }
}
