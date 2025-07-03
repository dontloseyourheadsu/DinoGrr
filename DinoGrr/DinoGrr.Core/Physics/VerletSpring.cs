using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DinoGrr.Core.Rendering;

namespace DinoGrr.Core.Physics;

/// <summary>
/// Distance constraint (spring) between two Verlet points.
/// </summary>
public class VerletSpring
{
    /// <summary>
    /// The first Verlet point.
    /// </summary>
    public readonly VerletPoint P1;

    /// <summary>
    /// The second Verlet point.
    /// </summary>
    public readonly VerletPoint P2;

    /// <summary>
    /// The length the spring tries to maintain.
    /// </summary>
    public float RestLength;

    /// <summary>
    /// Stiffness of the correction (0 – 1). 1 = full correction in a single sub-step.
    /// </summary>
    public float Stiffness;

    /// <summary>
    /// Thickness of the spring when drawn.
    /// </summary>
    public float Thickness;

    /// <summary>
    /// Color of the spring when drawn.
    /// </summary>
    public Color Color;

    /// <summary>
    /// Creates a new spring between two points.
    /// </summary>
    /// <param name="p1">First point.</param>
    /// <param name="p2">Second point.</param>
    /// <param name="stiffness">Stiffness of the spring (0 – 1).</param>
    /// <param name="thickness">Thickness of the spring when drawn.</param>
    /// <param name="color">Color of the spring when drawn.</param>
    public VerletSpring(VerletPoint p1, VerletPoint p2, float stiffness = 1f, float thickness = 2f,
        Color color = default)
    {
        P1 = p1;
        P2 = p2;
        RestLength = Vector2.Distance(p1.Position, p2.Position);
        Stiffness = MathHelper.Clamp(stiffness, 0f, 1f);
        Thickness = thickness;
        Color = color == default ? Color.LightGray : color;
    }

    /// <summary>
    /// Applies distance correction using positional integration.
    /// </summary>
    public void SatisfyConstraint()
    {
        // No correction needed if both points are fixed.
        if (P1.IsFixed && P2.IsFixed) return;

        Vector2 delta = P2.Position - P1.Position;
        float dist = delta.Length();
        if (dist <= 1e-5f) return; // Prevent division by zero

        float diff = (dist - RestLength) / dist; // Deviation factor
        Vector2 correction = delta * diff * Stiffness;

        // Use inverse mass for more realistic constraint satisfaction
        float invMass1 = P1.IsFixed ? 0 : 1.0f / P1.Mass;
        float invMass2 = P2.IsFixed ? 0 : 1.0f / P2.Mass;
        float totalInvMass = invMass1 + invMass2;

        if (totalInvMass > 0)
        {
            if (!P1.IsFixed)
                P1.Position += correction * (invMass1 / totalInvMass);

            if (!P2.IsFixed)
                P2.Position -= correction * (invMass2 / totalInvMass);
        }
    }

    /// <summary>
    /// Draws the spring as a line.
    /// </summary>
    /// <param name="sb">SpriteBatch used for drawing.</param>
    public void Draw(SpriteBatch sb)
    {
        Line.Draw(sb, P1.Position, P2.Position, Color, Thickness);
    }
}
