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
    private List<VerletPoint> points;

    /// <summary>
    /// List of Verlet springs (optional).
    /// </summary>
    private readonly List<VerletSpring> springs = new();

    /// <summary>
    /// Gravity vector for the system.
    /// </summary>
    private Vector2 gravity;

    /// <summary>
    /// Screen bounds for the system.
    /// </summary>
    private RectangleF _bounds;

    /// <summary>
    /// Damping factor for collisions (0.0 to 1.0).
    /// </summary>
    private float dampingFactor;

    /// <summary>
    /// Creates a new Verlet physics system.
    /// </summary>
    /// <param name="screenWidth">Width of the screen.</param>
    /// <param name="screenHeight">Height of the screen.</param>
    /// <param name="gravity">Gravity vector (defaults to downward).</param>
    /// <param name="dampingFactor">Damping factor (0.0 to 1.0, where 1.0 is perfectly elastic).</param>
    public VerletSystem(int screenWidth, int screenHeight, Vector2? gravity = null, float dampingFactor = 0.1f)
    {
        this.points = new List<VerletPoint>();
        this.gravity = gravity ?? new Vector2(0, 9.8f * 11);
        this._bounds = new RectangleF(0, 0, screenWidth, screenHeight);
        this.dampingFactor = MathHelper.Clamp(dampingFactor, 0.0f, 1.0f);
    }

    /// <summary>
    /// Adds an existing Verlet point to the system.
    /// </summary>
    public void AddPoint(VerletPoint point)
    {
        points.Add(point);
    }

    /// <summary>
    /// Creates and adds a new Verlet point to the system.
    /// </summary>
    public VerletPoint CreatePoint(Vector2 position, float radius, float mass, Color color, bool isFixed = false)
    {
        var point = new VerletPoint(position, radius, mass, color, isFixed);
        points.Add(point);
        return point;
    }

    /// <summary>
    /// Creates a spring between two Verlet points.
    /// </summary>
    public VerletSpring CreateSpring(VerletPoint p1, VerletPoint p2, float stiffness = 1f, float thickness = 2f)
    {
        var s = new VerletSpring(p1, p2, stiffness, thickness);
        springs.Add(s);
        return s;
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
        }
    }

    /// <summary>
    /// Applies external forces (e.g., gravity) to all points.
    /// </summary>
    private void ApplyForces()
    {
        foreach (var point in points)
        {
            point.ApplyForce(gravity * point.Mass);
        }
    }

    /// <summary>
    /// Updates the position of all points.
    /// </summary>
    private void UpdatePoints(float deltaTime)
    {
        foreach (var point in points)
        {
            point.Update(deltaTime);
        }
    }

    /// <summary>
    /// Resolves collisions between all points.
    /// </summary>
    private void ResolveCollisions()
    {
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                VerletPoint p1 = points[i];
                VerletPoint p2 = points[j];

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
                        float restitution = dampingFactor;
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
        foreach (var point in points)
        {
            point.ConstrainToBounds(_bounds.Width, _bounds.Height, dampingFactor);
        }
    }

    /// <summary>
    /// Satisfies spring constraints between points.
    /// </summary>
    private void SatisfySprings(int iterations = 1)
    {
        for (int k = 0; k < iterations; k++)
            foreach (var s in springs)
                s.SatisfyConstraint();
    }

    /// <summary>
    /// Draws all points and springs in the system.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var point in points)
        {
            Circle.Draw(spriteBatch, point.Position, point.Radius, point.Color);
        }

        foreach (var s in springs)
        {
            s.Draw(spriteBatch, Color.LightGray);
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
    /// Sets the screen bounds for the system.
    /// </summary>
    /// <param name="left">Left boundary.</param>
    /// <param name="top">Top boundary.</param>
    /// <param name="right">Right boundary.</param>
    /// <param name="bottom">Bottom boundary.</param>
    public void SetBounds(float left, float top, float right, float bottom)
    {
        _bounds = new RectangleF(left, top, right - left, bottom - top);
    }
}
