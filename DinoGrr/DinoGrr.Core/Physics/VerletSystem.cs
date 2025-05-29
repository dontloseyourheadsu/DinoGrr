using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using DinoGrr.Core.Render;
using System;
using System.Drawing;
using Color = Microsoft.Xna.Framework.Color;

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
    /// Creates a new Verlet physics system.
    /// </summary>
    /// <param name="screenWidth">Width of the screen.</param>
    /// <param name="screenHeight">Height of the screen.</param>
    /// <param name="gravity">Gravity vector (defaults to downward).</param>
    /// <param name="dampingFactor">Damping factor (0.0 to 1.0, where 1.0 is perfectly elastic).</param>
    public VerletSystem(int screenWidth, int screenHeight, Vector2? gravity = null, float dampingFactor = 0.1f)
    {
        this._points = new List<VerletPoint>();
        this._gravity = gravity ?? new Vector2(0, 9.8f * 11);
        this._bounds = new RectangleF(0, 0, screenWidth, screenHeight);
        this._dampingFactor = MathHelper.Clamp(dampingFactor, 0.0f, 1.0f);
    }

    /// <summary>
    /// Adds an existing Verlet point to the system.
    /// </summary>
    public void AddPoint(VerletPoint point)
    {
        _points.Add(point);
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
    public void Update(float deltaTime, int subSteps = 8)
    {
        float subDeltaTime = deltaTime / subSteps;

        for (int step = 0; step < subSteps; step++)
        {
            ApplyForces();
            UpdatePoints(subDeltaTime);
            SatisfySprings();
            ApplyConstraints();
            ResolveCollisions();
            ResolveSoftBodyCollisions();
        }
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

                if (distanceSquared < minDistanceSquared && distanceSquared > 0)
                {
                    float distance = (float)Math.Sqrt(distanceSquared);
                    Vector2 direction = delta / distance;
                    float overlap = minDistance - distance;

                    float totalMass = p1.Mass + p2.Mass;
                    float p1Factor = p1.IsFixed ? 0 : p2.Mass / totalMass;
                    float p2Factor = p2.IsFixed ? 0 : p1.Mass / totalMass;

                    Vector2 v1 = p1.GetVelocity();
                    Vector2 v2 = p2.GetVelocity();
                    Vector2 relativeVelocity = v2 - v1;
                    float velocityAlongNormal = Vector2.Dot(relativeVelocity, direction);

                    if (velocityAlongNormal < 0)
                    {
                        float restitution = _dampingFactor;
                        float impulseMagnitude = -(1.0f + restitution) * velocityAlongNormal;
                        impulseMagnitude /= (1.0f / p1.Mass) + (1.0f / p2.Mass);

                        Vector2 impulse = direction * impulseMagnitude;

                        if (!p1.IsFixed)
                        {
                            p1.Position -= direction * overlap * p1Factor;
                            p1.AdjustVelocity(-impulse / p1.Mass);
                        }

                        if (!p2.IsFixed)
                        {
                            p2.Position += direction * overlap * p2Factor;
                            p2.AdjustVelocity(impulse / p2.Mass);
                        }
                    }
                    else
                    {
                        if (!p1.IsFixed)
                            p1.Position -= direction * overlap * p1Factor;

                        if (!p2.IsFixed)
                            p2.Position += direction * overlap * p2Factor;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Applies boundary constraints to all points.
    /// </summary>
    private void ApplyConstraints()
    {
        foreach (var point in _points)
        {
            point.ConstrainToBounds(_bounds.Width, _bounds.Height, _dampingFactor);
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
        // Skip if point is fixed
        if (point.IsFixed) return;
        
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
        
        // Apply position correction to point
        point.Position += normal * penetration * pointResponse;
        
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
}