using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DinoGrr.Core.Render;

namespace DinoGrr.Core.Physics
{
    /// <summary>
    /// Restricción de distancia (resorte) entre dos puntos Verlet.
    /// </summary>
    public class VerletSpring
    {
        public readonly VerletPoint P1;
        public readonly VerletPoint P2;

        /// <summary>Longitud que el resorte intenta mantener.</summary>
        public float RestLength;

        /// <summary>Rigidez de corrección (0 – 1). 1 = corrección completa en un solo sub-paso.</summary>
        public float Stiffness;

        public float Thickness = 2f; // Grosor del resorte al dibujar

        /// <summary>
        /// Crea un nuevo resorte entre dos puntos.
        /// </summary>
        public VerletSpring(VerletPoint p1, VerletPoint p2, float stiffness = 1f, float thickness = 2f)
        {
            P1 = p1;
            P2 = p2;
            RestLength = Vector2.Distance(p1.Position, p2.Position);
            Stiffness = MathHelper.Clamp(stiffness, 0f, 1f);
            Thickness = thickness;
        }

        /// <summary>
        /// Aplica la corrección de distancia usando integración de posiciones.
        /// </summary>
        public void SatisfyConstraint()
        {
            // Puntos fijos no necesitan corrección.
            if (P1.IsFixed && P2.IsFixed) return;

            Vector2 delta = P2.Position - P1.Position;
            float dist = delta.Length();
            if (dist <= 1e-5f) return;          // evita división por cero

            float diff = (dist - RestLength) / dist; // factor de exceso/defecto de longitud
            Vector2 correction = delta * diff * Stiffness;

            float totalMass = P1.Mass + P2.Mass;

            if (!P1.IsFixed)
                P1.Position += correction * (P2.Mass / totalMass); // mover proporcional a masa

            if (!P2.IsFixed)
                P2.Position -= correction * (P1.Mass / totalMass);
        }

        /// <summary>Dibuja el resorte como línea.</summary>
        public void Draw(SpriteBatch sb, Color color)
        {
            Line.Draw(sb, P1.Position, P2.Position, color, Thickness);
        }
    }
}
