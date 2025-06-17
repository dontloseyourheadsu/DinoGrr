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
    /// A custom tag to help identify this soft body in the simulation.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new SoftBody instance.
    /// </summary>
    /// <param name="system">The VerletSystem to add the points and springs to.</param>
    /// <param name="localStiffness">0 – 1 factor multiplied into every spring.</param>
    /// <param name="maxSpeed">Optional maximum speed limit for all points in this soft body. If null, no limit is applied.</param>
    public SoftBody(VerletSystem system, float localStiffness, float? maxSpeed = null)
    {
        _vs = system ?? throw new ArgumentNullException(nameof(system));
        _localStiff = MathHelper.Clamp(localStiffness, 0, 1);
        _maxSpeed = maxSpeed;

        // Register this softbody with the VerletSystem
        _vs.RegisterSoftBody(this);
    }

    /// <summary>
    /// When a point is added to this soft body, apply the speed limit.
    /// </summary>
    public void AddPoint(VerletPoint point)
    {
        _pts.Add(point);
        point.OwnerSoftBody = this;

        // Apply speed limit if one is set
        if (_maxSpeed.HasValue)
        {
            point.MaxSpeed = _maxSpeed;
        }
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

    /// <summary>
    /// Gets or sets the maximum allowed speed for all points in this soft body.
    /// If null, no speed limit is applied. Setting this will apply to all points in the body.
    /// </summary>
    public float? MaxSpeed
    {
        get => _maxSpeed;
        set
        {
            _maxSpeed = value;
            ApplySpeedLimitToAllPoints();
        }
    }

    /// <summary>
    /// Internal storage for MaxSpeed.
    /// </summary>
    private float? _maxSpeed = null;

    /// <summary>
    /// Applies the current speed limit setting to all points in this soft body.
    /// </summary>
    public void ApplySpeedLimitToAllPoints()
    {
        foreach (var point in _pts)
        {
            point.MaxSpeed = _maxSpeed;
        }
    }
}