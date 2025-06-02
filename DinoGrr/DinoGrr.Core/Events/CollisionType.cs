namespace DinoGrr.Core.Events;

/// <summary>
/// Defines the type of collision that occurred.
/// </summary>
public enum CollisionType
{
    PointToPoint,
    PointToEdge,
    SoftBodyOverlap
}
