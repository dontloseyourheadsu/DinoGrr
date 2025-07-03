using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using System.Drawing;
using Color = Microsoft.Xna.Framework.Color;
using DinoGrr.Core.Events;
using DinoGrr.Core.Rendering;

namespace DinoGrr.Core.Physics;

public class VerletSystem
{
    /// <summary>
    /// List of Verlet points in the system.
    /// </summary>
    private readonly List<VerletPoint> _points;

    /// <summary>
    /// List of Verlet springs (optional).
    /// </summary>
    private readonly List<VerletSpring> _springs = new();

    /// <summary>
    /// List of all SoftBodies in the system for collision detection.
    /// </summary>
    private readonly List<SoftBody> _softBodies = new();

    /// <summary>
    /// Gravity vector for the system.
    /// </summary>
    private readonly Vector2 _gravity;

    /// <summary>
    /// Screen bounds for the system.
    /// </summary>
    private RectangleF _bounds;

    /// <summary>
    /// Damping factor for collisions (0.0 to 1.0).
    /// </summary>
    private readonly float _dampingFactor;

    /// <summary>
    /// Event that is triggered when a collision occurs.
    /// </summary>
    public event EventHandler<CollisionEventArgs> Collision;

    /// <summary>
    /// Diagnostic system for monitoring physics performance.
    /// </summary>
    public PhysicsDiagnostics Diagnostics { get; private set; } = new PhysicsDiagnostics();

    /// <summary>
    /// Counter for collision detection this frame.
    /// </summary>
    private int _currentFrameCollisions = 0;

    /// <summary>
    /// Creates a new Verlet physics system.
    /// </summary>
    /// <param name="screenWidth">Width of the screen.</param>
    /// <param name="screenHeight">Height of the screen.</param>
    /// <param name="gravity">Gravity vector (defaults to PhysicsConfig value).</param>
    /// <param name="dampingFactor">Damping factor (defaults to PhysicsConfig value).</param>
    public VerletSystem(int screenWidth, int screenHeight, Vector2? gravity = null, float? dampingFactor = null)
    {
        this._points = new List<VerletPoint>();
        this._gravity = gravity ?? PhysicsConfig.Gravity;
        this._bounds = new RectangleF(0, 0, screenWidth, screenHeight);
        this._dampingFactor = dampingFactor ?? PhysicsConfig.GlobalDamping;
    }

    /// <summary>
    /// Adds an existing Verlet point to the system.
    /// </summary>
    public void AddPoint(VerletPoint point)
    {
        _points.Add(point);
    }

    /// <summary>
    /// Gets all Verlet points in the system.
    /// </summary>
    public IReadOnlyList<VerletPoint> GetAllPoints()
    {
        return _points.AsReadOnly();
    }

    /// <summary>
    /// Creates and adds a new Verlet point to the system.
    /// </summary>
    public VerletPoint CreatePoint(Vector2 position, float radius, float mass, Color color, bool isFixed = false)
    {
        var point = new VerletPoint(position, radius, mass, color, isFixed);
        _points.Add(point);
        return point;
    }

    /// <summary>
    /// Creates a spring between two Verlet points.
    /// </summary>
    public VerletSpring CreateSpring(VerletPoint p1, VerletPoint p2, float stiffness = 1f, float thickness = 2f, Color color = default)
    {
        var s = new VerletSpring(p1, p2, stiffness, thickness, color);
        _springs.Add(s);
        return s;
    }

    /// <summary>
    /// Registers a SoftBody with the system for collision handling.
    /// </summary>
    /// <param name="body">The SoftBody to register.</param>
    public void RegisterSoftBody(SoftBody body)
    {
        if (!_softBodies.Contains(body))
        {
            _softBodies.Add(body);
        }
    }

    /// <summary>
    /// Updates the physics of all points in the system.
    /// </summary>
    public void Update(float deltaTime, int? subSteps = null)
    {
        var startTime = DateTime.UtcNow;
        _currentFrameCollisions = 0;

        int actualSubSteps = subSteps ?? PhysicsConfig.DefaultSubSteps;

        // Adaptive sub-stepping to prevent tunneling
        float maxVelocity = GetMaxVelocity();
        float minRadius = GetMinRadius();

        // Calculate required sub-steps based on velocity and object size
        if (maxVelocity > 0 && minRadius > 0)
        {
            int requiredSubSteps = Math.Max(actualSubSteps, (int)Math.Ceiling(maxVelocity * deltaTime / (minRadius * 0.5f)));
            actualSubSteps = Math.Min(requiredSubSteps, PhysicsConfig.MaxSubSteps);
        }

        float subDeltaTime = deltaTime / actualSubSteps;

        for (int step = 0; step < actualSubSteps; step++)
        {
            ApplyForces();
            UpdatePoints(subDeltaTime);
            ApplyVelocityDamping();

            // Multiple constraint satisfaction iterations to improve stability
            for (int i = 0; i < PhysicsConfig.ConstraintIterations; i++)
            {
                SatisfySprings(PhysicsConfig.SpringIterations);
                ResolveCollisions();
                ResolveSoftBodyCollisions();
                ApplyConstraints();
            }
        }

        // Update diagnostics
        var frameTime = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
        Diagnostics.UpdateFrame(frameTime, _currentFrameCollisions, maxVelocity);
    }

    /// <summary>
    /// Gets the maximum velocity of all non-fixed points in the system.
    /// </summary>
    private float GetMaxVelocity()
    {
        float maxVel = 0f;
        foreach (var point in _points)
        {
            if (!point.IsFixed)
            {
                float vel = point.GetVelocity().Length();
                if (vel > maxVel) maxVel = vel;
            }
        }
        return maxVel;
    }

    /// <summary>
    /// Gets the minimum radius of all points in the system.
    /// </summary>
    private float GetMinRadius()
    {
        if (_points.Count == 0) return 1f;

        float minRadius = float.MaxValue;
        foreach (var point in _points)
        {
            if (point.Radius < minRadius) minRadius = point.Radius;
        }
        return minRadius == float.MaxValue ? 1f : minRadius;
    }

    /// <summary>
    /// Applies external forces (e.g., gravity) to all points.
    /// </summary>
    private void ApplyForces()
    {
        foreach (var point in _points)
        {
            point.ApplyForce(_gravity * point.Mass);
        }
    }

    /// <summary>
    /// Updates the position of all points.
    /// </summary>
    private void UpdatePoints(float deltaTime)
    {
        foreach (var point in _points)
        {
            point.Update(deltaTime);
        }
    }

    /// <summary>
    /// Resolves collisions between all points.
    /// </summary>
    private void ResolveCollisions()
    {
        // Use spatial hashing for better performance with many objects
        for (int i = 0; i < _points.Count; i++)
        {
            for (int j = i + 1; j < _points.Count; j++)
            {
                VerletPoint p1 = _points[i];
                VerletPoint p2 = _points[j];

                Vector2 delta = p2.Position - p1.Position;
                float distanceSquared = delta.LengthSquared();

                float minDistance = p1.Radius + p2.Radius;
                float minDistanceSquared = minDistance * minDistance;

                if (distanceSquared < minDistanceSquared && distanceSquared > PhysicsConfig.MinDistanceThreshold)
                {
                    float distance = (float)Math.Sqrt(distanceSquared);
                    Vector2 direction = delta / distance;
                    float overlap = minDistance - distance;

                    // Use inverse mass for more realistic collision response
                    float invMass1 = p1.IsFixed ? 0 : 1.0f / p1.Mass;
                    float invMass2 = p2.IsFixed ? 0 : 1.0f / p2.Mass;
                    float totalInvMass = invMass1 + invMass2;

                    if (totalInvMass > 0)
                    {
                        Vector2 v1 = p1.GetVelocity();
                        Vector2 v2 = p2.GetVelocity();
                        Vector2 relativeVelocity = v2 - v1;
                        float velocityAlongNormal = Vector2.Dot(relativeVelocity, direction);

                        // Calculate impulse magnitude
                        float restitution = _dampingFactor;
                        float impulseMagnitude = -(1.0f + restitution) * velocityAlongNormal / totalInvMass;

                        // Fire collision event
                        Collision?.Invoke(this, new CollisionEventArgs(p1, p2, direction, MathF.Abs(impulseMagnitude)));
                        _currentFrameCollisions++;

                        // Position correction with stability improvements
                        float correctionPercent = PhysicsConfig.PositionCorrectionPercent;
                        float slop = PhysicsConfig.PositionSlop;
                        Vector2 correction = direction * Math.Max(overlap - slop, 0.0f) * correctionPercent / totalInvMass;

                        if (!p1.IsFixed)
                        {
                            p1.Position -= correction * invMass1;
                        }

                        if (!p2.IsFixed)
                        {
                            p2.Position += correction * invMass2;
                        }

                        // Apply velocity correction only if objects are approaching
                        if (velocityAlongNormal < 0)
                        {
                            Vector2 impulse = direction * impulseMagnitude;

                            if (!p1.IsFixed)
                            {
                                p1.AdjustVelocity(-impulse * invMass1);
                            }

                            if (!p2.IsFixed)
                            {
                                p2.AdjustVelocity(impulse * invMass2);
                            }

                            // Add friction for more realistic interactions
                            ApplyFriction(p1, p2, direction, impulseMagnitude);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Applies friction between two colliding points.
    /// </summary>
    private void ApplyFriction(VerletPoint p1, VerletPoint p2, Vector2 normal, float normalImpulse)
    {
        Vector2 v1 = p1.GetVelocity();
        Vector2 v2 = p2.GetVelocity();
        Vector2 relativeVelocity = v2 - v1;

        // Calculate tangent direction
        Vector2 tangent = relativeVelocity - Vector2.Dot(relativeVelocity, normal) * normal;
        if (tangent.LengthSquared() < PhysicsConfig.MinDistanceThreshold) return;

        tangent = Vector2.Normalize(tangent);

        // Determine friction coefficient based on surface types
        float frictionCoefficient = GetFrictionBetweenObjects(p1, p2);

        // Use surface-specific friction coefficient
        float frictionImpulse = -Vector2.Dot(relativeVelocity, tangent);

        float invMass1 = p1.IsFixed ? 0 : 1.0f / p1.Mass;
        float invMass2 = p2.IsFixed ? 0 : 1.0f / p2.Mass;
        frictionImpulse /= (invMass1 + invMass2);

        // Coulomb friction model
        if (Math.Abs(frictionImpulse) < normalImpulse * frictionCoefficient)
        {
            // Static friction
            Vector2 frictionForce = tangent * frictionImpulse;

            if (!p1.IsFixed)
                p1.AdjustVelocity(-frictionForce * invMass1);
            if (!p2.IsFixed)
                p2.AdjustVelocity(frictionForce * invMass2);
        }
        else
        {
            // Kinetic friction
            float sign = frictionImpulse < 0 ? -1 : 1;
            Vector2 frictionForce = tangent * normalImpulse * frictionCoefficient * sign;

            if (!p1.IsFixed)
                p1.AdjustVelocity(-frictionForce * invMass1);
            if (!p2.IsFixed)
                p2.AdjustVelocity(frictionForce * invMass2);
        }
    }

    /// <summary>
    /// Determines the friction coefficient between two objects based on their surface types.
    /// </summary>
    private float GetFrictionBetweenObjects(VerletPoint p1, VerletPoint p2)
    {
        // Get surface tags from the points or their owner soft bodies
        string surface1 = GetSurfaceTag(p1);
        string surface2 = GetSurfaceTag(p2);

        // Get friction for each surface
        float friction1 = PhysicsConfig.GetSurfaceFriction(surface1);
        float friction2 = PhysicsConfig.GetSurfaceFriction(surface2);

        // Use the minimum friction (most slippery surface dominates)
        return Math.Min(friction1, friction2);
    }

    /// <summary>
    /// Gets the surface tag for a point, checking the point's tag and its owner's tag.
    /// </summary>
    private string GetSurfaceTag(VerletPoint point)
    {
        // First check the point's own tag
        if (!string.IsNullOrEmpty(point.Tag))
            return point.Tag;

        // Then check the owner soft body's tag
        if (point.OwnerSoftBody != null && !string.IsNullOrEmpty(point.OwnerSoftBody.Tag))
            return point.OwnerSoftBody.Tag;

        // Default to empty string (will use default friction)
        return string.Empty;
    }

    /// <summary>
    /// Applies boundary constraints to all points.
    /// </summary>
    private void ApplyConstraints()
    {
        foreach (var point in _points)
        {
            bool collided = point.ConstrainToBounds(_bounds.Width, _bounds.Height, _dampingFactor);

            // If this point belongs to a softbody and collided with a boundary, fire a collision event
            if (collided && point.OwnerSoftBody != null)
            {
                // Determine which boundary was hit by checking position against bounds
                Vector2 normal = Vector2.Zero;
                float impulseMagnitude = 0;

                if (point.Position.Y >= _bounds.Height - point.Radius)
                {
                    // Ground collision
                    normal = new Vector2(0, -1);
                    impulseMagnitude = point.GetVelocity().Length() * point.Mass;

                    // Create temp points to represent the ground (without setting owner)
                    VerletPoint groundPoint1 = new VerletPoint(
                        new Vector2(point.Position.X - 50, _bounds.Height),
                        point.Radius, float.MaxValue, Color.White, true);

                    VerletPoint groundPoint2 = new VerletPoint(
                        new Vector2(point.Position.X + 50, _bounds.Height),
                        point.Radius, float.MaxValue, Color.White, true);

                    // Fire collision event for ground
                    Collision?.Invoke(this, new Events.CollisionEventArgs(
                        point, groundPoint1, groundPoint2, normal, impulseMagnitude));
                }
            }
        }
    }

    /// <summary>
    /// Satisfies spring constraints between points.
    /// </summary>
    private void SatisfySprings(int iterations = 1)
    {
        for (int k = 0; k < iterations; k++)
            foreach (var s in _springs)
                s.SatisfyConstraint();
    }

    /// <summary>
    /// Resolves collisions between all softbodies in the system.
    /// </summary>
    private void ResolveSoftBodyCollisions()
    {
        for (int i = 0; i < _softBodies.Count; i++)
        {
            for (int j = i + 1; j < _softBodies.Count; j++)
            {
                SoftBody bodyA = _softBodies[i];
                SoftBody bodyB = _softBodies[j];

                // Quick AABB check before detailed collision
                if (!AABBOverlap(bodyA, bodyB)) continue;

                // Use SAT for detailed collision detection and response
                CheckCollisionSAT(bodyA, bodyB);

                // Additional edge-point collision handling
                HandleEdgePointCollisions(bodyA, bodyB);
            }
        }
    }

    /// <summary>
    /// Checks if two softbodies' bounding boxes overlap.
    /// </summary>
    private bool AABBOverlap(SoftBody bodyA, SoftBody bodyB)
    {
        AABB a = GetBounds(bodyA);
        AABB b = GetBounds(bodyB);

        return !(a.Max.X < b.Min.X || a.Min.X > b.Max.X ||
                 a.Max.Y < b.Min.Y || a.Min.Y > b.Max.Y);
    }

    /// <summary>
    /// Gets the axis-aligned bounding box for a softbody.
    /// </summary>
    private AABB GetBounds(SoftBody body)
    {
        if (body.Points.Count == 0)
            return new AABB { Min = Vector2.Zero, Max = Vector2.Zero };

        Vector2 min = body.Points[0].Position;
        Vector2 max = min;

        foreach (var point in body.Points)
        {
            min.X = MathF.Min(min.X, point.Position.X - point.Radius);
            min.Y = MathF.Min(min.Y, point.Position.Y - point.Radius);
            max.X = MathF.Max(max.X, point.Position.X + point.Radius);
            max.Y = MathF.Max(max.Y, point.Position.Y + point.Radius);
        }

        return new AABB { Min = min, Max = max };
    }

    /// <summary>
    /// Performs Separating Axis Theorem collision detection and response.
    /// </summary>
    private bool CheckCollisionSAT(SoftBody bodyA, SoftBody bodyB)
    {
        // Get axes to test
        List<Vector2> axes = new List<Vector2>();

        // Add edges from bodyA as axes
        for (int i = 0; i < bodyA.Points.Count; i++)
        {
            int nextI = (i + 1) % bodyA.Points.Count;
            Vector2 edge = bodyA.Points[nextI].Position - bodyA.Points[i].Position;
            Vector2 normal = new Vector2(-edge.Y, edge.X);
            if (normal.LengthSquared() > 0.0001f)
            {
                normal.Normalize();
                axes.Add(normal);
            }
        }

        // Add edges from bodyB as axes
        for (int i = 0; i < bodyB.Points.Count; i++)
        {
            int nextI = (i + 1) % bodyB.Points.Count;
            Vector2 edge = bodyB.Points[nextI].Position - bodyB.Points[i].Position;
            Vector2 normal = new Vector2(-edge.Y, edge.X);
            if (normal.LengthSquared() > 0.0001f)
            {
                normal.Normalize();
                axes.Add(normal);
            }
        }

        // Test projection overlap on all axes
        Vector2 minOverlapAxis = Vector2.Zero;
        float minOverlapAmount = float.MaxValue;

        foreach (var axis in axes)
        {
            // Project bodyA onto axis
            float minA = float.MaxValue;
            float maxA = float.MinValue;
            foreach (var point in bodyA.Points)
            {
                float proj = Vector2.Dot(point.Position, axis);
                minA = MathF.Min(minA, proj);
                maxA = MathF.Max(maxA, proj);
            }

            // Project bodyB onto axis
            float minB = float.MaxValue;
            float maxB = float.MinValue;
            foreach (var point in bodyB.Points)
            {
                float proj = Vector2.Dot(point.Position, axis);
                minB = MathF.Min(minB, proj);
                maxB = MathF.Max(maxB, proj);
            }

            // Check for separation
            if (maxA < minB || maxB < minA)
            {
                // Found a separating axis, no collision
                return false;
            }

            // Calculate overlap
            float overlap = MathF.Min(maxA, maxB) - MathF.Max(minA, minB);

            // Track minimum overlap for collision response
            if (overlap < minOverlapAmount)
            {
                minOverlapAmount = overlap;
                minOverlapAxis = axis;

                // Ensure axis points from A to B
                float centerA = (minA + maxA) / 2;
                float centerB = (minB + maxB) / 2;
                if (centerA > centerB)
                {
                    minOverlapAxis = -minOverlapAxis;
                }
            }
        }

        // Apply collision response using minimum translation vector
        ApplySATCollisionResponse(bodyA, bodyB, minOverlapAxis, minOverlapAmount);
        return true;
    }

    /// <summary>
    /// Applies collision response based on Separating Axis Theorem results.
    /// </summary>
    private void ApplySATCollisionResponse(SoftBody bodyA, SoftBody bodyB, Vector2 axis, float depth)
    {
        // Count movable points in each body
        int movablePointsA = 0;
        int movablePointsB = 0;

        foreach (var point in bodyA.Points)
            if (!point.IsFixed) movablePointsA++;

        foreach (var point in bodyB.Points)
            if (!point.IsFixed) movablePointsB++;

        // Calculate response ratio based on movable point count
        float totalPoints = movablePointsA + movablePointsB;
        if (totalPoints == 0) return;

        float ratioA = movablePointsB / totalPoints;
        float ratioB = movablePointsA / totalPoints;

        // Apply displacement to each body
        Vector2 displaceA = axis * depth * ratioA;
        Vector2 displaceB = -axis * depth * ratioB;

        foreach (var point in bodyA.Points)
        {
            if (!point.IsFixed)
                point.Position += displaceA;
        }

        foreach (var point in bodyB.Points)
        {
            if (!point.IsFixed)
                point.Position += displaceB;
        }

        // Fire the softbody collision event
        Collision?.Invoke(this, new CollisionEventArgs(bodyA, bodyB, axis, depth));
    }

    /// <summary>
    /// Handles edge-point collisions between two softbodies.
    /// </summary>
    private void HandleEdgePointCollisions(SoftBody bodyA, SoftBody bodyB)
    {
        // First, check point-vs-edge collisions from bodyA points against bodyB edges
        for (int i = 0; i < bodyA.Points.Count; i++)
        {
            var point = bodyA.Points[i];

            // Check against all edges in bodyB
            for (int j = 0; j < bodyB.Points.Count; j++)
            {
                int nextJ = (j + 1) % bodyB.Points.Count;
                var edgeStart = bodyB.Points[j];
                var edgeEnd = bodyB.Points[nextJ];

                HandlePointEdgeCollision(point, edgeStart, edgeEnd);
            }
        }

        // Then, check point-vs-edge collisions from bodyB points against bodyA edges
        for (int i = 0; i < bodyB.Points.Count; i++)
        {
            var point = bodyB.Points[i];

            // Check against all edges in bodyA
            for (int j = 0; j < bodyA.Points.Count; j++)
            {
                int nextJ = (j + 1) % bodyA.Points.Count;
                var edgeStart = bodyA.Points[j];
                var edgeEnd = bodyA.Points[nextJ];

                HandlePointEdgeCollision(point, edgeStart, edgeEnd);
            }
        }
    }

    /// <summary>
    /// Handles collision between a point and an edge.
    /// </summary>
    private void HandlePointEdgeCollision(VerletPoint point, VerletPoint edgeStart, VerletPoint edgeEnd)
    {
        // Calculate closest point on line segment
        Vector2 edge = edgeEnd.Position - edgeStart.Position;
        float edgeLength = edge.Length();

        // Skip degenerate edges
        if (edgeLength < 0.0001f) return;

        // Normalize edge direction
        Vector2 edgeDir = edge / edgeLength;

        // Calculate vector from edge start to point
        Vector2 pointToEdgeStart = point.Position - edgeStart.Position;

        // Project onto edge
        float projection = Vector2.Dot(pointToEdgeStart, edgeDir);

        // Clamp projection to edge length
        projection = MathHelper.Clamp(projection, 0, edgeLength);

        // Calculate closest point on edge
        Vector2 closestPoint = edgeStart.Position + edgeDir * projection;

        // Calculate vector from closest point to point
        Vector2 normal = point.Position - closestPoint;
        float distance = normal.Length();

        // Skip if outside collision range
        float collisionThreshold = point.Radius;
        if (distance > collisionThreshold || distance < 0.0001f) return;

        // Normalize normal
        normal /= distance;

        // Calculate penetration depth
        float penetration = collisionThreshold - distance;

        // Calculate response strength based on edge vertices' mass vs point mass
        float edgePointMass = (edgeStart.Mass + edgeEnd.Mass) / 2;
        float totalMass = point.Mass + edgePointMass;
        float pointResponse = edgePointMass / totalMass;
        float edgeResponse = point.Mass / totalMass;

        // Store original position for impulse calculation
        Vector2 originalPosition = point.Position;
        float impulseMagnitude = 0;

        // Apply position correction to point if it's not fixed
        if (!point.IsFixed)
        {
            point.Position += normal * penetration * pointResponse;
            impulseMagnitude = (point.Position - originalPosition).Length() * point.Mass * 0.5f;
        }
        else
        {
            // For fixed points, still calculate an impulse for collision detection
            impulseMagnitude = penetration * point.Mass * 0.5f;
        }

        // Distribute correction to edge points based on projection
        if (!edgeStart.IsFixed && !edgeEnd.IsFixed)
        {
            // Calculate barycentric coordinates
            float alpha = 1.0f - (projection / edgeLength);
            float beta = projection / edgeLength;

            // Apply position correction to edge points
            edgeStart.Position -= normal * penetration * edgeResponse * alpha;
            edgeEnd.Position -= normal * penetration * edgeResponse * beta;
        }
        else if (!edgeStart.IsFixed)
        {
            edgeStart.Position -= normal * penetration * edgeResponse;
        }
        else if (!edgeEnd.IsFixed)
        {
            edgeEnd.Position -= normal * penetration * edgeResponse;
        }

        // Always fire the collision event, even for fixed points
        Collision?.Invoke(this, new CollisionEventArgs(point, edgeStart, edgeEnd, normal, impulseMagnitude));
    }

    /// <summary>
    /// Draws all points and springs in the system.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var point in _points)
        {
            Circle.Draw(spriteBatch, point.Position, point.Radius, point.Color);
        }

        foreach (var s in _springs)
        {
            s.Draw(spriteBatch);
        }
    }

    /// <summary>
    /// Enables or disables rendering of collision borders for debugging.
    /// </summary>
    public void ShowCollisionBorders(bool show, Color? borderColor = null)
    {
        Circle.ShowCollisionBorders = show;
        if (borderColor.HasValue)
        {
            Circle.DebugBorderColor = borderColor.Value;
        }
    }

    /// <summary>
    /// Performs continuous collision detection between two points to prevent tunneling.
    /// </summary>
    private bool ContinuousCollisionDetection(VerletPoint p1, VerletPoint p2, out Vector2 collisionPoint, out float collisionTime)
    {
        collisionPoint = Vector2.Zero;
        collisionTime = 0f;

        Vector2 pos1Start = p1.PreviousPosition;
        Vector2 pos1End = p1.Position;
        Vector2 pos2Start = p2.PreviousPosition;
        Vector2 pos2End = p2.Position;

        Vector2 vel1 = pos1End - pos1Start;
        Vector2 vel2 = pos2End - pos2Start;
        Vector2 relVel = vel1 - vel2;
        Vector2 relPos = pos1Start - pos2Start;

        float minDistance = p1.Radius + p2.Radius;

        // Solve quadratic equation for collision time
        float a = Vector2.Dot(relVel, relVel);
        float b = 2 * Vector2.Dot(relPos, relVel);
        float c = Vector2.Dot(relPos, relPos) - minDistance * minDistance;

        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0 || Math.Abs(a) < 0.0001f)
            return false; // No collision

        float sqrtDiscriminant = (float)Math.Sqrt(discriminant);
        float t1 = (-b - sqrtDiscriminant) / (2 * a);
        float t2 = (-b + sqrtDiscriminant) / (2 * a);

        // Use the earliest valid collision time
        float t = (t1 >= 0 && t1 <= 1) ? t1 : ((t2 >= 0 && t2 <= 1) ? t2 : -1);

        if (t < 0 || t > 1)
            return false; // No collision within this frame

        collisionTime = t;
        collisionPoint = pos1Start + vel1 * t;
        return true;
    }

    /// <summary>
    /// Applies global velocity damping to prevent runaway velocities.
    /// </summary>
    private void ApplyVelocityDamping()
    {
        foreach (var point in _points)
        {
            if (point.IsFixed) continue;

            Vector2 velocity = point.GetVelocity();
            float speed = velocity.Length();

            // Apply global damping
            velocity *= PhysicsConfig.VelocityDamping;

            // Clamp to maximum velocity
            if (speed > PhysicsConfig.MaxVelocity)
            {
                velocity = Vector2.Normalize(velocity) * PhysicsConfig.MaxVelocity;
            }

            point.SetVelocity(velocity);
        }
    }
}