using DinoGrr.Core.Physics;
using Microsoft.Xna.Framework;

namespace DinoGrr.Core.Entities.Dinosaurs;

/// <summary>
/// Represents a normal dinosaur entity in the game.
/// </summary>
public class NormalDinosaur : GroundEntity
{
    public NormalDinosaur(VerletSystem system, Vector2 position, float width, float height, string name, float jumpForce = 2.5F, float horizontalJumpMultiplier = 1.5F, float collisionThreshold = 0.5F, float stiffness = 0.01F, float? maxSpeed = null) : base(system, position, width, height, name, jumpForce, horizontalJumpMultiplier, collisionThreshold, stiffness, maxSpeed)
    {
    }
}