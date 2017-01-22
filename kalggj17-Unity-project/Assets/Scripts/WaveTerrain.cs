using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaveTerrain: MonoBehaviour {

   public static WaveTerrain instance;

   public int       size;
   public float     baseHeight, bottomHeight, minAltitude, maxAltitude, maxSlopeAngle, noiseLevel = 0.5f;
   public bool      flatShaded = true, lerpColors = true, updateVerts = true;
   public Transform cursorPlane;
   public Gradient  flatColors, slopeColors;

   internal Flat2DArray< float > heights, heightNoise, heightDeltas;
   internal float                centreOffset;
   
   Mesh                 mesh;
   Vector3[]            verts;
   Flat2DArray< Color > colorMap;
   Color[]              meshColors;

   void Awake() {

      instance = this;
   }

   public Vector3 PositionForCoord( Vector2 coord ) {

      var result = new Vector3( coord.x - centreOffset, heights[ Mathf.Clamp( Mathf.RoundToInt( coord.x ), 0, size - 1 ), Mathf.Clamp( Mathf.RoundToInt( coord.y ), 0, size - 1 ) ], coord.y - centreOffset );
      //Debug.Log( "Position for coord: " + coord + " - " + result );
      return result;
   }

   void AddTrianglePair( int a, int b, int c, int d, List< int > tris, string info ) {

      tris.Add( a );
      tris.Add( c );
      tris.Add( d );
      tris.Add( a );
      tris.Add( d );
      tris.Add( b );
      if (new[] { a, b, c, d }.Any( index => index == 10396 )) {
         Debug.Log( "Set tri index 10396! a: " + a + ", b: " + b + ", c: " + c + ", d: " + d + "; " + info );
      }
   }

   void UpdateMesh() {

      var normals = mesh.normals;
      
      if (updateVerts) {
      // top surface
      for (int x = 0; x < size; x++) {
         for (int z = 0; z < size; z++) {
            verts[ x + z * size ].y    = heights[ x, z ];
            if (lerpColors) {
               float slope      = Vector3.Angle( normals[ x + z * size ], Vector3.up );
               //Color flatColor  = flatColors.Evaluate( Mathf.InverseLerp( minAltitude, maxAltitude, heights[ x, z ] ) );
               //Color slopeColor = slopeColors.Evaluate( Mathf.InverseLerp( minAltitude, maxAltitude, heights[ x, z ] ) );
               Color flatColor  = Color.white;
               Color slopeColor = Color.black;
               meshColors[ x + z * size ] = Color.Lerp( flatColor, slopeColor, Mathf.InverseLerp( 0.0f, maxSlopeAngle, slope ) );
                  meshColors[ x + z * size ].g = Random.value;
            }
         }
      }
      }

      int topEdgeOffset = size * size;

      // top edges
      for (int i = 0; i < size; i++) {
         verts[ topEdgeOffset + i + 0 * size ].y = heights[ 0, i ];
         verts[ topEdgeOffset + i + 1 * size ].y = heights[ i, size - 1 ];
         verts[ topEdgeOffset + i + 2 * size ].y = heights[ size - 1, size - 1 - i ];
         verts[ topEdgeOffset + i + 3 * size ].y = heights[ size - 1 - i, 0 ];
      }

      if (updateVerts) {
         mesh.vertices = verts;
      }
      mesh.colors = meshColors;
      mesh.RecalculateNormals();

   }
   
   void Start() {
      
      heights              = new Flat2DArray< float >( size, size );
      heightDeltas         = new Flat2DArray< float >( size, size );
      heightNoise          = new Flat2DArray< float >( size, size );
      verts                = new Vector3[ size * size + 8 * size ];
      meshColors           = new Color[ verts.Length ];
      colorMap             = new Flat2DArray< Color >( size, size );
      centreOffset         = (size - 1) * 0.5f;
      mesh                 = new Mesh();
      var tris             = new List< int >();
      int topEdgeOffset    = size * size;
      int bottomEdgeOffset = size * size + size * 4;
      
      EntityController.entityInstances = new Flat2DArray< Entity >( size, size );

      Camera.main.transform.position = Camera.main.transform.position * (size / 150.0f);

      cursorPlane.position   = Vector3.up  * baseHeight;
      cursorPlane.localScale = Vector3.one * size * 0.1f;

      // top surface
      for (int x = 0; x < size; x++) {
         for (int z = 0; z < size; z++) {
            heightNoise[ x, z ] = Random.value;
            heights[ x, z ]       = baseHeight + heightNoise[ x, z ] * noiseLevel;
            verts[ x + z * size ] = new Vector3( x - centreOffset, baseHeight, z - centreOffset );
         }
      }

      // top edges
      for (int i = 0; i < size; i++) {
         verts[ topEdgeOffset + i + 0 * size ] = new Vector3( -centreOffset,   baseHeight, -centreOffset+i );
         verts[ topEdgeOffset + i + 1 * size ] = new Vector3( -centreOffset+i, baseHeight,  centreOffset   );
         verts[ topEdgeOffset + i + 2 * size ] = new Vector3(  centreOffset,   baseHeight,  centreOffset-i );
         verts[ topEdgeOffset + i + 3 * size ] = new Vector3(  centreOffset-i, baseHeight, -centreOffset   );
      }
      
      // bottom edges
      for (int i = 0; i < size; i++) {
         verts[ bottomEdgeOffset + i + 0 * size ] = new Vector3( -centreOffset,   bottomHeight, -centreOffset+i );
         verts[ bottomEdgeOffset + i + 1 * size ] = new Vector3( -centreOffset+i, bottomHeight,  centreOffset   );
         verts[ bottomEdgeOffset + i + 2 * size ] = new Vector3(  centreOffset,   bottomHeight,  centreOffset-i );
         verts[ bottomEdgeOffset + i + 3 * size ] = new Vector3(  centreOffset-i, bottomHeight, -centreOffset   );
      }

      // top surface triangles
      for (int x = 0; x < size - 1; x++) {
         for (int z = 0; z < size - 1; z++) {
            AddTrianglePair( x + z * size, x + 1 + z * size, x + (z + 1) * size, x + 1 + (z + 1) * size, tris,
                             "surface triangle x: " + x + ", z: " + z );
         }
      }

      // side triangles
      for (int i = 0; i < size - 1; i++) { 
         // left side
         AddTrianglePair( bottomEdgeOffset + 0 * size + 1 + i, bottomEdgeOffset + 0 * size + i,
                          topEdgeOffset    + 0 * size + 1 + i, topEdgeOffset    + 0 * size + i,
                          tris, "left edge triangle " + i );
         // back side
         AddTrianglePair( bottomEdgeOffset + 1 * size + 1 + i, bottomEdgeOffset + 1 * size + i,
                          topEdgeOffset    + 1 * size + 1 + i, topEdgeOffset    + 1 * size + i,
                          tris, "back edge triangle " + i );
         // right side
         AddTrianglePair( bottomEdgeOffset + 2 * size + 1 + i, bottomEdgeOffset + 2 * size + i,
                          topEdgeOffset    + 2 * size + 1 + i, topEdgeOffset    + 2 * size + i,
                          tris, "right edge triangle " + i );
         // front side
         AddTrianglePair( bottomEdgeOffset + 3 * size + 1 + i, bottomEdgeOffset + 3 * size + i,
                          topEdgeOffset    + 3 * size + 1 + i, topEdgeOffset    + 3 * size + i,
                          tris, "front edge triangle " + i );
      }

      mesh.vertices  = verts;
      Debug.Log( "vert count: " + verts.Length + "; max tri index: " + tris.Max() );
      mesh.triangles = tris.ToArray();
      mesh.RecalculateNormals();
      GetComponent< MeshFilter >().mesh = mesh;
   }

   float curveEvaluateTime;
   float totalTime;
   
   void Update() {

      for (int x = 0; x < size - 1; x++) {
         for (int z = 0; z < size - 1; z++) {
            //heights[ x, z ] = baseHeight + 1 * (Mathf.Sin( Time.time + x * 0.7f ) + Mathf.Sin( Time.time * 0.5f + z * 0.4f )) + 1.5f * Mathf.Sin( new Vector2( x - centreOffset, z - centreOffset ).magnitude - Time.time * 1.7f );
            float height = baseHeight + heightNoise[ x, z ] * noiseLevel;

            foreach (Ripple ripple in RippleController.instance.ripples) {

               float distance     = (new Vector2( x - centreOffset, z - centreOffset ) - ripple.pos).magnitude;
               float rippleRadius = (Time.time - ripple.startTime) * RippleController.instance.speed;

               if (RippleController.instance.useCurves) {

                  float st = Time.realtimeSinceStartup;
                  float t = (rippleRadius - distance) / RippleController.instance.speed;

                  if (t > 0 && t < ripple.curve.keys.Last().time) {
                     height += ripple.curve.Evaluate( Mathf.Pow( t, 1.1f ) );
                  }
                  curveEvaluateTime += Time.realtimeSinceStartup - st;
                  totalTime          = Time.realtimeSinceStartup;
               }
               else {
                  float pointWithinRipple = (rippleRadius - distance) / ripple.width;

                  if (pointWithinRipple >= 0.0f && pointWithinRipple <= 1.0f) {
                     height += Mathf.Sin( pointWithinRipple * Mathf.PI * 2.0f ) * ripple.height;
                  }
               }
            }
            heightDeltas[ x, z ] = height - heights[ x, z ];
            heights[ x, z ]      = height;
            //colorMap[ x, z ] = flatColors.Evaluate( Mathf.InverseLerp( minAltitude, maxAltitude, height ) );
         }
      }

      UpdateMesh();
   }
}
