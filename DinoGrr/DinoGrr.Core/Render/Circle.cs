using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DinoGrr.Core.Render
{
    internal static class Circle
    {
        /// <summary>
        /// Dibuja un círculo sólido.
        /// </summary>
        /// <param name="spriteBatch">El SpriteBatch para dibujar.</param>
        /// <param name="center">El centro del círculo.</param>
        /// <param name="radius">El radio del círculo.</param>
        /// <param name="color">El color del círculo.</param>
        public static void Draw(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
        {
            // Dibujar el círculo usando la textura del píxel.
            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(
                    (int)(center.X - radius),
                    (int)(center.Y - radius),
                    (int)(radius * 2),
                    (int)(radius * 2)),
                null,
                color,
                0f,
                Vector2.Zero,
                SpriteEffects.None,
                0);
        }

        /// <summary>
        /// Inicializa la textura de píxel necesaria para dibujar.
        /// </summary>
        /// <param name="graphicsDevice">El dispositivo gráfico para crear la textura.</param>
        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            if (pixelTexture == null)
            {
                pixelTexture = new Texture2D(graphicsDevice, 1, 1);
                pixelTexture.SetData(new[] { Color.White });
            }
        }

        private static Texture2D pixelTexture;
    }
}