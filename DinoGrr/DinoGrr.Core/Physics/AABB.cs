using Microsoft.Xna.Framework;

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
}