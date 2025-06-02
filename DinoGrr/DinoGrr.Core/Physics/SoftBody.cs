using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DinoGrr.Core.Physics;

/// <summary>
/// A collection of points + springs that behave like one deformable object.
/// Call SoftBody.CreateRectangle, then add it to your VerletSystem.
/// </summary>
public sealed class SoftBody
{
    /// <summary>
    /// Gets the VerletSystem this soft body is registered with.
    /// </summary>
    public IReadOnlyList<VerletPoint> Points => _pts;

    /// <summary>
    /// Gets the list of springs that connect the points in this soft body.
    /// </summary>
    public IReadOnlyList<VerletSpring> Springs => _spr;

    /// <summary>
    /// Gets the local stiffness factor applied to all springs in this soft body.
    /// </summary>
    public readonly VerletSystem _vs;

    /// <summary>
    /// Gets the local stiffness factor applied to all springs in this soft body.
    /// </summary>
    public readonly float _localStiff;

    /// <summary>
    /// List of points that make up this soft body.
    /// </summary>
    public readonly List<VerletPoint> _pts = new();

    /// <summary>
    /// List of springs that connect the points in this soft body.
    /// </summary>
    public readonly List<VerletSpring> _spr = new();

    /// <summary>
    /// Creates a new SoftBody instance.
    /// </summary>
    /// <param name="system">The VerletSystem to add the points and springs to.</param>
    /// <param name="localStiffness">0 – 1 factor multiplied into every spring.</param>
    public SoftBody(VerletSystem system, float localStiffness)
    {
        _vs = system ?? throw new ArgumentNullException(nameof(system));
        _localStiff = MathHelper.Clamp(localStiffness, 0, 1);

        // Register this softbody with the VerletSystem
        _vs.RegisterSoftBody(this);
    }

    // Add this method to SoftBody:
    /// <summary>
    /// Sets this SoftBody as the owner of all its points.
    /// </summary>
    public void SetAsOwnerForPoints()
    {
        foreach (var point in _pts)
        {
            point.OwnerSoftBody = this;
        }
    }
}