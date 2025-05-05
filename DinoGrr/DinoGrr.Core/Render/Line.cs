using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DinoGrr.Core.Render
{
    internal static class Line
    {
        /// <summary>
        /// Dibuja una línea entre dos puntos.
        /// </summary>
        /// <param name="spriteBatch">El SpriteBatch para dibujar.</param>
        /// <param name="start">El punto inicial de la línea.</param>
        /// <param name="end">El punto final de la línea.</param>
        /// <param name="color">El color de la línea.</param>
        /// <param name="thickness">El grosor de la línea.</param>
        public static void Draw(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
        {
            // Calcular la distancia y el ángulo entre los puntos.
            var distance = Vector2.Distance(start, end);
            var angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

            // Dibujar la línea usando la textura del píxel.
            spriteBatch.Draw(
                pixelTexture,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(distance, thickness),
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
