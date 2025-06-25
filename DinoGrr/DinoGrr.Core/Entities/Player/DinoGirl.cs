using DinoGrr.Core.Physics;
using Microsoft.Xna.Framework;
using System;

namespace DinoGrr.Core.Entities.Player;

public class DinoGirl : GroundEntity
{
    // Movement speed for walking
    public float WalkSpeed { get; set; } = 0.15f;

    // Direction indicator for animation and movement
    public bool FacingLeft { get; private set; } = false;

    // Whether the character is currently walking
    public bool IsWalking { get; private set; } = false;

    // Life system
    public int MaxLifePoints { get; private set; } = 3;
    public int CurrentLifePoints { get; private set; }

    // Invincibility system
    public bool IsInvincible { get; private set; } = false;
    public float InvincibilityDuration { get; set; } = 3.0f; // 3 seconds
    private float _invincibilityTimer = 0f;

    // Events
    public event EventHandler<int> LifePointsChanged;
    public event EventHandler<bool> InvincibilityChanged;

    public DinoGirl(VerletSystem system, Vector2 position, float width, float height, string name, float jumpForce = 2.5F, float horizontalJumpMultiplier = 1.5F, float collisionThreshold = 0.5F, float stiffness = 0.01F, float? maxSpeed = null)
        : base(system, position, width, height, name, jumpForce, horizontalJumpMultiplier, collisionThreshold, stiffness, maxSpeed)
    {
        CurrentLifePoints = MaxLifePoints;

        // Subscribe to collision events for damage detection
        _verletSystem.Collision += OnDinoGirlCollision;
    }

    /// <summary>
    /// Updates DinoGirl's state including invincibility timer.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    public void Update(float deltaTime)
    {
        if (IsInvincible)
        {
            _invincibilityTimer -= deltaTime;
            if (_invincibilityTimer <= 0f)
            {
                EndInvincibility();
            }
        }
    }

    /// <summary>
    /// Takes damage and enters invincibility state.
    /// </summary>
    /// <param name="damage">Amount of damage to take.</param>
    public void TakeDamage(int damage = 1)
    {
        if (IsInvincible) return; // Already invincible, no damage

        CurrentLifePoints = Math.Max(0, CurrentLifePoints - damage);
        LifePointsChanged?.Invoke(this, CurrentLifePoints);

        if (CurrentLifePoints > 0)
        {
            StartInvincibility();
        }
    }

    /// <summary>
    /// Starts the invincibility state.
    /// </summary>
    private void StartInvincibility()
    {
        IsInvincible = true;
        _invincibilityTimer = InvincibilityDuration;
        InvincibilityChanged?.Invoke(this, true);
    }

    /// <summary>
    /// Ends the invincibility state.
    /// </summary>
    private void EndInvincibility()
    {
        IsInvincible = false;
        _invincibilityTimer = 0f;
        InvincibilityChanged?.Invoke(this, false);
    }

    /// <summary>
    /// Handles collision events to detect dinosaur collisions.
    /// </summary>
    private void OnDinoGirlCollision(object sender, DinoGrr.Core.Events.CollisionEventArgs e)
    {
        // Check if DinoGirl is involved in the collision
        bool dinoGirlInvolved = false;
        bool dinosaurInvolved = false;

        // Check if DinoGirl's soft body is involved
        if (e.SoftBody1 == Body || e.SoftBody2 == Body)
        {
            dinoGirlInvolved = true;
        }

        // Check if any point belongs to DinoGirl
        if (!dinoGirlInvolved && e.Point1?.OwnerSoftBody == Body)
        {
            dinoGirlInvolved = true;
        }
        if (!dinoGirlInvolved && e.Point2?.OwnerSoftBody == Body)
        {
            dinoGirlInvolved = true;
        }

        if (!dinoGirlInvolved) return;

        // Check if a dinosaur is involved (look for "Dinosaur" in the tag)
        SoftBody otherBody = (e.SoftBody1 == Body) ? e.SoftBody2 : e.SoftBody1;
        if (otherBody != null && otherBody.Tag.Contains("Dinosaur"))
        {
            dinosaurInvolved = true;
        }

        // If both DinoGirl and a dinosaur are involved, take damage
        if (dinoGirlInvolved && dinosaurInvolved)
        {
            TakeDamage();
        }
    }

    /// <summary>
    /// Makes DinoGirl walk to the left.
    /// </summary>
    public void WalkLeft()
    {
        if (!CanJump || IsInvincible) return; // Can't move while invincible

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
        if (!CanJump || IsInvincible) return; // Can't move while invincible

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
        if (IsInvincible) return false; // Can't jump while invincible

        if (direction < 0)
            FacingLeft = true;
        else if (direction > 0)
            FacingLeft = false;

        return base.Jump(direction);
    }

    /// <summary>
    /// Resets DinoGirl to full health and removes invincibility.
    /// </summary>
    public void Reset()
    {
        CurrentLifePoints = MaxLifePoints;
        IsInvincible = false;
        _invincibilityTimer = 0f;

        // Trigger events
        LifePointsChanged?.Invoke(this, CurrentLifePoints);
        InvincibilityChanged?.Invoke(this, false);
    }

    /// <summary>
    /// Disposes resources and unsubscribes from events.
    /// </summary>
    public override void Dispose()
    {
        if (_verletSystem != null)
        {
            _verletSystem.Collision -= OnDinoGirlCollision;
        }
        base.Dispose();
    }
}