using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace DinoGrr.Core.Physics;

/// <summary>
/// A collection of points + springs that behave like one deformable object.
/// Call SoftBody.CreateRectangle / CreateCircle, then add it to your VerletSystem.
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
    }

    /* ---------- builders ---------- */

    public static SoftBody CreateRectangle(
        VerletSystem vs, Vector2 center, float w, float h,
        float radius = 6, float mass = 2,
        float edgeStiffness = .9f, // ⇦ stiffer outside
        float shearStiffness = .4f, // ⇦ looser inside
        bool pinTop = false)
    {
        var sb = new SoftBody(vs, 1); // 1 here; we tune per-spring below
        // 4 corners
        Vector2[] c =
        {
            center + new Vector2(-w / 2, -h / 2),
            center + new Vector2(w / 2, -h / 2),
            center + new Vector2(w / 2, h / 2),
            center + new Vector2(-w / 2, h / 2),
        };
        foreach (var p in c)
            sb._pts.Add(vs.CreatePoint(p, radius, mass, Color.Orange,
                pinTop && p.Y < center.Y)); // optional pins

        /* structural edges */
        sb.AddRing(edgeStiffness);
        /* shear springs (⁂) keep it from collapsing like a paper bag */
        sb.AddSpring(0, 2, shearStiffness);
        sb.AddSpring(1, 3, shearStiffness);
        return sb;
    }

    public static SoftBody CreateCircle(
        VerletSystem vs, Vector2 center, float rad, int segments = 12,
        float radius = 6, float mass = 2,
        float edgeStiffness = .8f, float spokeStiffness = .3f)
    {
        var sb = new SoftBody(vs, 1);
        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * MathHelper.TwoPi;
            var pos = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * rad;
            sb._pts.Add(vs.CreatePoint(pos, radius, mass, Color.Lime));
        }

        /* outer ring */
        sb.AddRing(edgeStiffness);
        /* spokes to center (optional) */
        var hub = vs.CreatePoint(center, radius, mass, Color.Lime);
        sb._pts.Add(hub);
        for (int i = 0; i < segments; i++)
            sb._spr.Add(vs.CreateSpring(sb._pts[i], hub, spokeStiffness, color: Color.LightGreen));
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
