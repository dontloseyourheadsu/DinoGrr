using DinoGrr.Core.Physics;
using Microsoft.Xna.Framework;

namespace DinoGrr.Core.Events;

/// <summary>
/// Event arguments for collision events.
/// </summary>
public class CollisionEventArgs
{
    /// <summary>
    /// Gets the type of collision that occurred.
    /// </summary>
    public CollisionType CollisionType { get; }

    /// <summary>
    /// Gets the first point involved in the collision.
    /// </summary>
    public VerletPoint Point1 { get; }

    /// <summary>
    /// Gets the second point involved in the collision (may be null for non-point collisions).
    /// </summary>
    public VerletPoint Point2 { get; }

    /// <summary>
    /// Gets the first soft body involved in the collision (may be null for point-only collisions).
    /// </summary>
    public SoftBody SoftBody1 { get; }

    /// <summary>
    /// Gets the second soft body involved in the collision (may be null for point-only collisions).
    /// </summary>
    public SoftBody SoftBody2 { get; }

    /// <summary>
    /// Gets the collision normal vector.
    /// </summary>
    public Vector2 Normal { get; }

    /// <summary>
    /// Gets the collision impulse magnitude.
    /// </summary>
    public float ImpulseMagnitude { get; }

    /// <summary>
    /// Gets the starting point of the edge involved in the collision (if applicable).
    /// </summary>
    public VerletPoint EdgeStart { get; }

    /// <summary>
    /// Gets the ending point of the edge involved in the collision (if applicable).
    /// </summary>
    public VerletPoint EdgeEnd { get; }

    /// <summary>
    /// Creates a new instance of CollisionEventArgs for point-to-point collisions.
    /// </summary>
    /// <param name="point"> The first point involved in the collision.</param>
    /// <param name="edgeStart">The starting point of the edge involved in the collision (if applicable).</param>
    /// <param name="edgeEnd">The ending point of the edge involved in the collision (if applicable).</param>
    /// <param name="normal">The collision normal vector.</param>
    /// <param name="impulseMagnitude">The magnitude of the collision impulse.</param>
    public CollisionEventArgs(VerletPoint point, VerletPoint edgeStart, VerletPoint edgeEnd,
    Vector2 normal, float impulseMagnitude)
    {
        CollisionType = CollisionType.PointToEdge;
        Point1 = point;
        EdgeStart = edgeStart;
        EdgeEnd = edgeEnd;
        Normal = normal;
        ImpulseMagnitude = impulseMagnitude;

        // Find the soft body this point belongs to (if any)
        SoftBody1 = point.OwnerSoftBody;

        // Find the soft body the edge belongs to (if any)
        // Assume both edge points belong to the same soft body
        SoftBody2 = edgeStart.OwnerSoftBody;
    }

    /// <summary>
    /// Creates a new instance of CollisionEventArgs for point-to-point collisions.
    /// </summary>
    /// <param name="point1">The first point involved in the collision.</param>
    /// <param name="point2">The second point involved in the collision.</param>
    /// <param name="normal">The collision normal vector.</param>
    /// <param name="impulseMagnitude">The magnitude of the collision impulse.</param>
    public CollisionEventArgs(VerletPoint point1, VerletPoint point2, Vector2 normal, float impulseMagnitude)
    {
        CollisionType = CollisionType.PointToPoint;
        Point1 = point1;
        Point2 = point2;
        Normal = normal;
        ImpulseMagnitude = impulseMagnitude;

        // Find the soft bodies these points belong to (if any)
        SoftBody1 = point1.OwnerSoftBody;
        SoftBody2 = point2.OwnerSoftBody;
    }

    /// <summary>
    /// Creates a new instance of CollisionEventArgs for soft body overlaps.
    /// </summary>
    public CollisionEventArgs(SoftBody body1, SoftBody body2, Vector2 normal, float depth)
    {
        CollisionType = CollisionType.SoftBodyOverlap;
        SoftBody1 = body1;
        SoftBody2 = body2;
        Normal = normal;
        ImpulseMagnitude = depth;
    }
}