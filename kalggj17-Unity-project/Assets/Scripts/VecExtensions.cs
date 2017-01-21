// Last modified: 2016-02-16 - added ContingentEvaluation

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

  public interface IHasFrequency {

    float frequency { get; set; }
  }
   
  public class DuplicateKeyComparer< TKey >: IComparer< TKey > where TKey: System.IComparable {

  bool descending = false;

  public DuplicateKeyComparer( bool descending=false ) {

    this.descending = descending;
  }

    public int Compare( TKey x, TKey y ) {

      int result = descending ? y.CompareTo( x ) : x.CompareTo( y );
      return result == 0 ? 1 : result;
    }
  }

  class DescendingComparer< T >: IComparer< T > {

    public int Compare( T x, T y ) { return Comparer< T >.Default.Compare( y, x ); }
  }

  public class CappedQueue< T >: Queue< T > {

    int       _maxCount;
    public int MaxCount {
        get { return _maxCount; }
        set {
          _maxCount = value;
          Trim();
        }
    }

    public CappedQueue( int maxCount ): base() { _maxCount = maxCount; }
      
    public new void Enqueue( T item ) {

        base.Enqueue( item );
        Trim();
    }

    void Trim() { while (Count > _maxCount) { Dequeue(); } }
  }

public class ListSet< T >: List< T > {

  bool               addAtEnd;
  Func< T, T, bool > tolerance;
    
  public ListSet( bool addAtEnd=false, Func< T, T, bool > toleranceFunc=null ): base() {

    this.addAtEnd = addAtEnd;
    tolerance     = toleranceFunc;
  }
    
  public ListSet( IEnumerable< T > collection, bool addAtEnd=false,
                  Func< T, T, bool > toleranceFunc=null ): base( collection.Count() ) {

    this.addAtEnd = addAtEnd;
    tolerance     = toleranceFunc;

    foreach (T item in collection) { Add( item ); }
  }
    
  public new void Add( T item ) {

    if (!Contains( item )) {
      if (tolerance == null || Count == 0 || tolerance( this.Last(), item )) { base.Add( item ); }
    }
    else if (addAtEnd) {

      Remove( item );
      base.Add( item );
    }
  }
    
  public new void AddRange( IEnumerable< T > items ) { foreach (T item in items) { Add( item ); } }
}

  public class TimedList< T >: SortedList< float, T > {

    bool          interruptThreads;
    float         lastModifiedTime;

    List< float > intervals;
    float         lastIntervalsRetrievalTime;

    float       _duration;
    public float Duration {
        get { return _duration; }
        set {
          _duration = value;
          Trim();
        }
    }

    public TimedList( float d, bool i=false ): base( new DuplicateKeyComparer< float >() ) {
         
        _duration        = d;
        interruptThreads = i && typeof( T ) == typeof( System.Threading.Thread );
        lastModifiedTime = -1.0f;
    }

    public void Add( T item ) {

        base.Add( Time.time, item );
        lastModifiedTime = Time.time;
        Trim();
    }

    public new void Add( float time, T item ) {

        base.Add( time, item );
        lastModifiedTime = Time.time;
        Trim();
    }

    public void Trim() {

        while (this.Any() && this.First().Key < Time.time - _duration) {

          if (interruptThreads) { ((System.Threading.Thread) (object) this.First().Value).Interrupt(); }
          RemoveAt( 0 );
          lastModifiedTime = Time.time;
        }
    }

    public List< float > Intervals {
        get {
          Trim();
          if (intervals != null && lastModifiedTime == lastIntervalsRetrievalTime) { return intervals; }

          intervals                  = new List< float >();
          lastIntervalsRetrievalTime = lastModifiedTime;
          float lastKey              = -1.0f;

          foreach (float time in Keys) {
              if (lastKey == -1.0f) { lastKey = time; }
              else {
                if (time != lastKey) { intervals.Add( time - lastKey ); }
                lastKey = time;
              }
          }
          return intervals;
        }
    }
  }

public class RotationDamper {

  Transform               tf;
  float                   time;
  TimedList< Quaternion > recent;

  public RotationDamper( Transform transform, float dampTime ) {

    tf     = transform;
    time   = dampTime;
    recent = new TimedList< Quaternion >( dampTime );
  }

  public void Target( Quaternion quaternion ) {

    recent.Add( quaternion );
    tf.rotation = recent.Values.Average();
  }

  public void Reset() {

    recent.Clear();
  }
}

/// <summary>
/// A dictionary with string keys that ignore whitespace and letter case.</summary>
public class GBDictionary< T >: Dictionary< string, T >, IDictionary< string, T > {
    
  public new T this[ string key ] {
    get { return base[ key.ToKey() ]; }
    set { base[ key.ToKey() ] = value; }
  }
    
  public new void Add( string key, T value ) { base.Add( key.ToKey(), value ); }
  public new void Remove( string key ) { base.Remove( key.ToKey() ); }
  public new bool ContainsKey( string key ) { return base.ContainsKey( key.ToKey() ); }

  public GBDictionary(): base() { }

  public GBDictionary( IDictionary< string, T > dict ) {
    foreach (var entry in dict.Keys) { this[ entry ] = dict[ entry ]; }
  }
}

public class CountTable: Dictionary< string, int > {
    
  public new int this[ string key ] {
    get {
      if (!ContainsKey( key )) { this[ key ] = 0; }
      return base[ key ];
    }
    set {
      base[ key ] = value;
    }
  }

  public override string ToString() { return ToString( false ); }

  public string ToString( bool bold=false ) {

    return string.Join( "\n", this.Select( kvp => (bold ? "<b>" : "") + kvp.Key + ": "
                                                    + (bold ? "</b>" : "") + kvp.Value ).ToArray() );
  }

  public int total { get { return Values.Sum(); } }
}

/// <summary>
/// Caches the result of a calculation, and returns new values only after the period has elapsed.</summary>
public class PeriodicEvaluation< T > {

  public float period;

  Func< T > calculation;
  float     last_evaluation_time;
  T         last_value;
  
  /// <summary>
  /// Construct a new PeriodicEvaluation with the given calculation and period.</summary>
  public PeriodicEvaluation( Func< T > calculation, float period=0.0f ) {

    this.calculation = calculation;
    this.period      = period;
  }

  /// <summary>
  /// Calculates, caches and returns a new value if the period has elapsed, otherwise returns the cached value.</summary>
  public T Value {
    get {
      if (Time.time != last_evaluation_time && Time.time >= last_evaluation_time + period) {

        last_evaluation_time = Time.time;
        last_value           = calculation();
      }
      return last_value;
    }
  }
  
  public void SetToReevaluate() {
    last_evaluation_time = Time.time - period * 2.0f;
  }

  public static implicit operator T( PeriodicEvaluation< T > evaluation ) { return evaluation.Value; }
}

/// <summary>
/// Caches the result of a calculation, and returns new values only after the condition has changed.</summary>
public class ContingentEvaluation< T > {

  public Func< object > condition;

  Func< T > calculation;
  object    last_condition_evaluation;
  T         last_value;
  bool      re_evaluate;
  
  /// <summary>
  /// Construct a new ContingentEvaluation with the given calculation and condition.</summary>
  public ContingentEvaluation( Func< T > calculation, Func< object > condition ) {

    this.calculation = calculation;
    this.condition   = condition;
  }

  /// <summary>
  /// Calculates, caches and returns a new value if the condition has changed, otherwise returns the cached value.
  /// </summary>
  public T Value {
    get {
      object eval  = condition();

      bool   equal = ((last_condition_evaluation != null) && last_condition_evaluation.GetType().IsValueType)
                       ? last_condition_evaluation.Equals( eval )
                       : (last_condition_evaluation == eval);

      if (re_evaluate || !equal) {

        last_condition_evaluation = eval;
        last_value                = calculation();
        re_evaluate               = false;
      }
      return last_value;
    }
  }
  
  public void SetToReevaluate() {
    re_evaluate = true;
  }

  public static implicit operator T( ContingentEvaluation< T > evaluation ) { return evaluation.Value; }
}

[Serializable]
public class SerializedNullable< T > where T: struct {
  
  [SerializeField] T    value;
  [SerializeField] bool hasValue;

  public bool HasValue { get { return hasValue; } }

  public T Value {
    get {
       if (hasValue) { return value; }
       throw new InvalidOperationException();
    }
  }

  public T BackingValue { get { return value; } }

  public SerializedNullable() {
    hasValue = false;
  }

  public SerializedNullable( T argument ) {

    hasValue = true;
    value    = argument;
  }

  public SerializedNullable( T? argument ) {

    hasValue = argument.HasValue;
    if (hasValue) { value = argument.Value; }
  }

  public static implicit operator T( SerializedNullable< T > nullable ) {
    
    if (nullable.hasValue) { return nullable.value; }
    throw new InvalidOperationException();
  }

  public static implicit operator SerializedNullable< T >( T argument ) {
    return new SerializedNullable< T >( argument );
  }

  public static implicit operator T?( SerializedNullable< T > nullable ) {
    return nullable.HasValue ? nullable.value : (T?) null;
  }

  public override string ToString() {

    if (!HasValue) { return "null"; }
    return value.ToString();
  }
}

public struct TransformData {

  public Vector3    position;
  public Quaternion rotation;
  public Vector3    scale;

  public TransformData( Transform transform, Space space=Space.World ) {

    position = space == Space.World ? transform.position : transform.localPosition;
    rotation = space == Space.World ? transform.rotation : transform.localRotation;
    scale    = transform.localScale;
  }

  public void SetTransform( Transform transform, Space space=Space.World ) {
    
    if (space == Space.Self) {

      transform.localPosition = position;
      transform.localRotation = rotation;
    }
    else {
      transform.position = position;
      transform.rotation = rotation;
    }
    scale = transform.localScale;
  }
}

public class VariantRandomiser {

  CappedQueue< int > recent;
  int                variants = 1;

  public VariantRandomiser( int min_before_repeat, int variants ) {
  
    this.variants = variants;
    recent        = new CappedQueue< int >( min_before_repeat );
  }

  public int next {
    get {
      int selection = UnityEngine.Random.Range( 0, variants - recent.Count );
      for (int i = 0; i < selection; i++) { if (recent.Contains( i )) { selection++; } }
      recent.Enqueue( selection );
      return selection;
    }
  }

  public void Reset() {

    recent.Clear();
  }
}

/// <summary>
/// A giant mass of utility methods and extension methods.</summary>
public static class Utils {

  public static bool Overlap( RectTransform a, RectTransform b, RectTransform canvas ) {
      
    Rect a_rect = RectInCanvasSpace( a, canvas );
    Rect b_rect = RectInCanvasSpace( b, canvas );
      
    Vector2 offset  = a_rect.center - b_rect.center;
    bool    overlap = Mathf.Abs( offset.x ) < (a_rect.width  + b_rect.width ) * 0.5f
                   && Mathf.Abs( offset.y ) < (a_rect.height + b_rect.height) * 0.5f;
      
    return overlap;
  }

  public static Rect[] SplitRectHorizontal( Rect rect, float[] division_points, bool proportional=true,
                                            float gap_size=0.0f ) {
    
    Rect[] rects = new Rect[ division_points.Length + 1 ];
    int    i;

    if (proportional) {

      rects[0] = new Rect( rect.x, rect.y, rect.width * division_points[0] - gap_size * 0.5f, rect.height );
      for (i = 0; i < division_points.Length - 1; i++) {
        rects[ i+1 ] = new Rect( rect.x + rect.width * division_points[i] + gap_size * 0.5f, rect.y,
                                 rect.width * (division_points[ i+1 ] - division_points[i]) - gap_size, rect.height );
      }
      rects[ i+1 ] = new Rect( rect.x + rect.width * division_points[i] + gap_size * 0.5f, rect.y,
                               rect.width * (1.0f - division_points[i]) - gap_size * 0.5f, rect.height );
    }
    else {
      rects[0] = new Rect( rect.x, rect.y, division_points[0] - gap_size * 0.5f, rect.height );
      for (i = 0; i < division_points.Length - 1; i++) {
        rects[ i+1 ] = new Rect( rect.x + division_points[i] + gap_size * 0.5f, rect.y,
                                 division_points[ i+1 ] - division_points[i] - gap_size, rect.height );
      }
      rects[ i+1 ] = new Rect( rect.x + division_points[i] + gap_size * 0.5f, rect.y,
                               rect.width - division_points[i] - gap_size * 0.5f, rect.height );
    }
    return rects;
  }

  public static Rect[][] SplitRectLabels( Rect rect, float[] division_points, float label_width ) {

    Rect[]   sections = SplitRectHorizontal( rect, division_points );
    Rect[][] rects    = new Rect[ division_points.Length + 1 ][];

    for (int i = 0; i < division_points.Length + 1; i++) {
      rects[ i ] = SplitRectHorizontal( sections[ i ], new[] { label_width }, false );
    }
    return rects;
  }
    
  public static Rect RectInCanvasSpace( RectTransform r, RectTransform canvas ) {
      
    Vector3[] corners_in_canvas_space = CornersInCanvasSpace( r, canvas );
      
    return new Rect( corners_in_canvas_space[ 0 ].x,  corners_in_canvas_space[ 0 ].y,
                     corners_in_canvas_space[ 2 ].x - corners_in_canvas_space[ 0 ].x,
                     corners_in_canvas_space[ 1 ].y - corners_in_canvas_space[ 0 ].y );
  }
    
  public static Vector3[] CornersInCanvasSpace( RectTransform r, RectTransform canvas ) {
      
    Vector3[] world_corners = new Vector3[ 4 ];
    r.GetWorldCorners( world_corners );
    return world_corners.Select( v => canvas.InverseTransformPoint( v )
                                      + Vector3.up * canvas.rect.height ).ToArray();
  }
  
  public static Vector3 WorldToCanvasSpace( Vector3 world_target, RectTransform canvas_rect,
                                            RectTransform object_transform ) {
    
    Vector3 canvas_point = Camera.main.WorldToViewportPoint( world_target );
    canvas_point.Scale( canvas_rect.rect.size );
    return canvas_point - Vector3.up    * canvas_rect.rect.height * object_transform.anchorMax.y
                        - Vector3.right * canvas_rect.rect.width  * object_transform.anchorMin.x;
  }

  public static Vector2 DistanceFromRectEdge( RectTransform rect_transform, Vector2 local_point ) {

    Vector2 distance = local_point - (Vector2) rect_transform.localPosition;

    for (int i = 0; i < 2; i++) {
      if (distance[i] > 0) {
        distance[i] = Mathf.Max( 0.0f, distance[i] - rect_transform.rect.size[i] * (1.0f - rect_transform.pivot[i]) );
      } else {
        distance[i] = Mathf.Min( 0.0f, distance[i] + rect_transform.rect.size[i] * rect_transform.pivot[i] );
      }
    }
    return distance;
  }

  public static string[] BoldNames< T >( this IEnumerable< T > objects ) where T: UnityEngine.Object {
    return objects.Select( o => o.BoldName() ).ToArray();
  }

  public static string BoldName( this UnityEngine.Object ob ) { return "<b>" + ob.name + "</b>"; }

  public static string StripStyleTags( this string str ) {
    
    foreach (string tag in new[] { "<b>", "</b>", "<i>", "</i>" }) { str = str.Replace( tag, "" ); }
    return str;
  }

  public static string ToKey( this string str ) {

    if (str == null) { return null; }
    return str.ToLower().Replace(" ", "");
  }

  public static string CommaJoin( this IEnumerable< string > items, bool oxfordComma=false ) {

    string[]      array = items.ToArray();
    StringBuilder sb    = new StringBuilder();
    string        final = items.Count() > 2 && oxfordComma ? ", and " : " and ";
    for (int i = 0; i < array.Length; i++) {
      if (i == 0)                     { sb.Append(         array[ i ] ); }
      else if (i == array.Length - 1) { sb.Append( final + array[ i ] ); }
      else                            { sb.Append(  ", " + array[ i ] ); }
    }
    return sb.ToString();
  }

  public static string IndefiniteArticle( string thing, bool lowercase=false ) {

    string result = thing != null && thing.Length > 1 && "aeiou".Contains( thing.ToLower()[0] ) ? "An" : "A";
    if (lowercase) { result = "a" + result.Substring( 1 ); }
    return result;
  }

    public static string NullChainCheck( string n, object obj, params string[] invocations ) {

      string str = n.Bold() + " invocation chain:\nstarting with:" + obj.ToString();
      object current = obj;

      foreach (string invocation in invocations) {

        if (current == null) {

          str += "\n at invocation '" + invocation + "'; current is null; breaking...";
          break;
        }
        
        Type currType      = current.GetType();
        var  binding_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        FieldInfo field = currType.GetField( invocation, binding_flags );

        if (field != null) {

          current = field.GetValue( current );
          str    += "\n" + (invocation + " (field): ").Bold() + (current != null ? current.ToString() : "null");
          continue;
        }

        MethodInfo method = currType.GetMethod( invocation, binding_flags );

        if (method != null) {

          current = method.Invoke( current, new object[] { } );
          str    += "\n" + (invocation + " (method): ").Bold() + (current != null ? current.ToString() : "null");
          continue;
        }

        PropertyInfo property = currType.GetProperty( invocation, binding_flags );

        if (property != null) {

          current = property.GetValue( current, null );
          str    += "\n" + (invocation + " (property): ").Bold() + (current != null ? current.ToString() : "null");
          continue;
        }
        str += "\n at invocation '" + invocation + "'; no method or property found on type '"
                 + currType.ToString() + "'; breaking...";
        break;
      }
      return str;
    }

  public static string PrintVals< T >( T[] data, bool newline=false ) {
    return string.Join( newline? "\n" : ", ",
                        data.Select( val => val.GetType().Name + ": " + val.ToString() ).ToArray() );
  }

  public static string PrintVals< TKey, TValue >( IDictionary< TKey, TValue > data,
                                                  bool boldKeys=true, bool boldVals=false ) {

    return string.Join( "\n", data.Select( kvp =>
        (boldKeys ? (kvp.Key.ToString() + ": ").Bold() : (kvp.Key.ToString() + ": "))
      + (boldVals ?  kvp.Value.ToString().Bold()       :  kvp.Value.ToString()      ) ).ToArray() );
  }

  struct PrintableValue {

    public string name;
    public string val;
  }
    
  public static PrintValList PrintVals() { return new PrintValList(); }
  public static PrintValList PrintVals< T >( string n, T v ) { return new PrintValList().Add( n, v ); }
  public static PrintValList PrintVals< T >( string n, Func< T > f ) { return new PrintValList().Add( n, f ); }

  public class PrintValList {

    List< PrintableValue > vals = new List< PrintableValue >();

    public PrintValList Add< T >( string n, T v ) {
        
      string val = v == null ? val = "null" : v.ToString();
      vals.Add( new PrintableValue() { name=n, val=val } );
      return this;
    }

    public PrintValList Add< T >( string n, Func< T > f ) {

      string val = "null exception";
      try { val = f().ToString(); }
      catch { }
      vals.Add( new PrintableValue() { name=n, val=val } );
      return this;
    }

    public string ToString( string separator="\n", bool bold_names=true, bool bold_values=false ) {

      return string.Join( separator, vals.Select( v => (bold_names ? (v.name + ": ").Bold()
                                                                   :  v.name + ": ")
                                            + (bold_values ? (v.val != null ? v.val.ToString() : "null").Bold()
                                                           :  v.val != null ? v.val.ToString() : "null") ).ToArray() );
    }

    public static implicit operator string( PrintValList list ) { return list.ToString(); }

    public void Log( bool bold_names=true, bool bold_values=false ) {

      Debug.Log( ToString( "\n", bold_names, bold_values ) );
    }
  }

  public class SwitchMap< Tkey, Tresult > {

    Tkey    input;
    Tresult current;

    internal SwitchMap( Tkey input ) { this.input = input; }

    public static implicit operator Tresult( SwitchMap< Tkey, Tresult > map ) { return map.current; }

    public SwitchMap< Tkey, Tresult > Map( Tkey key, Tresult output ) {

      if (input.Equals( key )) { current = output; }
      return this;
    }

    public Tresult Value { get { return current; } }
  }

  public static SwitchMap< T, U > Map< T, U >( this T input, T key, U output ) {

    var dict = new SwitchMap< T, U >( input );
    return dict.Map( key, output );
  }

  public static GBDictionary< T > ToGBDictionary< T >( this IEnumerable< T > collection,
                                                       Func< T, string > keySelector ) {

    GBDictionary< T > dict = new GBDictionary< T >();
    foreach (T item in collection) { dict[ keySelector( item ) ] = item; }
    return dict;
  }

  public static GBDictionary< TValue > ToGBDictionary< TEntry, TValue >( this IEnumerable< TEntry > collection,
                                                                         Func< TEntry, string > keySelector,
                                                                         Func< TEntry, TValue > valueSelector ) {
    GBDictionary< TValue > dict = new GBDictionary< TValue >();
    foreach (TEntry item in collection) { dict[ keySelector( item ) ] = valueSelector( item ); }
    return dict;
  }

  public static float SeamlessRamp( float x ) { return x - (0.5f / Mathf.PI) * Mathf.Sin( x * 2.0f * Mathf.PI ); }

  public static float SinRamp( float x ) { return 0.5f + -0.5f * Mathf.Cos( x * Mathf.PI ); }

  public static float ProportionalVariationRange( float x ) { return -( (2.0f * x) / (x - 2.0f) ); }
      
    public static float ReversibleClamp( float value, float bound1, float bound2 ) {
        return Mathf.Clamp( value, Mathf.Min( bound1, bound2 ), Mathf.Max( bound1, bound2 ) );
  }
    
  public static float ValueMap( float input, float inMin, float inMax, float outMin, float outMax ) {
      
    float normalisedInput = (input - inMin) / (inMax - inMin); 
    float result = normalisedInput * (outMax - outMin) + outMin;

    if (Input.GetKey( KeyCode.V )) {

      PrintVals( "input",  input  )
           .Add( "inMin",  inMin  )
           .Add( "inMax",  inMax  )
           .Add( "outMin", outMin )
           .Add( "outMax", outMax )
           .Add( "result", result ).Log();
    }
    return result;
  }
    
  public static float SignedPow( float x, float pow ) { return Mathf.Sign( x ) * Mathf.Pow( Mathf.Abs( x ), pow ); }
    
  public static Color ColorMap( float input, float inMin, float inMax, Color outMin, Color outMax ) {
      
    return new Color( ValueMap( input, inMin, inMax, outMin.r, outMax.r ),
                      ValueMap( input, inMin, inMax, outMin.g, outMax.g ),
                      ValueMap( input, inMin, inMax, outMin.b, outMax.b ),
                      ValueMap( input, inMin, inMax, outMin.a, outMax.a ) );
  }

    public static float Median( this IEnumerable< float > vals ) {

        int n = vals.Count();
        if (n < 1) { throw new ArgumentException("Collection is empty"); }
        List< float > sorted = new List< float >( vals );
        sorted.Sort();
        if (n % 2 == 1) { return sorted[ n / 2 ]; }
        else { return (sorted[ n / 2 - 1 ] + sorted[ n / 2 ]) / 2.0f; }
    }
      
    public static Vector3 Sum( this IEnumerable< Vector3 > vecs ) {

        Vector3 sum = Vector3.zero;
        foreach (Vector3 vec in vecs) { sum += vec; }
        return sum;
    }
      
    public static Vector2 Sum( this IEnumerable< Vector2 > vecs ) {

        Vector2 sum = Vector2.zero;
        foreach (Vector2 vec in vecs) { sum += vec; }
        return sum;
    }

    public static Vector3 Average( this IEnumerable< Vector3 > vecs ) {
         
        if (vecs.Count() < 1) { throw new ArgumentException("Collection is empty"); }
        return vecs.Sum() / vecs.Count();
    }

   public static Vector3 WeightedAverage( this IEnumerable< Vector3 > vecs, IEnumerable< float > weights ) {

      float   weightTotal = 0.0f;
      Vector3 sum         = Vector3.zero;

      for (int i = 0; i < vecs.Count(); i++) {

         float weight = weights.ElementAt( i );
         sum         += vecs.ElementAt( i ) * weight;
         weightTotal += weight;
      }
      return sum / weightTotal;
   }

   public static float WeightedAverage( this IEnumerable< float > values, IEnumerable< float > weights ) {

      float weightTotal = 0.0f;
      float sum         = 0.0f;

      for (int i = 0; i < values.Count(); i++) {

         float weight = weights.ElementAt( i );
         sum         += values.ElementAt( i ) * weight;
         weightTotal += weight;
      }
      return sum / weightTotal;
   }

    public static Vector2 Average( this IEnumerable< Vector2 > vecs ) {
         
        if (vecs.Count() < 1) { throw new ArgumentException("Collection is empty"); }
        return vecs.Sum() / vecs.Count();
    }

    public static string MatrixToString< T >( this IList< IList< T > > vecs ) {

        return string.Join( "\n", vecs.Select( vec => string.Join( ", ", vec.Select( v => v.ToString() )
                                                                            .ToArray() ) )
                                      .ToArray() );
    }
      
    public static T[][] Transpose< T >( this IList< IList< T > > vecs ) {

      return Enumerable.Range( 0, vecs.First().Count() )
                       .Select( i => vecs.Select( v => v[i] )
                                         .ToArray() )
                       .ToArray();
    }

    // Vector median generalised to lists of floats, getting the median of corresponding entries. Filthy.
    public static IEnumerable< float > Median( this IEnumerable< IList< float > > vecs ) {

        return Enumerable.Range( 0, vecs.First().Count() )
                         .Select( i => vecs.Select( v => v[ i ] )
                                           .Median() );
    }

    public static Vector3 Median( this IEnumerable< Vector3 > vecs )  {

        Vector3 vec = Vector3.zero;
        for (int i = 0; i < 3; i++) { vec[i] = vecs.Select( v => v[i] ).Median(); }
        return vec;
    }

    public static Vector3 Center( this IEnumerable< Vector3 > vecs ) {

        Vector3 vec = Vector3.zero;

        for (int i = 0; i < 3; i++) {
          vec[i] = (vecs.Select( v => v[i] ).Min()
                  + vecs.Select( v => v[i] ).Max()) * 0.5f;
        }
        return vec;
    }

    public static float AbsoluteSum( this IEnumerable< float > vals ) {

        return vals.Select( v => Mathf.Abs( v ) ).Sum();
    }

    public static Vector3 RandomVector3 {
        get {
          return new Vector3( UnityEngine.Random.value,
                              UnityEngine.Random.value,
                              UnityEngine.Random.value ) * 2.0f - Vector3.one;
        }
    }

    public static Vector2 RandomVector2 {
        get {
          return new Vector2( UnityEngine.Random.value,
                              UnityEngine.Random.value ) * 2.0f - Vector2.one;
        }
    }

    public static T GetOrNull< U, T >( this IDictionary< U, T > dictionary, U key ) where T: class {

        if (!dictionary.ContainsKey( key )) { return null; }
        return dictionary[ key ];
    }

    public static T? FirstOrNull< T >( this ICollection< T > collection, Func< T, bool > predicate ) where T: struct {

    if (collection.Any( i => predicate( i ) )) { return collection.First( i => predicate( i ) ); }
    return null;
  }

    public static void DestroyGameObject( this UnityEngine.Object o ) {
       
      if (o.GetType().IsSubclassOf( typeof( Component ) )) {
        UnityEngine.Object.Destroy( (o as Component).gameObject );
      } else {
        UnityEngine.Object.Destroy( o );
      }
    }

    public static void DestroyAll< T >( this ICollection< T > collection, bool destroyGameObject=true,
                                        bool clear=true ) where T: UnityEngine.Object {


      foreach (T item in collection) {
        if (destroyGameObject) { item.DestroyGameObject(); }
        else { UnityEngine.Object.Destroy( item ); }
      }
      if (clear) { collection.Clear(); }
    }

    public static void DestroyAllValues< T, U >( this IDictionary< T, U > collection, bool destroyGameObject=true )
                                      where U: UnityEngine.Object {
       
      foreach (U item in collection.Values) {
        if (destroyGameObject) { item.DestroyGameObject(); }
        else { UnityEngine.Object.Destroy( item ); }
      }
      collection.Clear();
    }
    
  public static void Destroy< T >( this ICollection< T > collection, T o, bool destroyGameObject=true )
                        where T: UnityEngine.Object {
      
    collection.Remove( o );
    if (destroyGameObject) { o.DestroyGameObject(); }
    else { UnityEngine.Object.Destroy( o ); }
  }
    
  public static void Destroy< T, U >( this IDictionary< T, U > collection, T key, bool destroyGameObject=true )
                           where U: UnityEngine.Object {

    UnityEngine.Object o = collection.GetOrNull( key );
    if (!o) {
      Debug.LogWarning("Key not found in IDictionary.Destroy()");
      return;
    }
    collection.Remove( key );
    if (destroyGameObject) { o.DestroyGameObject(); }
    else { UnityEngine.Object.Destroy( o ); }
  }

  // modified from http://stackoverflow.com/a/653602
  public static int RemoveAll< TKey, Tvalue >( this SortedList< TKey, Tvalue > collection,
                                            Func< KeyValuePair< TKey, Tvalue >, bool > predicate ) {

    KeyValuePair< TKey, Tvalue > element;
    int                          num_removed = 0;

    for (int i = 0; i < collection.Count; i++) {
        element = collection.ElementAt( i );
        if (predicate( element )) {
            collection.RemoveAt( i );
            num_removed++;
            i--;
        }
    }
    return num_removed;
  }

  public static int RemoveValue< TKey, Tvalue >( this SortedList< TKey, Tvalue > collection, Tvalue value )
                                 where Tvalue: IEquatable< Tvalue > {

    KeyValuePair< TKey, Tvalue > element;
    int                          num_removed = 0;

    for (int i = 0; i < collection.Count; i++) {
        element = collection.ElementAt( i );
        if (element.Value.Equals( value )) {
            collection.RemoveAt( i );
            num_removed++;
            i--;
        }
    }
    return num_removed;
  }

    public static T Random< T >( this IEnumerable< T > collection ) {

        return collection.ElementAt( UnityEngine.Random.Range( 0, collection.Count() ) );
    }

    // Should only be used on sets of similar quaternions
    public static Quaternion Average( this IEnumerable< Quaternion > quats ) {

        int        n   = 0;
        Quaternion avg = Quaternion.identity;

        foreach( Quaternion q in quats ) {

          avg = Quaternion.Slerp( avg, q, 1.0f / n );
          n++;
        }
        return avg;
    }

    public static void SetAlpha( this Graphic graphic, float alpha ) {
        
      if (!graphic) { return; }
      Color color   = graphic.color;
      color.a       = alpha;
      graphic.color = color;
    }

    public static void SetAlpha( this Material material, float alpha ) {
       
      Color color    = material.color;
      color.a        = alpha;
      material.color = color;
    }

    public static void SetColorExceptAlpha( this Graphic graphic, Color color ) {

      if (!graphic) { return; }
      Color original = graphic.color;
      color.a        = original.a;
      graphic.color  = color;
    }

    public static bool IsNullOrWhitespace( this string str ) {

      if (str == null) { return true; }
      if (str.Trim().Length == 0) { return true; }
      return false;
    }

    public static string Color( this string str, Color col ) {
      //return "<color=#" + col.ToHexStringRGBA() + ">" + str + "</color>";
      return "<color=#" + ColorUtility.ToHtmlStringRGBA( col ) + ">" + str + "</color>";
    }

    public static string Bold( this string str ) { return "<b>" + str + "</b>"; }

    public static string Truncated( this string str, int max, string suffix="...", int display_max=-1 ) {

      if (str == null) { return null; }
      if (display_max < 0) { display_max = max; }
      return str.Length > max ? str.Substring( 0, display_max ) + suffix : str;
    }

  public static string MaxSubstring( this string str, int startIndex, int length ) {

    length = Mathf.Clamp( length, 0, str.Length - startIndex );
    return str.Substring( startIndex, length );
  }

    public static T[] EnumValues< T >() {
      return Enum.GetValues( typeof( T ) ).Cast< T >().ToArray();
    }

    public static T ToEnum< T >( this string str ) {
      return (T) Enum.Parse( typeof( T ), str, true );
    }

    public static float Sqrt( this float f )          { return Mathf.Sqrt( f );    }
    public static float Pow ( this float f, float p ) { return Mathf.Pow ( f, p ); }

  public static int Ring( this int n, int size, int change ) {

    n += change;
    while (n <  0   ) { n += size; }
    while (n >= size) { n -= size; }
    return n;
  }

  public static float TimeLog( float start_time=-1, string procedure=null ) {
      
    if (start_time >= 0 && procedure != null) {
      Debug.Log( "Time taken for <b>" + procedure + "</b>: " + (Time.realtimeSinceStartup - start_time) );
    }
    return Time.realtimeSinceStartup;
  }

  public static void DoWhenTrue( MonoBehaviour behaviour, Func< bool > condition, Action action,
                                 Func< bool > abort_condition=null ) {

    string   trace       = Environment.StackTrace;
    string[] split_trace = trace.Split('\n');
      for (int i = 0; i < split_trace.Length; i++) {
      string[] split_line = split_trace[ i ].Split( (char[]) null, StringSplitOptions.RemoveEmptyEntries );
      for (int j = 0; j < split_line.Length; j++) {
        split_line[ j ] = split_line[ j ].Split('\\').Last();
      }
      split_trace[ i ] = string.Join( " ", split_line );
      //split_trace[ i ] = string.Join( " ", new[] { split_line[0], split_line[1], split_line.Last() } );
    }
    trace = string.Join( "\n", split_trace );
    behaviour.StartCoroutine( WaitForCondition( action, condition, behaviour.name + " (" + behaviour.GetType() + ")",
                                                trace, abort_condition ) );
  }

  static IEnumerator WaitForCondition( Action action, Func< bool > condition, string name, string trace,
                                       Func< bool > abort_condition=null ) {

    while (true) {
      if (abort_condition != null && abort_condition()) { yield break; }
      if (!condition()) { yield return null; }
      else { break; }
    }
    //Debug.Log( "DoWhenTrue action about to be performed on: " + name + ", trace follows:\n" + trace );
    action();
  }

  public static T FindComponent< T >( this Component component, string name ) where T: Component {

    foreach (T c in component.GetComponentsInChildren< T >( true )) {
      if (c.name.ToKey() == name.ToKey()) { return c; }
    }
    return null;
  }

  public static TResult To< TSource, TResult >( this TSource source, Func< TSource, TResult > operation ) {

    return operation( source );
  }

  public static IEnumerable< TResult > SplitTo< TResult >( this string str, char separator,
                                                           Func< string, TResult > operation ) {

    return str.Split( new[] { separator } ).Select( s => operation( s ) );
  }

  public static T MinBy< T, TBy >( this IEnumerable< T > items, Func< T, TBy > selector )
                      where TBy: IComparable {

    T   current_item = items.First();
    TBy current_min  = selector( current_item );
    foreach (T item in items) {
      TBy value = selector( item );
      if (value.CompareTo( current_min ) < 0) {
        current_item = item;
        current_min  = value;
      }
    }
    return current_item;
  }

  public static T MaxBy< T, TBy >( this IEnumerable< T > items, Func< T, TBy > selector )
                      where TBy: IComparable {

    T   current_item = items.First();
    TBy current_max  = selector( current_item );
    foreach (T item in items) {
      TBy value = selector( item );
      if (value.CompareTo( current_max ) > 0) {
        current_item = item;
        current_max  = value;
      }
    }
    return current_item;
  }

  public static T MatchingName< T >( this IEnumerable< T > items, string name, bool keyify=true )
                          where T: UnityEngine.Object {

    if (items == null || items.Count() == 0) { return null; }
    if (keyify) { return items.FirstOrDefault( item => item && item.name.ToKey() == name.ToKey() ); }
    return items.FirstOrDefault( item => item && item.name == name );
  }
  
  static string[] resources_subdirectories;

  public static void SetResourcesDirectories() {
    
    string   resources_path  = Application.dataPath + "/Resources";
    string[] directories     = System.IO.Directory.GetDirectories( resources_path, "*",
                                                                   System.IO.SearchOption.AllDirectories );
    resources_subdirectories = directories.Select( s => s.Substring( resources_path.Length + 1 )
                                                         .Replace( '\\', '/' ) ).ToArray();
  }
  
  public static T FindResource< T >( string name, bool verbose=false ) where T: UnityEngine.Object {
    
    if (resources_subdirectories == null) { SetResourcesDirectories(); }
    T result = null;

    foreach (string subdirectory in resources_subdirectories) {

      if (verbose) {
        Debug.Log( "Attempting to load " + typeof( T ).Name + " at " + subdirectory + "/" + name );
      }
      result = Resources.Load< T >( subdirectory + "/" + name );
      if (result != null) {
        if (verbose) { Debug.Log( "Found " + typeof( T ).Name + " at " + subdirectory + "/" + name ); }
        return result;
      }
    }
    if (verbose) { Debug.LogWarning( "No " + typeof( T ).Name + " found in resources" ); }
    return null;
  }

  public static void SetNavigationSequence( IEnumerable< Selectable > selectables, bool horizontal,
                                            bool keep_current=true ) {

    Selectable prev = null;
    foreach (Selectable s in selectables) {
      if (!keep_current) { s.SetNavigation( null, null, null, null ); }
      Navigation s_navigation = s.navigation;
      if (prev) {
        Navigation prev_navigation = prev.navigation;
        if (horizontal) {
          prev_navigation.selectOnRight = s;
          s_navigation.selectOnLeft     = prev;
        } else {
          prev_navigation.selectOnDown = s;
          s_navigation.selectOnUp      = prev;
        }
        prev.navigation = prev_navigation;
        s.navigation    = s_navigation;
      }
      prev = s;
    }
    Debug.Log( "Setting " + (horizontal ? "horizontal" : "vertical") + " navigation sequence:\n"
                 + string.Join( "\n", selectables.Select( s => s.name ).ToArray() ) );
  }

  public static void SetNavigation( this Selectable selectable,
                                    Selectable up, Selectable down, Selectable left, Selectable right,
                                    bool keep_current=true ) {

    Navigation navigation = selectable.navigation;

    if (up    != null || !keep_current) { navigation.selectOnUp    = up;    }
    if (down  != null || !keep_current) { navigation.selectOnDown  = down;  }
    if (left  != null || !keep_current) { navigation.selectOnLeft  = left;  }
    if (right != null || !keep_current) { navigation.selectOnRight = right; }

    selectable.navigation = navigation;
  }

  public static void SetNavigationMode( this Selectable selectable, Navigation.Mode mode ) {

    Navigation navigation = selectable.navigation;
    navigation.mode       = mode;
    selectable.navigation = navigation;
  }

  static Selectable ClosestSelectableForDirection( Vector2 origin, Vector2 direction,
                                                   IEnumerable< KeyValuePair< Selectable, RectTransform > > others ) {
    
    var current_closest = others.First();
    foreach (var other in others) {
      if (Vector2.Angle( other.Value.anchoredPosition - origin, direction )
          < Vector2.Angle( current_closest.Value.anchoredPosition - origin, direction )) {
        current_closest = other;
      }
    }
    return Vector2.Angle( current_closest.Value.anchoredPosition - origin, direction ) < 90.0f ? current_closest.Key
                                                                                               : null;
  }

  public static void SetFreeformNavigation( Dictionary< Selectable, RectTransform > elements ) {

    if (elements.Count < 2) {

      Debug.LogWarning("Can't set freeform navigation for less than two elements.");
      return;
    }

    foreach (var element in elements) {

      Navigation navigation     = element.Key.navigation;
      Vector2    this_pos       = element.Value.anchoredPosition;
      var        other_elements = elements.Where( e => e.Key != element.Key );
      navigation.selectOnUp     = ClosestSelectableForDirection( this_pos, Vector2.up,    other_elements );
      navigation.selectOnRight  = ClosestSelectableForDirection( this_pos, Vector2.right, other_elements );
      navigation.selectOnDown   = ClosestSelectableForDirection( this_pos, Vector2.down,  other_elements );
      navigation.selectOnLeft   = ClosestSelectableForDirection( this_pos, Vector2.left,  other_elements );
      element.Key.navigation    = navigation;
    }
  }

  public static bool CanNavigateTo( this Selectable self, Selectable other ) {
    
    if (self.navigation.selectOnUp    == other) { return true; }
    if (self.navigation.selectOnRight == other) { return true; }
    if (self.navigation.selectOnDown  == other) { return true; }
    if (self.navigation.selectOnLeft  == other) { return true; }
    return false;
  }

  public static int LogNearest( float value, List< float > vals ) {

    int   closest_ind      = 0;
    float closest_distance = Mathf.Infinity;
    float log_value        = Mathf.Log( value, 2.0f );

    for (int i = 0; i < vals.Count; i++) {

      float distance = Mathf.Abs( Mathf.Log( vals[ i ], 2.0f ) - log_value );

      if (distance < closest_distance) {

        closest_ind      = i;
        closest_distance = distance;
      }
    }
    return closest_ind;
  }

  public static float FlatToLinearRamp( float value, bool reverse=false, float mix=1.0f ) {

    return value.Pow( reverse ? 2.0f : 0.5f ) * mix + value * (1.0f - mix);
  }

   public static float PowerRamp( float value, float power ) {

      float pingpong = Mathf.PingPong( value * 2.0f, 1.0f ).Pow( power );
      return value < 0.5f ? pingpong * 0.5f : 1.0f - pingpong * 0.5f;
   }

  public static int FractionalChoiceProbability( int choices, float min, float max ) {

    int val = Mathf.Clamp( (int) (UnityEngine.Random.Range( min, max ) * choices), 0, choices - 1 );
    //Debug.Log( "Fractional choice, n: " + choices + ", min: " + min + ", max: " + max + ", chosen: " + val );
    return val;
  }

  public static T WeightedRandom< T >( this IEnumerable< T > items ) where T: IHasFrequency {

    float randomPoint = UnityEngine.Random.Range( 0.0f, items.Sum( i => i.frequency ) );

    foreach (T item in items) {
      if (randomPoint < item.frequency) { return item; }
      randomPoint -= item.frequency;
    }
    Debug.LogWarning( "THIS SHOULD NEVER HAPPEN: iterated over entire weighted item list; returning last. List contents:\n" + string.Join( "\n", items.Select( i => i.ToString() + "; frequency: " + i.frequency ).ToArray() ) );
    throw new InvalidOperationException( "Iterated over whole weighted item list :(" );
    //return items.Last();
  }

  public static IEnumerable< T > RandomSelection< T >( this IEnumerable< T > items, int num ) {

    int total = items.Count();
    if (num >= total) { return items; }
    T[] selected = new T[ num ];
    var random   = new System.Random();
    int source_index = 0, selected_index = 0;

    foreach (T item in items) {
      if (random.NextDouble() < (num - selected_index) / (double) (total - source_index++)) {
        selected[ selected_index++ ] = item;
      }
    }
    Debug.Log( "RandomSelection()".Bold() + " returned " + selected_index + " * " + typeof( T ) );
    return selected;
  }

  public static bool InRange( int value, int min, int max ) {

    if (value >= min && value < max) { return true; }
    return false;
  }
  
  public static uint RotateLeft( this uint value, int count )
  {
      return (value << count) | (value >> (32 - count));
  }

  public static int DiceRoll( int num_dice, int dice_max, int base_result=0 ) {

    return base_result + Enumerable.Range( 0, num_dice - 1 )
                                   .Select( n => UnityEngine.Random.Range( 1, dice_max + 1 ) ).Sum();
  }

  public static float Cycle( float period, bool centerOnZero ) {
    return Cycle( period, 0.0f, 0.0f, centerOnZero );
  }

  public static float Cycle( float period, float startTime=0.0f, float phase=0.0f, bool centerOnZero=false ) {
      
    float raw = Mathf.Sin( ((Time.time - startTime) * 2.0f * Mathf.PI) / period + (phase * Mathf.Deg2Rad) );
    return centerOnZero ? raw : raw * 0.5f + 0.5f;
  }

  public static float deltaTimeAdjustment { get { return Time.deltaTime / 0.0166667f; } }

  public static float SignedProjectionMagnitute( Vector3 vector, Vector3 onNormal, bool proportionalToNormal=false ) {
    
      return Vector3.Project( vector, onNormal ).magnitude
               * Mathf.Sign( Vector3.Dot( vector, onNormal ) )
               * (proportionalToNormal ? onNormal.magnitude : 1.0f);
  }

  //static string CheckMatch( int x, int y, IntRange? override_range=null ) {

  //  Coord initial_coord = new Coord( x, y );
  //  long pair_value = Utils.ElegantPair( initial_coord.x, initial_coord.y, override_range );
  //  Coord extracted_coord = Utils.ElegantUnpair( pair_value, override_range );
  //  return "Match: " + (initial_coord == extracted_coord) + "; initial: " + initial_coord
  //           + "; pair value: " + pair_value + "; extracted: " + extracted_coord;
  //}

  //static bool CheckMatchBool( int x, int y, IntRange range ) {
    
  //  Coord initial_coord = new Coord( x, y );
  //  long pair_value = Utils.ElegantPair( initial_coord.x, initial_coord.y, range );
  //  Coord extracted_coord = Utils.ElegantUnpair( pair_value, range );
  //  return initial_coord == extracted_coord;
  //}

  //static IntRange GetSignedIntRange( int max_unsigned ) {  // TODO: this should not use integer division maybe?

  //  return new IntRange( Mathf.FloorToInt( -(max_unsigned / 2) ), Mathf.FloorToInt( (max_unsigned - 1) / 2 ) );
  //}

  //static void TestRange( IntRange range, bool override_range=true ) {
    
  //  List< string > lines = new List< string >();
  //  lines.Add( "Testing range: " + range );
  //  int value = range.min;
  //  lines.Add( CheckMatch( value, value, override_range ? range : (IntRange?)null ) );
  //  value = range.min + 1;
  //  lines.Add( CheckMatch( value, value, override_range ? range : (IntRange?)null ) );
  //  value = range.max;
  //  lines.Add( CheckMatch( value, value, override_range ? range : (IntRange?)null ) );
  //  value = range.max - 1;
  //  lines.Add( CheckMatch( value, value, override_range ? range : (IntRange?)null ) );
  //  value = -2;
  //  lines.Add( CheckMatch( value, value, override_range ? range : (IntRange?)null ) );
  //  value = -1;
  //  lines.Add( CheckMatch( value, value, override_range ? range : (IntRange?)null ) );
  //  value =  0;
  //  lines.Add( CheckMatch( value, value, override_range ? range : (IntRange?)null ) );
  //  value =  1;
  //  lines.Add( CheckMatch( value, value, override_range ? range : (IntRange?)null ) );
  //  value =  2;
  //  lines.Add( CheckMatch( value, value, override_range ? range : (IntRange?)null ) );
  //  Debug.Log( string.Join( "\n", lines.ToArray() ) );
  //}

  //static bool ValidateRange( IntRange range ) {

  //  return CheckMatchBool( -1, -1, range );
  //}

  //static int MaxRawArg( long intRange ) {

  //  long max_raw = Mathf.FloorToInt( intRange / (Mathf.Ceil( Mathf.Sqrt( intRange ) ) + 1) );
  //  long tested_result = max_raw * max_raw + max_raw + max_raw;
  //  Debug.Log( "MaxRawArg; int max: " + intRange + "; max raw: " + max_raw
  //                      + "; tested_result: " + tested_result );
  //  return (int) max_raw;
  //}

  //public static long ElegantPair( int x, int y, IntRange? override_range=null ) {

  //  checked {
  //    int max  =  23169;
  //    int min  = -23169;
  //    int flip =  46339;

  //    if (override_range.HasValue) {

  //      max  = override_range.Value.max;
  //      min  = override_range.Value.min;
  //      flip = max - min + 1;
  //    }
    
  //    if (x > max || x < min) {
  //      throw new ArgumentOutOfRangeException( "x",
  //        "Arguments must not have an absolute value greater than " + max + ". Value given: " + x );
  //    }
  //    if (y > max || y < min) {
  //      throw new ArgumentOutOfRangeException( "y",
  //        "Arguments must not have an absolute value greater than " + max + ". Value given: " + y );
  //    }
  //    if (max >= 4000000 || -min > 4000000) {
  //      throw new ArgumentOutOfRangeException( "override_range",
  //        "Unable to unpair values with argument ranges over 4000000. Value given: " + override_range.Value );
  //    }
  //    long nx = x < 0 ? x + flip : x;
  //    long ny = y < 0 ? y + flip : y;
  //    return (nx >= ny) ? (nx * nx + nx + ny) : (ny * ny + nx);
  //  }
  //}

  //public static Coord ElegantUnpair( long z, IntRange? override_range=null ) {
    
  //  checked {
  //    int max  = 23169;
  //    int flip = 46339;

  //    if (override_range.HasValue) {

  //      max  = override_range.Value.max;
  //      flip = max - override_range.Value.min + 1;
  //    }
    
  //    int sqrtz = (int) Math.Sqrt( z );
  //    int sqz   = sqrtz * sqrtz;
  //    long x = 0, y = 0;

  //    if ((z - sqz) >= sqrtz) {
  //      x = sqrtz;
  //      y = z - sqz - sqrtz;
  //    } else {
  //      x = z - sqz;
  //      y = sqrtz;
  //    }
  //    if (x > max) { x -= flip; }
  //    if (y > max) { y -= flip; }

  //    return new Coord( (int) x, (int) y );
  //  } 
  //}

  public static GameObject InstantiateParticleEffect( GameObject effect, Vector3 position, Quaternion rotation,
                                                      MonoBehaviour script ) {
    
    GameObject effect_instance = UnityEngine.Object.Instantiate( effect, position, rotation ) as GameObject;

    Lerp( script, effect_instance.GetComponentsInChildren< ParticleSystem >().Max( ps => ps.startLifetime ), null,
          () => UnityEngine.Object.Destroy( effect_instance ) );

    return effect_instance;
  }

  public static void ShowCachedParticleEffect( GameObject effect, Vector3 position, Quaternion rotation,
                                                     MonoBehaviour script ) {

      effect.SetActive( true );
      effect.transform.position = position;
      effect.transform.rotation = rotation;

      foreach (ParticleSystem ps in effect.GetComponentsInChildren< ParticleSystem >()) { ps.Play(); }

    Lerp( script, effect.GetComponentsInChildren< ParticleSystem >().Max( ps => ps.startLifetime ), null,
          () => effect.SetActive( false ) );
  }

   static IEnumerator LerpCoroutine( float duration, Action< float > lerpAction, Action completion, bool realTime ) {

      float startTime = realTime ? Time.unscaledTime : Time.time;
      yield return null;

      while (true) {
         
         float time = realTime ? Time.unscaledTime : Time.time;
         if (lerpAction != null) { lerpAction( Mathf.Clamp01( time / duration ) ); }

         if (startTime + duration < time) {
            
            if (completion != null) { completion(); }
            yield break;
         }
         yield return null;
      }
   }

   public static void Lerp( MonoBehaviour behaviour, float duration, Action< float > lerpAction, Action completion,
                            bool realTime=false ) {

      behaviour.StartCoroutine( LerpCoroutine( duration, lerpAction, completion, realTime ) );
   }

   public static void BulletTime( MonoBehaviour behaviour, float slow_timescale, float duration, bool relative=false,
                                  bool revert_to_1=true ) {
    
    float prev_timescale = Time.timeScale;
    Time.timeScale       = relative ? Time.timeScale * slow_timescale : slow_timescale;
    Lerp( behaviour, duration, null, () => Time.timeScale = revert_to_1 ? 1.0f : prev_timescale, true );
  }

  public static bool ChangeMaterial( this Renderer renderer, string name, Action< Material > change ) {

    return renderer.ChangeMaterial( m => m.name == name, change );
  }

  public static bool ChangeMaterial( this Renderer renderer, Func< Material, bool > selector,
                                     Action< Material > change ) {

    Material[] mats    = renderer.materials;
    bool       changed = false;

    for (int i = 0; i < mats.Length; i++) {

      if (selector( mats[ i ] )) {

        change( mats[ i ] );
        changed = true;
      }
    }
    renderer.materials = mats;
    return changed;
  }

  public static void SetData( this Transform transform, TransformData data, Space space=Space.World ) {

    if (space == Space.World) {
      transform.position = data.position;
      transform.rotation = data.rotation;
    } else {
      transform.localPosition = data.position;
      transform.localRotation = data.rotation;
    }
    transform.localScale = data.scale;
  }

  public static void DrawDebugGraph( IEnumerable< float > vals, Color color, float vertical_scale=1.0f,
                                     float interval=1.0f, Vector3? origin_point=null, float duration=0.0f ) {
    
    Vector3 last   = Vector3.zero;
    Vector3 origin = origin_point ?? Vector3.zero;
    int     i      = 0;

    foreach (float val in vals) {

      Vector3 current = origin + new Vector3( i * interval, val * vertical_scale, 0.0f );
      if (i > 0) { Debug.DrawLine( last, current, color, duration ); }
      last = current;
      i++;
    }
  }

  public static void DrawDebugGraph( IEnumerable< int > vals, Color color, float vertical_scale=1.0f,
                                     float interval=1.0f, Vector3? origin_point=null, float duration=0.0f ) {
    
    Vector3 last   = Vector3.zero;
    Vector3 origin = origin_point ?? Vector3.zero;
    int     i      = 0;

    foreach (int val in vals) {

      Vector3 current = origin + new Vector3( i * interval, val * vertical_scale, 0.0f );
      if (i > 0) { Debug.DrawLine( last, current, color, duration ); }
      last = current;
      i++;
    }
  }

  public static Vector3 RotateAround( this Vector3 v, Vector3 pivot, Vector3 angles ) {
    return Quaternion.Euler( angles ) * (v - pivot) + pivot;
  }

   // Rodrigues' rotation formula
   public static Vector3 RotateAroundAxis( this Vector3 v, Vector3 axis, float angle ) {

      return v * Mathf.Cos( angle ) + Vector3.Cross( axis, v ) * Mathf.Sin( angle ) + axis * Vector3.Dot( axis, v ) * (1.0f - Mathf.Cos( angle ));
   }
    
  public static Vector2 xy( this Vector3 v ) { return new Vector2( v.x, v.y ); }
  public static Vector2 xz( this Vector3 v ) { return new Vector2( v.x, v.z ); }
  public static Vector2 yz( this Vector3 v ) { return new Vector2( v.y, v.z ); }
    
  public static Vector3 xPart( this Vector3 v ) { return Vector3.right   * v.x; }
  public static Vector3 yPart( this Vector3 v ) { return Vector3.up      * v.y; }
  public static Vector3 zPart( this Vector3 v ) { return Vector3.forward * v.z; }
    
  public static Vector3 ZeroX( this Vector3 v ) { return new Vector3( 0.0f, v.y,  v.z  ); }
  public static Vector3 ZeroY( this Vector3 v ) { return new Vector3( v.x,  0.0f, v.z  ); }
  public static Vector3 ZeroZ( this Vector3 v ) { return new Vector3( v.x,  v.y,  0.0f ); }
    
  public static Vector3 RotateTowardsOvershoot( Vector3 from, Vector3 to, float angle ) {

      if (Vector3.Angle( from, to ) < angle) { return Vector3.RotateTowards( from, to, angle, Mathf.Infinity ); }
      return Vector3.RotateTowards( -from, to, 180.0f - angle, Mathf.Infinity );
   }

  public static Vector3 MagnitudeAdjusted( this Vector3 v, float adjustment ) {
    return v.normalized * (v.magnitude + adjustment);
  }
  
  public static bool Down( this KeyCode key )    { return Input.GetKeyDown( key ); }
  public static bool Pressed( this KeyCode key ) { return Input.GetKey( key );     }
  public static bool Up( this KeyCode key )      { return Input.GetKeyUp( key );   }

  /*  Zoom function:
   *  - 0 input = base FOV
   *  - each +1 to input should result in halving the rect height of FOV
   *  - fov -> rect height:
   *     rect_height = 2.0f * Mathf.Tan( Mathf.Deg2Rad * fov * 0.5f )
   *  - rect height -> fov:
   *     fov = 2.0f * Mathf.Rad2Deg * Mathf.Atan( rect_height * 0.5f );
   *  - rect height -> zoom value: (if base rect height is 10)
   *     10    -> 0
   *      5    -> 1
   *      2.5  -> 2
   *      1.25 -> 3
   *     zoom_value = Mathf.Log( base_rect_height / rect_height, 2.0f )
   *  - zoom value -> rect height: (if base rect height is 10)
   *     0 -> 10
   *     1 ->  5
   *     2 ->  2.5
   *     3 ->  1.25
   *     rect_height = base_rect_height / Mathf.Pow( 2.0f, zoom_value )
   */
  static float FOVToZoomValue( float fov ) {

    float rect_height = 2.0f * Mathf.Tan( Mathf.Deg2Rad * fov * 0.5f );
    return Mathf.Log( 10 / rect_height, 2.0f );
  }

  static float ZoomValueToFOV( float zoomValue ) {

    float rect_height = 10 / Mathf.Pow( 2.0f, zoomValue );
    return 2.0f * Mathf.Rad2Deg * Mathf.Atan( rect_height * 0.5f );
  }

  public static float LerpPerceptualZoom( float fromFOV, float toFOV, float t ) {

    return ZoomValueToFOV( Mathf.Lerp( FOVToZoomValue( fromFOV ), FOVToZoomValue( toFOV ), t ) );
  }
}
