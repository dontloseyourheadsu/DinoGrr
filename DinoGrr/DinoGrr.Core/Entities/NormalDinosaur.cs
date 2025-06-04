using System;
using System.Collections.Generic;
using DinoGrr.Core.Builders;
using DinoGrr.Core.Events;
using DinoGrr.Core.Physics;
using Microsoft.Xna.Framework;

namespace DinoGrr.Core.Entities;

/// <summary>
/// Represents a normal dinosaur entity in the game.
/// </summary>
public class NormalDinosaur
{
    /// <summary>
    /// Gets whether the dinosaur can currently jump.
    /// </summary>
    public bool CanJump { get; private set; } = false;

    /// <summary>
    /// Gets the bottom left point (left leg) of the dinosaur.
    /// </summary>
    public VerletPoint LeftLeg { get; private set; }

    /// <summary>
    /// Gets the bottom right point (right leg) of the dinosaur.
    /// </summary>
    public VerletPoint RightLeg { get; private set; }

    /// <summary>
    /// Gets the SoftBody that represents the dinosaur's physical body.
    /// </summary>
    public SoftBody Body { get; private set; }

    /// <summary>
    /// Gets all points in the dinosaur's body.
    /// </summary>
    public IReadOnlyList<VerletPoint> Points => Body.Points;

    /// <summary>
    /// Gets all springs in the dinosaur's body.
    /// </summary>
    public IReadOnlyList<VerletSpring> Springs => Body.Springs;

    /// <summary>
    /// The jump force applied when jumping.
    /// </summary>
    private readonly float _jumpForce;

    /// <summary>
    /// The minimum collision impulse required to register as a ground collision.
    /// </summary>
    private readonly float _collisionThreshold;

    /// <summary>
    /// Reference to the VerletSystem.
    /// </summary>
    private readonly VerletSystem _verletSystem;

    /// <summary>
    /// The name of the dinosaur entity.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Creates a new NormalDinosaur instance.
    /// </summary>
    /// <param name="system">The VerletSystem to add the points and springs to.</param>
    /// <param name="position">The center position of the dinosaur.</param>
    /// <param name="width">The width of the dinosaur.</param>
    /// <param name="height">The height of the dinosaur.</param>
    /// <param name="name">The name of the dinosaur.</param>
    /// <param name="jumpForce">The force applied when jumping.</param>
    /// <param name="collisionThreshold">The minimum impulse to register as a ground collision.</param>
    /// <param name="stiffness">The stiffness of the body.</param>
    public NormalDinosaur(
        VerletSystem system,
        Vector2 position,
        float width,
        float height,
        string name,
        float jumpForce = 2.5f,
        float collisionThreshold = 0.5f,
        float stiffness = 0.01f
    )
    {
        _verletSystem = system ?? throw new ArgumentNullException(nameof(system));
        _jumpForce = jumpForce;
        _collisionThreshold = collisionThreshold;

        // Set the name of the dinosaur
        Name = name ?? throw new ArgumentNullException(nameof(name));

        // Create the dinosaur body as a rectangle
        CreateDinoBody(position, width, height, stiffness);

        // Subscribe to collision events
        _verletSystem.Collision += OnCollision;
    }

    /// <summary>
    /// Creates the dinosaur body as a rectangle.
    /// </summary>
    private void CreateDinoBody(Vector2 position, float width, float height, float stiffness)
    {
        // Create a rectangle soft body
        Body = RectangleSoftBodyBuilder.CreateRectangle(
            _verletSystem,
            position,
            width,
            height,
            angle: 0,
            radius: 1,
            mass: 2,
            edgeStiffness: 0.9f,
            shearStiffness: 0.1f,
            pinTop: false,
            stiffness: stiffness
        );

        // Set this SoftBody as the owner for all its points
        Body.SetAsOwnerForPoints();

        // Store references to the legs (assuming RectangleSoftBodyBuilder creates points in the same order)
        // Points are created in this order: top-left, top-right, bottom-right, bottom-left
        if (Body.Points.Count >= 4)
        {
            RightLeg = Body.Points[2]; // bottom-right
            LeftLeg = Body.Points[3];  // bottom-left

            RightLeg.Tag = $"{Name}RightLeg";
            LeftLeg.Tag = $"{Name}LeftLeg";

            Body.Tag = $"{Name}Body";

            // Change the color of the legs to indicate they're special
            RightLeg.Color = Color.Red;
            LeftLeg.Color = Color.Red;

            // Change the body color to green
            foreach (var point in Body.Points)
            {
                if (point != RightLeg && point != LeftLeg)
                {
                    point.Color = Color.Green;
                }
            }

            // Change the spring color for the dinosaur
            foreach (var spring in Body.Springs)
            {
                spring.Color = Color.LightGreen;
            }
        }

        // Start with CanJump = false, will become true on first collision
        CanJump = false;
    }

    /// <summary>
    /// Makes the dinosaur jump if it's allowed to.
    /// </summary>
    /// <returns>True if the jump was performed, false otherwise.</returns>
    public bool Jump()
    {
        if (!CanJump)
            return false;

        // Apply an upward impulse instead of a force
        Vector2 jumpVector = new Vector2(0, -_jumpForce);

        // Apply a stronger impulse to the legs
        LeftLeg.SetVelocity(LeftLeg.GetVelocity() + jumpVector * 0.2f);
        RightLeg.SetVelocity(RightLeg.GetVelocity() + jumpVector * 0.2f);

        // And slightly less to the body points
        foreach (var point in Body.Points)
        {
            if (point != LeftLeg && point != RightLeg)
            {
                point.SetVelocity(point.GetVelocity() + jumpVector * 0.1f);
            }
        }

        // Disable jumping until legs collide again
        CanJump = false;
        return true;
    }

    /// <summary>
    /// Handles collision events for the dinosaur's legs.
    /// </summary>
    private void OnCollision(object sender, CollisionEventArgs e)
    {
        // Check for leg collisions based on collision type
        switch (e.CollisionType)
        {
            case CollisionType.PointToPoint:
                HandlePointToPointCollision(e);
                break;

            case CollisionType.PointToEdge:
                HandlePointToEdgeCollision(e);
                break;

            case CollisionType.SoftBodyOverlap:
                HandleSoftBodyOverlapCollision(e);
                break;
        }
    }

    /// <summary>
    /// Handles point-to-point collisions.
    /// </summary>
    private void HandlePointToPointCollision(CollisionEventArgs e)
    {
        bool legCollision = e.Point1.Tag == $"{Name}LeftLeg" || e.Point1.Tag == $"{Name}RightLeg" ||
                                  e.Point2.Tag == $"{Name}LeftLeg" || e.Point2.Tag == $"{Name}RightLeg";

        // Check if the collision was strong enough and the normal is pointing upward (ground collision)
        bool isGroundCollision = e.Normal.Y < 0 && e.ImpulseMagnitude > _collisionThreshold;

        // Allow jumping if a leg collided with the ground
        if (legCollision || isGroundCollision)
        {
            CanJump = true;
        }
    }

    /// <summary>
    /// Handles point-to-edge collisions.
    /// </summary>
    /// <param name="e">The collision event arguments.</param>
    private void HandlePointToEdgeCollision(CollisionEventArgs e)
    {
        // Check if the point is one of our legs
        bool legCollision = e.Point1.Tag == $"{Name}LeftLeg" || e.Point1.Tag == $"{Name}RightLeg";

        // Check if our legs are part of the edge
        bool legInEdge = e.EdgeStart.Tag == $"{Name}LeftLeg" || e.EdgeStart.Tag == $"{Name}RightLeg" ||
                         e.EdgeEnd.Tag == $"{Name}LeftLeg" || e.EdgeEnd.Tag == $"{Name}RightLeg";

        bool isGroundCollision = e.Normal.Y < 0 && e.ImpulseMagnitude > _collisionThreshold;

        // Allow jumping if a leg collided with the ground (either as the point or as part of the edge)
        if (legCollision || legInEdge || isGroundCollision)
        {
            CanJump = true;
        }
    }

    /// <summary>
    /// Handles softbody overlap collisions.
    /// </summary>
    private void HandleSoftBodyOverlapCollision(CollisionEventArgs e)
    {
        // Check if our body is involved in the collision
        if (e.SoftBody1.Tag == Body.Tag || e.SoftBody2.Tag == Body.Tag)
        {
            CanJump = true;
        }
    }

    /// <summary>
    /// Cleans up event handlers when the dinosaur is no longer needed.
    /// </summary>
    public void Dispose()
    {
        _verletSystem.Collision -= OnCollision;
    }
}