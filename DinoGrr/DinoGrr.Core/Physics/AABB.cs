using Microsoft.Xna.Framework;
using System;

namespace DinoGrr.Core.Physics;

/// <summary>
/// Represents an Axis-Aligned Bounding Box for broad-phase collision detection.
/// </summary>
public struct AABB
{
  public Vector2 Min;
  public Vector2 Max;

  public static bool Intersects(AABB a, AABB b)
  {
    return !(a.Max.X < b.Min.X || a.Min.X > b.Max.X ||
             a.Max.Y < b.Min.Y || a.Min.Y > b.Max.Y);
  }

  /// <summary>
  /// Creates an AABB from a center point and size.
  /// </summary>
  public static AABB FromCenterAndSize(Vector2 center, Vector2 size)
  {
    Vector2 halfSize = size * 0.5f;
    return new AABB
    {
      Min = center - halfSize,
      Max = center + halfSize
    };
  }

  /// <summary>
  /// Creates an AABB that encompasses both input AABBs.
  /// </summary>
  public static AABB Union(AABB a, AABB b)
  {
    return new AABB
    {
      Min = Vector2.Min(a.Min, b.Min),
      Max = Vector2.Max(a.Max, b.Max)
    };
  }

  /// <summary>
  /// Gets the center point of the AABB.
  /// </summary>
  public Vector2 Center => (Min + Max) * 0.5f;

  /// <summary>
  /// Gets the size of the AABB.
  /// </summary>
  public Vector2 Size => Max - Min;

  /// <summary>
  /// Gets the area of the AABB.
  /// </summary>
  public float Area => Size.X * Size.Y;

  /// <summary>
  /// Expands the AABB by the specified amount in all directions.
  /// </summary>
  public AABB Expand(float amount)
  {
    Vector2 expansion = new Vector2(amount);
    return new AABB
    {
      Min = Min - expansion,
      Max = Max + expansion
    };
  }

  /// <summary>
  /// Checks if this AABB contains the specified point.
  /// </summary>
  public bool Contains(Vector2 point)
  {
    return point.X >= Min.X && point.X <= Max.X &&
           point.Y >= Min.Y && point.Y <= Max.Y;
  }

  /// <summary>
  /// Checks if this AABB is valid (Min <= Max).
  /// </summary>
  public bool IsValid => Min.X <= Max.X && Min.Y <= Max.Y;
}