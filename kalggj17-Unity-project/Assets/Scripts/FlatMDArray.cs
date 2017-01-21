using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class FlatMDArray< T > {

   public static FlatMDArray< T > Create( params int[] dimensions ) {

      switch (dimensions.Length) {
         
         case 2:  { return new Flat2DArray< T >( dimensions[ 0 ], dimensions[ 1 ] ); }
         case 3:  { return new Flat3DArray< T >( dimensions[ 0 ], dimensions[ 1 ], dimensions[ 2 ] ); }
         default: { return new FlatMDArray< T >( dimensions ); }
      }
   }

   public T[]    flat             { get { return m_array;            } }
   public int[]  dimensions       { get { return m_dimensions;       } }
   public string dimensionsString { get { return m_dimensionsString; } }

   protected T[]    m_array;
   protected int[]  m_dimensions;
   protected string m_dimensionsString;

   public FlatMDArray( params int[] dimensions ) {

      m_dimensions = dimensions;
      int length   = 1;
      foreach (int d in dimensions) { length *= d; }
      m_array            = new T[ length ];
      m_dimensionsString = "[" + string.Join( ", ", dimensions.Select( d => d.ToString() ).ToArray() ) + "]";
   }

   public virtual int FlatIndex( params int[] indices ) {

      if (indices.Length != m_dimensions.Length) {

         throw new System.ArgumentException( "Expected " + m_dimensions.Length + " indices; only "
                                                + indices.Length + " were specified." );
      }
      int index = 0;

      for (int i = 0; i < indices.Length; i++) {

         int multiplier = 1;

         if (indices[ i ] < 0 || indices[ i ] >= m_dimensions[ i ]) {

            throw new System.ArgumentException( "Index #" + i + ": " + indices[ i ]
                                                   + " is out of bounds. Dimensions: " + m_dimensionsString );
         }
         for (int j = i - 1; j >= 0; j--) { multiplier *= m_dimensions[ j ]; }

         index += indices[ i ] * multiplier;
      }
      return index;
   }

   // this should not be used - exists to be overridden in Flat2DArray
   public virtual int FlatIndex( int x, int y ) {

      return FlatIndex( new[] { x, y } );
   }
   
   // this should not be used - exists to be overridden in Flat2DArray
   public virtual int FlatIndex( int x, int y, int z ) {

      return FlatIndex( new[] { x, y, z } );
   }

   public T this[ params int[] indices ] {
      get { return m_array[ FlatIndex( indices ) ];  }
      set { m_array[ FlatIndex( indices ) ] = value; }
   }
}

class Flat2DArray< T >: FlatMDArray< T > {

   public int width  { get { return m_width;  } }
   public int height { get { return m_height; } }

   protected int m_width, m_height;

   public Flat2DArray( int width, int height ): base( width, height ) {

      m_width  = width;
      m_height = height;
   }

   public override int FlatIndex( int x, int y ) {

      if (x < 0 || x > m_width || y < 0 || y > m_height) {

         throw new System.ArgumentException( "Index (x: " + x + ", y: " + y
                                                + ") out of bounds. Dimensions: " + m_dimensionsString );
      }
      return x + y * m_width;
   }
}

class Flat3DArray< T >: FlatMDArray< T > {

   public int width  { get { return m_width;  } }
   public int height { get { return m_height; } }
   public int depth  { get { return m_depth;  } }

   protected int m_width, m_height, m_depth, zMultiple;

   public Flat3DArray( int width, int height, int depth ): base( width, height, depth ) {

      m_width   = width;
      m_height  = height;
      m_depth   = depth;
      zMultiple = width * height;
   }

   public override int FlatIndex( int x, int y, int z ) {

      if (x < 0 || x >= m_width || y < 0 || y >= m_height || z < 0 || z >= m_depth ) {

         throw new System.ArgumentException( "Index (x: " + x + ", y: " + y + ", z: " + z
                                                + ") out of bounds. Dimensions: " + m_dimensionsString );
      }
      return x + y * m_width + z * zMultiple;
   }
}