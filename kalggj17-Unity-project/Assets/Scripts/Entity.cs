using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity: MonoBehaviour {

   public AnimationCurve chancePerAltitude;
   public float          chanceMultiplier;
   public float          minSize, maxSize;

   internal float lastHeight;
   internal bool  spawning, dying;

   Transform tf;
   float     animStartTime, targetSize;

   void Awake() {

      tf = transform;
   }

   void Start() {
      
      spawning      = true;
      animStartTime = Time.time;
      targetSize    = Random.Range( minSize, maxSize );
      tf.localScale = Vector3.zero;
   }

   public void Die() {

      spawning      = false;
      dying         = true;
      animStartTime = Time.time;
   }
   
   void Update() {

      lastHeight = tf.position.y;

      if (spawning) {

         float t = Mathf.Clamp01( (Time.time - animStartTime) / EntityController.instance.growTime );
         tf.localScale = Vector3.one * EntityController.instance.growCurve.Evaluate( t ) * targetSize;
         if (t == 1.0f) { spawning = false; }
      }
      if (dying) {

         float t = Mathf.Clamp01( (Time.time - animStartTime) / EntityController.instance.dieTime );
         tf.localScale = Vector3.one * EntityController.instance.dieCurve.Evaluate( t ) * targetSize;
         if (t >= 0.99f) { Destroy( gameObject ); }
      }
   }
}
