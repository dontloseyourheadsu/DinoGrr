using Microsoft.Xna.Framework;
using System;

namespace DinoGrr.Core.Physics;

/// <summary>
/// Represents a point in a Verlet physics simulation.
/// </summary>
public class VerletPoint
{
    /// <summary>
    /// Current position of the point.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Previous position (used to calculate implicit velocity).
    /// </summary>
    public Vector2 PreviousPosition { get; set; }

    /// <summary>
    /// Current acceleration applied to the point.
    /// </summary>
    public Vector2 Acceleration { get; set; }

    /// <summary>
    /// Mass of the point.
    /// </summary>
    public float Mass { get; set; }

    /// <summary>
    /// Visual radius for rendering.
    /// </summary>
    public float Radius { get; set; }

    /// <summary>
    /// Color used for rendering.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Determines whether the point is fixed (immovable).
    /// </summary>
    public bool IsFixed { get; set; }

    /// <summary>
    /// The soft body this point belongs to, if any.
    /// </summary>
    public SoftBody OwnerSoftBody { get; set; }

    // Add this to your VerletPoint class
    /// <summary>
    /// A custom tag to help identify special points in the simulation.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new Verlet point with the specified parameters.
    /// </summary>
    /// <param name="position">Initial position.</param>
    /// <param name="radius">Visual radius.</param>
    /// <param name="mass">Mass of the point.</param>
    /// <param name="color">Visual color.</param>
    /// <param name="isFixed">Whether the point is fixed in space.</param>
    public VerletPoint(Vector2 position, float radius, float mass, Color color, bool isFixed = false)
    {
        Position = position;
        PreviousPosition = position; // Initially no velocity
        Acceleration = Vector2.Zero;
        Mass = mass <= 0 ? 1.0f : mass; // Avoid zero or negative mass
        Radius = radius;
        Color = color;
        IsFixed = isFixed;
    }

    /// <summary>
    /// Updates the point's position using Verlet integration.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last update.</param>
    public void Update(float deltaTime)
    {
        if (IsFixed)
            return;

        Vector2 temp = Position;
        Vector2 velocity = Position - PreviousPosition;
        Position = Position + velocity + Acceleration * deltaTime * deltaTime;
        PreviousPosition = temp;
        Acceleration = Vector2.Zero;
    }

    /// <summary>
    /// Applies a force to the point.
    /// </summary>
    /// <param name="force">Force vector to apply.</param>
    public void ApplyForce(Vector2 force)
    {
        if (IsFixed)
            return;

        Acceleration += force / Mass;
    }

    /// <summary>
    /// Directly adjusts the implicit velocity by modifying the previous position.
    /// </summary>
    /// <param name="velocityChange">Velocity change to apply.</param>
    public void AdjustVelocity(Vector2 velocityChange)
    {
        if (IsFixed)
            return;

        PreviousPosition = Position - (Position - PreviousPosition + velocityChange);
    }

    /// <summary>
    /// Constrains the point within the screen bounds and applies bounce with optional friction.
    /// </summary>
    /// <param name="width">Screen width.</param>
    /// <param name="height">Screen height.</param>
    /// <param name="bounceFactor">Bounce factor (0.0 to 1.0).</param>
    /// <returns>True if the point collided with a boundary, false otherwise.</returns>
    public bool ConstrainToBounds(float width, float height, float bounceFactor = 0.8f)
    {
        if (IsFixed)
            return false;

        Vector2 velocity = Position - PreviousPosition;
        Vector2 newVelocity = velocity;
        bool collided = false;
        Vector2 normal = Vector2.Zero;
        float impulseMagnitude = 0;

        // Horizontal bounds
        if (Position.X < Radius)
        {
            Position = new Vector2(Radius, Position.Y);
            newVelocity.X = -velocity.X * bounceFactor;
            collided = true;
            normal = new Vector2(1, 0); // Right-facing normal
            impulseMagnitude = MathF.Abs(velocity.X) * Mass;
        }
        else if (Position.X > width - Radius)
        {
            Position = new Vector2(width - Radius, Position.Y);
            newVelocity.X = -velocity.X * bounceFactor;
            collided = true;
            normal = new Vector2(-1, 0); // Left-facing normal
            impulseMagnitude = MathF.Abs(velocity.X) * Mass;
        }

        // Vertical bounds
        if (Position.Y < Radius)
        {
            Position = new Vector2(Position.X, Radius);
            newVelocity.Y = -velocity.Y * bounceFactor;
            collided = true;
            normal = new Vector2(0, 1); // Down-facing normal
            impulseMagnitude = MathF.Abs(velocity.Y) * Mass;
        }
        else if (Position.Y > height - Radius)
        {
            Position = new Vector2(Position.X, height - Radius);
            newVelocity.Y = -velocity.Y * bounceFactor;
            collided = true;
            normal = new Vector2(0, -1); // Up-facing normal (for ground collision)
            impulseMagnitude = MathF.Abs(velocity.Y) * Mass;
        }

        if (collided)
        {
            // Update position and velocity
            PreviousPosition = Position - newVelocity;

            // Add floor friction
            if (Position.Y >= height - Radius)
            {
                float frictionFactor = 0.98f;
                Vector2 horizontalVelocity = new Vector2(newVelocity.X * frictionFactor, newVelocity.Y);
                PreviousPosition = Position - horizontalVelocity;
            }

            // Notify the owner SoftBody (if any) about this boundary collision
            if (OwnerSoftBody != null)
            {
                // The edge is represented by two fixed points at the boundary
                // We'll create temporary fixed points to represent the boundary edge
                VerletPoint boundaryPoint1 = new VerletPoint(
                    normal.X != 0 ? Position : new Vector2(Position.X - 50, Position.Y),
                    Radius, float.MaxValue, Color.White, true);

                VerletPoint boundaryPoint2 = new VerletPoint(
                    normal.X != 0 ? Position + new Vector2(0, 100) : new Vector2(Position.X + 50, Position.Y),
                    Radius, float.MaxValue, Color.White, true);

                // For now we won't directly fire events from the point
                // Instead, we'll use the return value to indicate a collision occurred
            }
        }

        return collided;
    }

    /// <summary>
    /// Calculates the current implicit velocity of the point.
    /// </summary>
    /// <returns>Velocity vector.</returns>
    public Vector2 GetVelocity()
    {
        return Position - PreviousPosition;
    }

    /// <summary>
    /// Sets the velocity of the point.
    /// </summary>
    /// <param name="velocity">New velocity to set.</param>
    public void SetVelocity(Vector2 velocity)
    {
        if (IsFixed)
            return;

        PreviousPosition = Position - velocity;
    }
}
