using DinoGrr.Core.Physics;
using Microsoft.Xna.Framework;

namespace DinoGrr.Core.Entities.Player;

public class DinoGirl : GroundEntity
{
    // Movement speed for walking
    public float WalkSpeed { get; set; } = 0.15f;

    // Direction indicator for animation and movement
    public bool FacingLeft { get; private set; } = false;

    // Whether the character is currently walking
    public bool IsWalking { get; private set; } = false;

    public DinoGirl(VerletSystem system, Vector2 position, float width, float height, string name, float jumpForce = 2.5F, float horizontalJumpMultiplier = 1.5F, float collisionThreshold = 0.5F, float stiffness = 0.01F, float? maxSpeed = null)
        : base(system, position, width, height, name, jumpForce, horizontalJumpMultiplier, collisionThreshold, stiffness, maxSpeed)
    {
    }

    /// <summary>
    /// Makes DinoGirl walk to the left.
    /// </summary>
    public void WalkLeft()
    {
        if (!CanJump) return;

        IsWalking = true;
        FacingLeft = true;

        // Apply a horizontal force to simulate walking
        Vector2 walkVector = new Vector2(-WalkSpeed, 0);

        // Apply to all body points (less to legs to simulate leaning into the walk)
        foreach (var point in Body.Points)
        {
            float factor = (point == LeftLeg || point == RightLeg) ? 0.8f : 1.0f;

            // Get current velocity and adjust it more smoothly
            Vector2 currentVelocity = point.GetVelocity();

            // If already moving in the opposite direction, dampen the change to avoid abrupt stops
            if (currentVelocity.X > 0)
            {
                // Reduce the existing velocity before adding new
                currentVelocity.X *= 0.7f;
                point.SetVelocity(currentVelocity + walkVector * factor * 0.8f);
            }
            else
            {
                // Otherwise, apply normal velocity change
                point.SetVelocity(currentVelocity + walkVector * factor);
            }
        }
    }

    /// <summary>
    /// Makes DinoGirl walk to the right.
    /// </summary>
    public void WalkRight()
    {
        if (!CanJump) return;

        IsWalking = true;
        FacingLeft = false;

        // Apply a horizontal force to simulate walking
        Vector2 walkVector = new Vector2(WalkSpeed, 0);

        // Apply to all body points (less to legs to simulate leaning into the walk)
        foreach (var point in Body.Points)
        {
            float factor = (point == LeftLeg || point == RightLeg) ? 0.8f : 1.0f;

            // Get current velocity and adjust it more smoothly
            Vector2 currentVelocity = point.GetVelocity();

            // If already moving in the opposite direction, dampen the change to avoid abrupt stops
            if (currentVelocity.X < 0)
            {
                // Reduce the existing velocity before adding new
                currentVelocity.X *= 0.7f;
                point.SetVelocity(currentVelocity + walkVector * factor * 0.8f);
            }
            else
            {
                // Otherwise, apply normal velocity change
                point.SetVelocity(currentVelocity + walkVector * factor);
            }
        }
    }

    /// <summary>
    /// Stops the walking motion.
    /// </summary>
    public void StopWalking()
    {
        IsWalking = false;
    }

    // Override the Jump method to maintain facing direction during jumps
    public override bool Jump(int direction = 0)
    {
        if (direction < 0)
            FacingLeft = true;
        else if (direction > 0)
            FacingLeft = false;

        return base.Jump(direction);
    }


}