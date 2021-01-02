using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
public struct ColorRGBA : IEquatable<ColorRGBA>
{
  private static readonly char[] TrimChars = new char[1]
  {
    '#'
  };

  public static readonly ColorRGBA Red = new ColorRGBA( byte.MaxValue, (byte) 0, (byte) 0 );
  public static readonly ColorRGBA Blue = new ColorRGBA( (byte) 0, (byte) 0, byte.MaxValue );
  public static readonly ColorRGBA Gray = new ColorRGBA( (byte) 127, (byte) 127, (byte) 127 );
  public static readonly ColorRGBA Cyan = new ColorRGBA( (byte) 0, byte.MaxValue, byte.MaxValue );
  public static readonly ColorRGBA White = new ColorRGBA( byte.MaxValue, byte.MaxValue, byte.MaxValue );
  public static readonly ColorRGBA Green = new ColorRGBA( (byte) 0, byte.MaxValue, (byte) 0 );
  public static readonly ColorRGBA Yellow = new ColorRGBA( byte.MaxValue, byte.MaxValue, (byte) 0 );
  public static readonly ColorRGBA Magenta = new ColorRGBA( byte.MaxValue, (byte) 0, byte.MaxValue );
  public static readonly ColorRGBA Black = new ColorRGBA( (byte) 0, (byte) 0, (byte) 0 );
  public static readonly ColorRGBA Lightgray = new ColorRGBA( (byte) 200, (byte) 200, (byte) 200 );
  public static readonly ColorRGBA ColliderGreen = new ColorRGBA( (byte) 181, (byte) 230, (byte) 29 );
  public static readonly ColorRGBA ColliderBlue = new ColorRGBA( (byte) 153, (byte) 217, (byte) 234 );
  [FieldOffset( 0 )] public byte R;
  [FieldOffset( 1 )] public byte G;
  [FieldOffset( 2 )] public byte B;
  [FieldOffset( 3 )] public byte A;
  public const int SIZE = 4;

  public ColorRGBA( string hex )
  {
    if ( hex[0] == '#' )
      hex = hex.TrimStart( ColorRGBA.TrimChars );
    if ( hex.Length == 6 )
    {
      this.R = (byte) int.Parse( hex.Substring( 0, 2 ), NumberStyles.HexNumber );
      this.G = (byte) int.Parse( hex.Substring( 2, 2 ), NumberStyles.HexNumber );
      this.B = (byte) int.Parse( hex.Substring( 4, 2 ), NumberStyles.HexNumber );
      this.A = byte.MaxValue;
    }
    else if ( hex.Length == 8 )
    {
      this.R = (byte) int.Parse( hex.Substring( 0, 2 ), NumberStyles.HexNumber );
      this.G = (byte) int.Parse( hex.Substring( 2, 2 ), NumberStyles.HexNumber );
      this.B = (byte) int.Parse( hex.Substring( 4, 2 ), NumberStyles.HexNumber );
      this.A = (byte) int.Parse( hex.Substring( 6, 2 ), NumberStyles.HexNumber );
    }
    else
    {
      this.R = byte.MaxValue;
      this.G = (byte) 0;
      this.B = byte.MaxValue;
      this.A = byte.MaxValue;
      UnityEngine.Debug.LogError( $"Can't parse color {(object) hex}" );
    }
  }

  public ColorRGBA( byte r, byte g, byte b )
  {
    this.R = r;
    this.G = g;
    this.B = b;
    this.A = byte.MaxValue;
  }

  public ColorRGBA( byte r, byte g, byte b, byte a )
  {
    this.R = r;
    this.G = g;
    this.B = b;
    this.A = a;
  }

  public override int GetHashCode() =>
    ( ( ( 17 * 31 + (int) this.R ) * 31 + (int) this.G ) * 31 + (int) this.B ) * 31 + (int) this.A;

  public override bool Equals( object obj ) => obj is ColorRGBA other && this.Equals( other );

  public override string ToString() => string.Format( "{{R:{0} G:{1} B:{2} A:{3}}}", (object) this.R, (object) this.G,
    (object) this.B, (object) this.A );

  public bool Equals( ColorRGBA other ) => (int) this.R == (int) other.R && (int) this.G == (int) other.G &&
                                           (int) this.B == (int) other.B && (int) this.A == (int) other.A;

  public ColorRGBA SetA( byte a ) => new ColorRGBA( this.R, this.G, this.B, a );

  public class EqualityComparer : IEqualityComparer<ColorRGBA>
  {
    public static readonly ColorRGBA.EqualityComparer Instance = new ColorRGBA.EqualityComparer();

    private EqualityComparer()
    {
    }

    bool IEqualityComparer<ColorRGBA>.Equals( ColorRGBA x, ColorRGBA y ) => x.Equals( y );

    int IEqualityComparer<ColorRGBA>.GetHashCode( ColorRGBA x ) => x.GetHashCode();
  }
  
}
