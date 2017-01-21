using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundInput: MonoBehaviour {

   public float mouseAmplitude, mousePitch;
   public bool  mouseMode;
   
   void Update() {

      if (mouseMode) {
         RippleController.instance.amplitudeInput = Input.GetMouseButton( 0 ) ? mouseAmplitude : 0.0f;
         RippleController.instance.pitchInput     = mousePitch;
      }
   }
}
