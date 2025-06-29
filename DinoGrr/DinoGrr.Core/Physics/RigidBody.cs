using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using DinoGrr.Core.Rendering;

namespace DinoGrr.Core.Physics;

/// <summary>
/// Represents a rigid body that maintains its shape during physics simulation.
/// Unlike soft bodies, rigid bodies do not deform and move/rotate as a single unit.
/// </summary>
public class RigidBody
{
    /// <summary>
    /// World position of the rigid body's center of mass.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// World rotation of the rigid body in radians.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Linear velocity of the rigid body.
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// Angular velocity of the rigid body in radians per second.
    /// </summary>
    public float AngularVelocity { get; set; }

    /// <summary>
    /// Mass of the rigid body.
    /// </summary>
    public float Mass { get; private set; }

    /// <summary>
    /// Moment of inertia for rotational dynamics.
    /// </summary>
    public float MomentOfInertia { get; private set; }

    /// <summary>
    /// Local shape points relative to the center of mass.
    /// </summary>
    public List<Vector2> LocalPoints { get; private set; }

    /// <summary>
    /// Color used for rendering the rigid body.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Thickness of the lines when drawing the rigid body.
    /// </summary>
    public float LineThickness { get; set; }

    /// <summary>
    /// Whether the rigid body is fixed in space (immovable).
    /// </summary>
    public bool IsFixed { get; set; }

    /// <summary>
    /// Restitution coefficient for collisions (0 = perfectly inelastic, 1 = perfectly elastic).
    /// </summary>
    public float Restitution { get; set; }

    /// <summary>
    /// Friction coefficient for collisions.
    /// </summary>
    public float Friction { get; set; }

    /// <summary>
    /// Custom tag for identification.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Axis-aligned bounding box for broad-phase collision detection.
    /// </summary>
    public AABB AABB { get; private set; }

    /// <summary>
    /// Creates a new rigid body from a list of world points.
    /// </summary>
    /// <param name="worldPoints">Points in world coordinates that define the shape.</param>
    /// <param name="color">Color for rendering.</param>
    /// <param name="lineThickness">Thickness of the outline.</param>
    /// <param name="density">Density for mass calculation.</param>
    /// <param name="restitution">Bounce factor (0-1).</param>
    /// <param name="friction">Friction coefficient.</param>
    public RigidBody(List<Vector2> worldPoints, Color color, float lineThickness = 2f,
                     float density = 1f, float restitution = 0.8f, float friction = 0.3f)
    {
        if (worldPoints == null || worldPoints.Count < 2)
            throw new ArgumentException("Rigid body must have at least 2 points");

        Color = color;
        LineThickness = lineThickness;
        Restitution = restitution;
        Friction = friction;

        // Calculate center of mass
        Position = CalculateCenterOfMass(worldPoints);

        // Convert to local coordinates
        LocalPoints = new List<Vector2>();
        foreach (var point in worldPoints)
        {
            LocalPoints.Add(point - Position);
        }

        // Calculate mass and moment of inertia
        CalculateMassProperties(density);

        // Initialize physics properties
        Velocity = Vector2.Zero;
        AngularVelocity = 0f;
        Rotation = 0f;
        IsFixed = false;

        UpdateAABB();
    }

    /// <summary>
    /// Gets the world coordinates of all points in the rigid body.
    /// </summary>
    public List<Vector2> GetWorldPoints()
    {
        var worldPoints = new List<Vector2>();
        float cos = (float)Math.Cos(Rotation);
        float sin = (float)Math.Sin(Rotation);

        foreach (var localPoint in LocalPoints)
        {
            // Rotate and translate to world coordinates
            var rotated = new Vector2(
                localPoint.X * cos - localPoint.Y * sin,
                localPoint.X * sin + localPoint.Y * cos
            );
            worldPoints.Add(Position + rotated);
        }

        return worldPoints;
    }

    /// <summary>
    /// Updates the rigid body's physics.
    /// </summary>
    /// <param name="deltaTime">Time step.</param>
    /// <param name="gravity">Gravity vector.</param>
    public void Update(float deltaTime, Vector2 gravity)
    {
        if (IsFixed) return;

        // Apply gravity to center of mass
        Vector2 gravityForce = gravity * Mass;
        Vector2 acceleration = gravityForce / Mass;

        // Integrate linear motion
        Velocity += acceleration * deltaTime;
        Position += Velocity * deltaTime;

        // Apply gravity torque based on shape distribution
        // This creates natural rotation based on how the mass is distributed
        ApplyGravityTorque(gravity, deltaTime);

        // Integrate angular motion
        Rotation += AngularVelocity * deltaTime;

        // Apply damping
        Velocity *= 0.998f; // Reduced damping for more natural movement
        AngularVelocity *= 0.995f; // Reduced angular damping for more rotation

        UpdateAABB();
    }

    /// <summary>
    /// Applies torque from gravity based on the shape's mass distribution.
    /// </summary>
    private void ApplyGravityTorque(Vector2 gravity, float deltaTime)
    {
        if (LocalPoints.Count < 2) return;

        // Calculate torque by considering each point as having mass
        float totalTorque = 0f;
        float pointMass = Mass / LocalPoints.Count; // Distribute mass evenly among points

        foreach (var localPoint in LocalPoints)
        {
            // Transform local point to world space
            float cos = (float)Math.Cos(Rotation);
            float sin = (float)Math.Sin(Rotation);
            Vector2 worldPoint = new Vector2(
                localPoint.X * cos - localPoint.Y * sin,
                localPoint.X * sin + localPoint.Y * cos
            ) + Position;

            // Calculate the arm from center of mass to this point
            Vector2 arm = worldPoint - Position;

            // Apply gravity to this point and calculate torque
            Vector2 forceAtPoint = gravity * pointMass;
            float torque = arm.X * forceAtPoint.Y - arm.Y * forceAtPoint.X; // Cross product

            totalTorque += torque;
        }

        // Apply the accumulated torque
        float angularAcceleration = totalTorque / MomentOfInertia;
        AngularVelocity += angularAcceleration * deltaTime;
    }

    /// <summary>
    /// Applies an impulse at a specific world point.
    /// </summary>
    /// <param name="impulse">Impulse vector.</param>
    /// <param name="contactPoint">World point where impulse is applied.</param>
    public void ApplyImpulse(Vector2 impulse, Vector2 contactPoint)
    {
        if (IsFixed) return;

        // Apply linear impulse
        Velocity += impulse / Mass;

        // Apply angular impulse
        Vector2 r = contactPoint - Position;
        float torque = r.X * impulse.Y - r.Y * impulse.X; // Cross product in 2D
        AngularVelocity += torque / MomentOfInertia;
    }

    /// <summary>
    /// Applies a force at the center of mass.
    /// </summary>
    /// <param name="force">Force vector.</param>
    public void ApplyForce(Vector2 force)
    {
        if (IsFixed) return;
        Vector2 acceleration = force / Mass;
        Velocity += acceleration * (1f / 60f); // Assuming 60 FPS for force application
    }

    /// <summary>
    /// Draws the rigid body as connected line segments with points.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch for drawing.</param>
    public void Draw(SpriteBatch spriteBatch)
    {
        var worldPoints = GetWorldPoints();

        // Draw lines connecting consecutive points (don't close the shape)
        for (int i = 0; i < worldPoints.Count - 1; i++)
        {
            var start = worldPoints[i];
            var end = worldPoints[i + 1];
            Line.Draw(spriteBatch, start, end, Color, LineThickness);
        }

        // Draw bigger circles at each point
        foreach (var point in worldPoints)
        {
            Circle.Draw(spriteBatch, point, LineThickness * 2f, Color);
        }

        // Draw center of mass as a small circle for debugging
        Circle.Draw(spriteBatch, Position, 3f, Color.Red);
    }

    /// <summary>
    /// Calculates the center of mass for a line or polygon.
    /// </summary>
    private Vector2 CalculateCenterOfMass(List<Vector2> points)
    {
        if (points.Count <= 2)
        {
            // For lines, use simple average
            return new Vector2(
                points.Average(p => p.X),
                points.Average(p => p.Y)
            );
        }

        // For polygons, use area-weighted centroid
        Vector2 centroid = Vector2.Zero;
        float area = 0f;

        for (int i = 0; i < points.Count; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % points.Count];

            float cross = p1.X * p2.Y - p2.X * p1.Y;
            area += cross;
            centroid += (p1 + p2) * cross;
        }

        area *= 0.5f;
        if (Math.Abs(area) < 1e-6f)
        {
            // Fallback to simple average for degenerate cases
            return new Vector2(
                points.Average(p => p.X),
                points.Average(p => p.Y)
            );
        }

        return centroid / (6f * area);
    }    /// <summary>
         /// Calculates mass and moment of inertia based on the line or polygon shape.
         /// </summary>
    private void CalculateMassProperties(float density)
    {
        if (LocalPoints.Count <= 2)
        {
            // For lines, calculate mass based on length
            float totalLength = 0f;
            for (int i = 0; i < LocalPoints.Count - 1; i++)
            {
                totalLength += Vector2.Distance(LocalPoints[i], LocalPoints[i + 1]);
            }

            Mass = Math.Max(totalLength * density, 0.1f);

            // Better moment of inertia calculation for lines
            // Treat each point as a point mass and sum their contributions
            float totalMoment = 0f;
            float pointMass = Mass / LocalPoints.Count;

            foreach (var point in LocalPoints)
            {
                float distanceSquared = point.LengthSquared();
                totalMoment += pointMass * distanceSquared;
            }

            MomentOfInertia = Math.Max(totalMoment, 0.1f);
        }
        else
        {
            // For polygons, calculate area-based mass
            float area = 0f;
            for (int i = 0; i < LocalPoints.Count; i++)
            {
                var p1 = LocalPoints[i];
                var p2 = LocalPoints[(i + 1) % LocalPoints.Count];
                area += p1.X * p2.Y - p2.X * p1.Y;
            }
            area = Math.Abs(area) * 0.5f;

            // Mass is proportional to area
            Mass = Math.Max(area * density, 0.1f);

            // Better moment of inertia calculation treating points as discrete masses
            float totalMoment = 0f;
            float pointMass = Mass / LocalPoints.Count;

            foreach (var point in LocalPoints)
            {
                float distanceSquared = point.LengthSquared();
                totalMoment += pointMass * distanceSquared;
            }

            MomentOfInertia = Math.Max(totalMoment, 0.1f);
        }
    }

    /// <summary>
    /// Updates the axis-aligned bounding box.
    /// </summary>
    private void UpdateAABB()
    {
        var worldPoints = GetWorldPoints();
        if (worldPoints.Count == 0) return;

        float minX = worldPoints[0].X;
        float maxX = worldPoints[0].X;
        float minY = worldPoints[0].Y;
        float maxY = worldPoints[0].Y;

        foreach (var point in worldPoints)
        {
            if (point.X < minX) minX = point.X;
            if (point.X > maxX) maxX = point.X;
            if (point.Y < minY) minY = point.Y;
            if (point.Y > maxY) maxY = point.Y;
        }

        AABB = new AABB
        {
            Min = new Vector2(minX, minY),
            Max = new Vector2(maxX, maxY)
        };
    }

    /// <summary>
    /// Constrains the rigid body within screen bounds.
    /// </summary>
    /// <param name="width">Screen width.</param>
    /// <param name="height">Screen height.</param>
    public void ConstrainToBounds(float width, float height)
    {
        if (IsFixed) return;

        var worldPoints = GetWorldPoints();
        bool collided = false;
        Vector2 correction = Vector2.Zero;
        Vector2 contactPoint = Vector2.Zero;
        Vector2 normal = Vector2.Zero;
        int contactCount = 0;

        foreach (var point in worldPoints)
        {
            Vector2 pointCorrection = Vector2.Zero;
            Vector2 pointNormal = Vector2.Zero;
            bool pointCollided = false;

            if (point.X < 0)
            {
                pointCorrection.X = -point.X;
                pointNormal = Vector2.UnitX;
                pointCollided = true;
            }
            else if (point.X > width)
            {
                pointCorrection.X = width - point.X;
                pointNormal = -Vector2.UnitX;
                pointCollided = true;
            }

            if (point.Y < 0)
            {
                pointCorrection.Y = -point.Y;
                pointNormal = Vector2.UnitY;
                pointCollided = true;
            }
            else if (point.Y > height)
            {
                pointCorrection.Y = height - point.Y;
                pointNormal = -Vector2.UnitY;
                pointCollided = true;
            }

            if (pointCollided)
            {
                collided = true;
                contactPoint += point;
                contactCount++;

                // Accumulate the largest correction needed
                if (Math.Abs(pointCorrection.X) > Math.Abs(correction.X))
                    correction.X = pointCorrection.X;
                if (Math.Abs(pointCorrection.Y) > Math.Abs(correction.Y))
                    correction.Y = pointCorrection.Y;

                normal += pointNormal;
            }
        }

        if (collided)
        {
            // Apply position correction
            Position += correction;

            // Calculate average contact point and normal
            contactPoint /= contactCount;
            normal = Vector2.Normalize(normal);

            // Apply collision impulse at the contact point for realistic rotation
            Vector2 relativeVelocity = GetVelocityAtPoint(contactPoint);
            float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

            if (velocityAlongNormal < 0) // Moving towards the boundary
            {
                // Calculate impulse
                float impulseScalar = -(1 + Restitution) * velocityAlongNormal;
                impulseScalar /= (1f / Mass + GetAngularMassAtPoint(contactPoint, normal));

                Vector2 impulse = impulseScalar * normal;
                ApplyImpulse(impulse, contactPoint);
            }

            UpdateAABB();
        }
    }

    /// <summary>
    /// Gets the velocity at a specific point on the rigid body.
    /// </summary>
    private Vector2 GetVelocityAtPoint(Vector2 worldPoint)
    {
        Vector2 r = worldPoint - Position;
        return Velocity + new Vector2(-r.Y, r.X) * AngularVelocity;
    }

    /// <summary>
    /// Calculates the angular mass contribution at a specific point.
    /// </summary>
    private float GetAngularMassAtPoint(Vector2 worldPoint, Vector2 normal)
    {
        Vector2 r = worldPoint - Position;
        float rCrossN = r.X * normal.Y - r.Y * normal.X;
        return (rCrossN * rCrossN) / MomentOfInertia;
    }
}
