using DinoGrr.Core.Physics;
using Microsoft.Xna.Framework;

namespace DinoGrr.Core.Entities.Player;

public class DinoGirl : GroundEntity
{
    public DinoGirl(VerletSystem system, Vector2 position, float width, float height, string name, float jumpForce = 2.5F, float horizontalJumpMultiplier = 1.5F, float collisionThreshold = 0.5F, float stiffness = 0.01F) : base(system, position, width, height, name, jumpForce, horizontalJumpMultiplier, collisionThreshold, stiffness)
    {
    }
}