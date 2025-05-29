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
    public IReadOnlyList<VerletPoint> Points => _pts;
    public IReadOnlyList<VerletSpring> Springs => _spr;

    private readonly VerletSystem _vs;
    private readonly float _localStiff;

    private readonly List<VerletPoint> _pts = new();
    private readonly List<VerletSpring> _spr = new();

    /// <param name="system">The global VerletSystem that does the simulation.</param>
    /// <param name="localStiffness">0 – 1 factor multiplied into every spring.</param>
    private SoftBody(VerletSystem system, float localStiffness)
    {
        _vs = system ?? throw new ArgumentNullException(nameof(system));
        _localStiff = MathHelper.Clamp(localStiffness, 0, 1);
        
        // Register this softbody with the VerletSystem
        _vs.RegisterSoftBody(this);
    }

    /* ---------- builders ---------- */

    /// <summary>
    /// Creates a rectangle-shaped soft body with 4 points and 4 edges.
    /// </summary>
    /// <param name="vs">VerletSystem to add the points and springs to.</param>
    /// <param name="position">The position of the center of the rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <param name="angle">The rotation angle of the rectangle.</param>
    /// <param name="radius">The radius of the points.</param>
    /// <param name="mass">The mass of the points.</param>
    /// <param name="edgeStiffness">The stiffness of the edges.</param>
    /// <param name="shearStiffness">The stiffness of the shear springs.</param>
    /// <param name="pinTop">Whether to pin the top corners.</param>
    /// <param name="stiffness">The overall stiffness of the soft body.</param>
    /// <returns>A new SoftBody instance representing the rectangle.</returns>
    public static SoftBody CreateRectangle(
        VerletSystem vs, Vector2 position, float width, float height, float angle = 0,
        float radius = 6, float mass = 2,
        float edgeStiffness = .9f, // ⇦ stiffer outside
        float shearStiffness = .4f, // ⇦ looser inside
        bool pinTop = false,
        float stiffness = 0.01f)
    {
        var sb = new SoftBody(vs, stiffness); // Using the new stiffness parameter here
        
        // Calculate corner positions with rotation
        var corners = new Vector2[4];
        var halfWidth = width / 2;
        var halfHeight = height / 2;
        
        // Define corners relative to center (0,0)
        Vector2[] relativeCorners =
        {
            new Vector2(-halfWidth, -halfHeight), // top-left
            new Vector2(halfWidth, -halfHeight),  // top-right
            new Vector2(halfWidth, halfHeight),   // bottom-right
            new Vector2(-halfWidth, halfHeight)   // bottom-left
        };
        
        // Rotation matrix components
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        
        // Apply rotation and position to each corner
        for (int i = 0; i < 4; i++)
        {
            // Rotate the corner around (0,0)
            float rotatedX = relativeCorners[i].X * cos - relativeCorners[i].Y * sin;
            float rotatedY = relativeCorners[i].X * sin + relativeCorners[i].Y * cos;
            
            // Translate to final position
            corners[i] = new Vector2(rotatedX, rotatedY) + position;
            
            // Create the verlet point
            sb._pts.Add(vs.CreatePoint(
                corners[i], 
                radius, 
                mass, 
                Color.Orange,
                pinTop && corners[i].Y < position.Y)); // optional pins
        }

        /* structural edges */
        sb.AddRing(edgeStiffness);
        /* shear springs (⁂) keep it from collapsing like a paper bag */
        sb.AddSpring(0, 2, shearStiffness);
        sb.AddSpring(1, 3, shearStiffness);
        return sb;
    }

    /* ---------- helpers ---------- */

    private void AddRing(float k)
    {
        for (int i = 0; i < _pts.Count; i++)
            AddSpring(i, (i + 1) % _pts.Count, k); // loop
    }

    private void AddSpring(int i, int j, float k)
    {
        _spr.Add(_vs.CreateSpring(_pts[i], _pts[j], k * _localStiff, color: Color.LightGray));
    }
}