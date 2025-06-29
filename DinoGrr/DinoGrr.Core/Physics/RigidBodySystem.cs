using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DinoGrr.Core.Events;
using Color = Microsoft.Xna.Framework.Color;

namespace DinoGrr.Core.Physics;

/// <summary>
/// Manages rigid body physics simulation and collisions.
/// Works alongside the VerletSystem to provide hybrid physics.
/// </summary>
public class RigidBodySystem
{
    /// <summary>
    /// List of all rigid bodies in the system.
    /// </summary>
    private readonly List<RigidBody> _rigidBodies = new List<RigidBody>();

    /// <summary>
    /// Gravity vector applied to all rigid bodies.
    /// </summary>
    public Vector2 Gravity { get; set; }

    /// <summary>
    /// Screen bounds for constraint.
    /// </summary>
    private readonly RectangleF _bounds;

    /// <summary>
    /// Reference to the Verlet system for hybrid collisions.
    /// </summary>
    private readonly VerletSystem _verletSystem;

    /// <summary>
    /// Event triggered when rigid body collisions occur.
    /// </summary>
    public event EventHandler<CollisionEventArgs> Collision;

    /// <summary>
    /// Creates a new rigid body physics system.
    /// </summary>
    /// <param name="screenWidth">Screen width for bounds.</param>
    /// <param name="screenHeight">Screen height for bounds.</param>
    /// <param name="verletSystem">Reference to Verlet system for hybrid physics.</param>
    /// <param name="gravity">Gravity vector.</param>
    public RigidBodySystem(int screenWidth, int screenHeight, VerletSystem verletSystem, Vector2? gravity = null)
    {
        _bounds = new RectangleF(0, 0, screenWidth, screenHeight);
        _verletSystem = verletSystem ?? throw new ArgumentNullException(nameof(verletSystem));
        Gravity = gravity ?? new Vector2(0, 9.8f * 15);
    }

    /// <summary>
    /// Adds a rigid body to the system.
    /// </summary>
    /// <param name="rigidBody">The rigid body to add.</param>
    public void AddRigidBody(RigidBody rigidBody)
    {
        if (rigidBody != null && !_rigidBodies.Contains(rigidBody))
        {
            _rigidBodies.Add(rigidBody);
        }
    }

    /// <summary>
    /// Removes a rigid body from the system.
    /// </summary>
    /// <param name="rigidBody">The rigid body to remove.</param>
    public void RemoveRigidBody(RigidBody rigidBody)
    {
        _rigidBodies.Remove(rigidBody);
    }

    /// <summary>
    /// Creates a rigid body from a drawing and adds it to the system.
    /// </summary>
    /// <param name="points">Drawing points in world coordinates.</param>
    /// <param name="color">Color for the rigid body.</param>
    /// <param name="lineThickness">Line thickness for rendering.</param>
    /// <param name="density">Density for mass calculation.</param>
    /// <returns>The created rigid body.</returns>
    public RigidBody CreateRigidBodyFromDrawing(List<Vector2> points, Color color,
                                                float lineThickness = 2f, float density = 1f)
    {
        if (points == null || points.Count < 3)
            return null;

        var rigidBody = new RigidBody(points, color, lineThickness, density);
        AddRigidBody(rigidBody);
        return rigidBody;
    }

    /// <summary>
    /// Updates all rigid bodies in the system.
    /// </summary>
    /// <param name="deltaTime">Time step.</param>
    public void Update(float deltaTime, int subSteps = 4)
    {
        float subDeltaTime = deltaTime / subSteps;

        for (int step = 0; step < subSteps; step++)
        {
            // Update physics for all rigid bodies
            foreach (var rigidBody in _rigidBodies)
            {
                rigidBody.Update(subDeltaTime, Gravity);
            }

            // Resolve rigid body to rigid body collisions
            ResolveRigidBodyCollisions();

            // Resolve rigid body to Verlet point collisions
            ResolveRigidBodyVerletCollisions();

            // Apply boundary constraints
            ApplyBoundaryConstraints();
        }
    }

    /// <summary>
    /// Draws all rigid bodies in the system.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch for drawing.</param>
    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var rigidBody in _rigidBodies)
        {
            rigidBody.Draw(spriteBatch);
        }
    }

    /// <summary>
    /// Gets all rigid bodies in the system.
    /// </summary>
    public IReadOnlyList<RigidBody> RigidBodies => _rigidBodies.AsReadOnly();

    /// <summary>
    /// Resolves collisions between rigid bodies.
    /// </summary>
    private void ResolveRigidBodyCollisions()
    {
        for (int i = 0; i < _rigidBodies.Count; i++)
        {
            for (int j = i + 1; j < _rigidBodies.Count; j++)
            {
                var bodyA = _rigidBodies[i];
                var bodyB = _rigidBodies[j];

                // Skip if both bodies are fixed
                if (bodyA.IsFixed && bodyB.IsFixed) continue;

                // Broad phase collision detection using AABB
                if (AABB.Intersects(bodyA.AABB, bodyB.AABB))
                {
                    // Narrow phase collision detection using SAT
                    if (CheckCollisionSAT(bodyA, bodyB, out Vector2 normal, out float depth, out Vector2 contactPoint))
                    {
                        ResolveCollision(bodyA, bodyB, normal, depth, contactPoint);

                        // Trigger collision event
                        Collision?.Invoke(this, new CollisionEventArgs(
                            CollisionType.RigidBodyToRigidBody,
                            contactPoint,
                            normal,
                            depth
                        ));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Resolves collisions between rigid bodies and Verlet points.
    /// </summary>
    private void ResolveRigidBodyVerletCollisions()
    {
        // Get all Verlet points from the system
        var verletPoints = GetVerletPoints();

        foreach (var rigidBody in _rigidBodies)
        {
            foreach (var point in verletPoints)
            {
                if (IsPointInsideRigidBody(point.Position, rigidBody, out Vector2 normal, out float depth))
                {
                    ResolveRigidBodyPointCollision(rigidBody, point, normal, depth);

                    // Trigger collision event
                    Collision?.Invoke(this, new CollisionEventArgs(
                        CollisionType.RigidBodyToVerletPoint,
                        point.Position,
                        normal,
                        depth
                    ));
                }
            }
        }
    }

    /// <summary>
    /// Checks collision between two rigid bodies using Separating Axis Theorem.
    /// </summary>
    private bool CheckCollisionSAT(RigidBody bodyA, RigidBody bodyB, out Vector2 normal,
                                   out float depth, out Vector2 contactPoint)
    {
        normal = Vector2.Zero;
        depth = float.MaxValue;
        contactPoint = Vector2.Zero;

        var pointsA = bodyA.GetWorldPoints();
        var pointsB = bodyB.GetWorldPoints();

        // Test all axes from both polygons
        var axes = new List<Vector2>();

        // Get axes from body A
        for (int i = 0; i < pointsA.Count; i++)
        {
            var edge = pointsA[(i + 1) % pointsA.Count] - pointsA[i];
            var axis = new Vector2(-edge.Y, edge.X); // Perpendicular
            if (axis.LengthSquared() > 1e-6f)
            {
                axes.Add(Vector2.Normalize(axis));
            }
        }

        // Get axes from body B
        for (int i = 0; i < pointsB.Count; i++)
        {
            var edge = pointsB[(i + 1) % pointsB.Count] - pointsB[i];
            var axis = new Vector2(-edge.Y, edge.X); // Perpendicular
            if (axis.LengthSquared() > 1e-6f)
            {
                axes.Add(Vector2.Normalize(axis));
            }
        }

        // Test each axis
        foreach (var axis in axes)
        {
            // Project both polygons onto the axis
            var projA = ProjectPolygon(pointsA, axis);
            var projB = ProjectPolygon(pointsB, axis);

            // Check for separation
            if (projA.Max < projB.Min || projB.Max < projA.Min)
            {
                return false; // Separating axis found, no collision
            }

            // Calculate overlap
            float overlap = Math.Min(projA.Max - projB.Min, projB.Max - projA.Min);
            if (overlap < depth)
            {
                depth = overlap;
                normal = axis;
            }
        }

        // Ensure normal points from A to B
        Vector2 centerA = bodyA.Position;
        Vector2 centerB = bodyB.Position;
        if (Vector2.Dot(normal, centerB - centerA) < 0)
        {
            normal = -normal;
        }

        // Calculate approximate contact point
        contactPoint = (centerA + centerB) * 0.5f;

        return true; // Collision detected
    }

    /// <summary>
    /// Projects a polygon onto an axis.
    /// </summary>
    private (float Min, float Max) ProjectPolygon(List<Vector2> points, Vector2 axis)
    {
        float min = Vector2.Dot(points[0], axis);
        float max = min;

        for (int i = 1; i < points.Count; i++)
        {
            float projection = Vector2.Dot(points[i], axis);
            if (projection < min) min = projection;
            if (projection > max) max = projection;
        }

        return (min, max);
    }

    /// <summary>
    /// Resolves collision between two rigid bodies.
    /// </summary>
    private void ResolveCollision(RigidBody bodyA, RigidBody bodyB, Vector2 normal, float depth, Vector2 contactPoint)
    {
        // Separate the bodies
        Vector2 separation = normal * depth;

        if (!bodyA.IsFixed && !bodyB.IsFixed)
        {
            float totalMass = bodyA.Mass + bodyB.Mass;
            bodyA.Position -= separation * (bodyB.Mass / totalMass);
            bodyB.Position += separation * (bodyA.Mass / totalMass);
        }
        else if (!bodyA.IsFixed)
        {
            bodyA.Position -= separation;
        }
        else if (!bodyB.IsFixed)
        {
            bodyB.Position += separation;
        }

        // Calculate relative velocity at contact point
        Vector2 relativeVelocity = GetVelocityAtPoint(bodyB, contactPoint) - GetVelocityAtPoint(bodyA, contactPoint);
        float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

        // Don't resolve if velocities are separating
        if (velocityAlongNormal > 0) return;

        // Calculate restitution
        float restitution = Math.Min(bodyA.Restitution, bodyB.Restitution);

        // Calculate impulse scalar
        float impulseScalar = -(1 + restitution) * velocityAlongNormal;

        // Calculate mass terms
        float massTermA = bodyA.IsFixed ? 0 : (1f / bodyA.Mass);
        float massTermB = bodyB.IsFixed ? 0 : (1f / bodyB.Mass);

        Vector2 rA = contactPoint - bodyA.Position;
        Vector2 rB = contactPoint - bodyB.Position;

        float angularTermA = bodyA.IsFixed ? 0 : (rA.X * normal.Y - rA.Y * normal.X) * (rA.X * normal.Y - rA.Y * normal.X) / bodyA.MomentOfInertia;
        float angularTermB = bodyB.IsFixed ? 0 : (rB.X * normal.Y - rB.Y * normal.X) * (rB.X * normal.Y - rB.Y * normal.X) / bodyB.MomentOfInertia;

        impulseScalar /= (massTermA + massTermB + angularTermA + angularTermB);

        // Apply impulse
        Vector2 impulse = impulseScalar * normal;

        if (!bodyA.IsFixed)
        {
            bodyA.ApplyImpulse(-impulse, contactPoint);
        }
        if (!bodyB.IsFixed)
        {
            bodyB.ApplyImpulse(impulse, contactPoint);
        }
    }

    /// <summary>
    /// Gets velocity at a specific point on a rigid body.
    /// </summary>
    private Vector2 GetVelocityAtPoint(RigidBody body, Vector2 point)
    {
        Vector2 r = point - body.Position;
        return body.Velocity + new Vector2(-r.Y, r.X) * body.AngularVelocity;
    }

    /// <summary>
    /// Checks if a point is inside a rigid body.
    /// </summary>
    private bool IsPointInsideRigidBody(Vector2 point, RigidBody rigidBody, out Vector2 normal, out float depth)
    {
        normal = Vector2.Zero;
        depth = 0f;

        var worldPoints = rigidBody.GetWorldPoints();
        if (worldPoints.Count < 2) return false;

        // For lines (open shapes), check distance to line segments
        if (worldPoints.Count == 2 || !IsPointInPolygon(point, worldPoints))
        {
            return CheckPointToLineCollision(point, worldPoints, out normal, out depth);
        }

        // For closed polygons, use the existing method
        bool inside = IsPointInPolygon(point, worldPoints);

        if (inside)
        {
            // Find the closest edge and calculate normal and penetration depth
            float minDistance = float.MaxValue;
            Vector2 closestNormal = Vector2.Zero;

            for (int i = 0; i < worldPoints.Count; i++)
            {
                var p1 = worldPoints[i];
                var p2 = worldPoints[(i + 1) % worldPoints.Count];

                var edge = p2 - p1;
                if (edge.LengthSquared() < 1e-6f) continue; // Skip degenerate edges

                var edgeNormal = Vector2.Normalize(new Vector2(-edge.Y, edge.X));

                // Distance from point to edge
                float distance = Math.Abs(Vector2.Dot(point - p1, edgeNormal));

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestNormal = edgeNormal;
                }
            }

            normal = closestNormal;
            depth = minDistance;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks collision between a point and line segments (for open shapes).
    /// </summary>
    private bool CheckPointToLineCollision(Vector2 point, List<Vector2> linePoints, out Vector2 normal, out float depth)
    {
        normal = Vector2.Zero;
        depth = 0f;

        float minDistance = float.MaxValue;
        Vector2 closestNormal = Vector2.Zero;
        bool hasCollision = false;

        const float collisionThreshold = 15f; // Increased threshold for easier standing

        // Check distance to each line segment
        for (int i = 0; i < linePoints.Count - 1; i++)
        {
            var p1 = linePoints[i];
            var p2 = linePoints[i + 1];

            // Find closest point on line segment to the point
            Vector2 edge = p2 - p1;
            float edgeLength = edge.Length();
            if (edgeLength < 1e-6f) continue;

            Vector2 edgeDir = edge / edgeLength;
            Vector2 toPoint = point - p1;

            // Project point onto line segment
            float projection = Vector2.Dot(toPoint, edgeDir);
            projection = MathHelper.Clamp(projection, 0f, edgeLength);

            Vector2 closestPointOnLine = p1 + edgeDir * projection;
            Vector2 pointToLine = point - closestPointOnLine;
            float distance = pointToLine.Length();

            // Check if within collision threshold
            if (distance < collisionThreshold && distance < minDistance)
            {
                minDistance = distance;
                hasCollision = true;

                // Calculate normal pointing away from the line
                if (distance > 1e-6f)
                {
                    closestNormal = Vector2.Normalize(pointToLine);
                }
                else
                {
                    // Point is exactly on the line, use perpendicular to edge
                    closestNormal = Vector2.Normalize(new Vector2(-edge.Y, edge.X));
                }
            }
        }

        if (hasCollision)
        {
            normal = closestNormal;
            depth = collisionThreshold - minDistance; // How much to push out
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a point is inside a polygon using ray casting.
    /// </summary>
    private bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        bool inside = false;
        int j = polygon.Count - 1;

        for (int i = 0; i < polygon.Count; i++)
        {
            if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
            {
                inside = !inside;
            }
            j = i;
        }

        return inside;
    }

    /// <summary>
    /// Resolves collision between a rigid body and a Verlet point.
    /// </summary>
    private void ResolveRigidBodyPointCollision(RigidBody rigidBody, VerletPoint point, Vector2 normal, float depth)
    {
        // Only resolve if penetration is significant enough
        const float minResolutionDepth = 2f;
        if (depth < minResolutionDepth) return;

        // Gentle separation - don't push too hard
        float separationAmount = Math.Min(depth * 0.8f, point.Radius); // Cap separation
        Vector2 separation = normal * separationAmount;
        point.Position += separation;

        // Calculate relative velocity
        Vector2 pointVelocity = point.GetVelocity();
        Vector2 rigidBodyVelocity = GetVelocityAtPoint(rigidBody, point.Position);
        Vector2 relativeVelocity = pointVelocity - rigidBodyVelocity;

        float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

        // Allow resting contacts - only resolve if approaching
        if (velocityAlongNormal > -1f) return; // Very small threshold for resting

        // Much lower restitution for better standing
        float restitution = Math.Min(rigidBody.Restitution * 0.3f, 0.2f); // Reduced restitution
        float impulseScalar = -(1 + restitution) * velocityAlongNormal;

        float massTermPoint = 1f / point.Mass;
        float massTermRigid = rigidBody.IsFixed ? 0 : (1f / rigidBody.Mass);

        impulseScalar /= (massTermPoint + massTermRigid);

        // Reduce impulse magnitude for gentler collisions
        impulseScalar *= 0.7f;

        Vector2 impulse = impulseScalar * normal;

        // Apply impulse to point
        point.AdjustVelocity(impulse / point.Mass);

        // Apply much weaker impulse to rigid body to prevent excessive movement
        if (!rigidBody.IsFixed)
        {
            rigidBody.ApplyImpulse(-impulse * 0.5f, point.Position);
        }

        // Add friction for better resting
        Vector2 tangent = relativeVelocity - Vector2.Dot(relativeVelocity, normal) * normal;
        if (tangent.LengthSquared() > 1e-6f)
        {
            tangent = Vector2.Normalize(tangent);
            float frictionImpulse = Math.Min(Math.Abs(impulseScalar) * 0.3f, tangent.Length());
            Vector2 frictionForce = -tangent * frictionImpulse;

            point.AdjustVelocity(frictionForce / point.Mass);
        }
    }

    /// <summary>
    /// Gets all Verlet points from the Verlet system.
    /// </summary>
    private List<VerletPoint> GetVerletPoints()
    {
        return _verletSystem.GetAllPoints().ToList();
    }

    /// <summary>
    /// Applies boundary constraints to all rigid bodies.
    /// </summary>
    private void ApplyBoundaryConstraints()
    {
        foreach (var rigidBody in _rigidBodies)
        {
            rigidBody.ConstrainToBounds(_bounds.Width, _bounds.Height);
        }
    }
}
