using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityController: MonoBehaviour {

   public static EntityController instance;

   public float                 growTime, dieTime;
   public Entity[]              entityPrefabs;
   public static Flat2DArray< Entity > entityInstances;
   public AnimationCurve        growCurve, dieCurve;

   bool            _spawnTrees = true;
   public bool      spawnTrees {
      get { return _spawnTrees; }
      set {
         if (!value) { Clear(); }
         _spawnTrees = value;
      }
   }

   void Awake() {

      instance = this;
   }

   public void Clear() {
      
      for (int x = 0; x < WaveTerrain.instance.size; x++) {
         for (int z = 0; z < WaveTerrain.instance.size; z++) {
            if (entityInstances[ x, z ]) {
               entityInstances[ x, z ].Die();
            }
         }
      }
   }
   
   void Update() {
      
      if (!spawnTrees) { return; }

      for (int x = 0; x < WaveTerrain.instance.size; x++) {
         for (int z = 0; z < WaveTerrain.instance.size; z++) {
            float height     = WaveTerrain.instance.heights[ x, z ];
            float lastHeight = height + WaveTerrain.instance.heightDeltas[ x, z ];
            if (!entityInstances[ x, z ]) {
               // check possibility of making an entity
               foreach (Entity entity in entityPrefabs) {

                  float lastChance  = entity.chancePerAltitude.Evaluate( lastHeight ) * entity.chanceMultiplier;
                  float currChance  = entity.chancePerAltitude.Evaluate( height ) * entity.chanceMultiplier;
                  if (currChance < lastChance) { continue; }
                  float frameChance = (currChance - lastChance) / (1.0f - lastChance);

                  if (Random.value < frameChance) {

                     entityInstances[ x, z ] = Instantiate( entity, new Vector3( x - WaveTerrain.instance.centreOffset + Random.Range( -0.25f, 0.25f ),
                                                 height, z - WaveTerrain.instance.centreOffset + Random.Range( -0.25f, 0.25f ) ), Quaternion.Euler( 0.0f, Random.Range( 0.0f, 360.0f ), 0.0f ) );
                     break;
                  }
               }
            } else {
               entityInstances[ x, z ].transform.position = WaveTerrain.instance.PositionForCoord( new Vector2( x, z ) );
               if (entityInstances[ x, z ].dying) { continue; }
               // check possibility of killing the current one, or just move it to the right height if necessary
               float lastChance  = entityInstances[ x, z ].chancePerAltitude.Evaluate( lastHeight ) * entityInstances[ x, z ].chanceMultiplier;
               float currChance  = entityInstances[ x, z ].chancePerAltitude.Evaluate( height ) * entityInstances[ x, z ].chanceMultiplier;
               float frameChance = (lastChance - currChance) / lastChance;
               if (currChance < 0.01f) { frameChance = 1.0f; }

               if (Random.value < frameChance) {

                  entityInstances[ x, z ].Die();
                  continue;
               }
            }
         }
      }
   }
}
