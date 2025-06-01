using System;
using System.Collections.Generic;
using DinoGrr.Core.Physics;
using Microsoft.Xna.Framework;

namespace DinoGrr.Core.Builders;

/// <summary>
/// A builder for creating rectangle-shaped soft bodies in a Verlet system.
/// </summary>
public static class RectangleSoftBodyBuilder
{
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
        AddRing(sb, edgeStiffness);
        /* shear springs (⁂) keep it from collapsing like a paper bag */
        AddSpring(sb, 0, 2, shearStiffness);
        AddSpring(sb, 1, 3, shearStiffness);
        return sb;
    }

    /* ---------- helpers ---------- */
    /// <summary>
    /// Adds a ring of springs connecting all points in a loop.
    /// </summary>
    /// /// <param name="sb">The soft body to which the springs will be added.</param>
    /// <param name="k">The stiffness of the springs in the ring.</param>
    private static void AddRing(SoftBody sb, float k)
    {
        for (int i = 0; i < sb._pts.Count; i++)
            AddSpring(sb, i, (i + 1) % sb._pts.Count, k); // loop
    }

    /// <summary>
    /// Adds a spring between two points.
    /// </summary>
    /// <param name="sb">The soft body to which the spring will be added.</param>
    /// <param name="i">The index of the first point.</param>
    /// <param name="j">The index of the second point.</param>
    /// <param name="k">The stiffness of the spring.</param>
    private static void AddSpring(SoftBody sb, int i, int j, float k)
    {
        sb._spr.Add(sb._vs.CreateSpring(sb._pts[i], sb._pts[j], k * sb._localStiff, color: Color.LightGray));
    }
}