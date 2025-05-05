using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

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
        /// <param name="segments">El número de segmentos para dibujar (más segmentos = más suave).</param>
        public static void Draw(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int segments = 32)
        {
            if (circleTexture == null)
                throw new InvalidOperationException("Circle texture not initialized. Call Initialize first.");

            // Dibujar la textura del círculo pre-renderizada
            spriteBatch.Draw(
                circleTexture,
                new Rectangle(
                    (int)(center.X - radius),
                    (int)(center.Y - radius),
                    (int)(radius * 2),
                    (int)(radius * 2)),
                null,
                color,
                0f,
                new Vector2(circleTexture.Width / 2, circleTexture.Height / 2),
                SpriteEffects.None,
                0);
        }

        /// <summary>
        /// Inicializa la textura del círculo necesaria para dibujar.
        /// </summary>
        /// <param name="graphicsDevice">El dispositivo gráfico para crear la textura.</param>
        /// <param name="size">El tamaño de la textura del círculo (mayor = más detallado).</param>
        public static void Initialize(GraphicsDevice graphicsDevice, int size = 256)
        {
            // Crear una textura cuadrada para el círculo
            circleTexture = new Texture2D(graphicsDevice, size, size);

            // Datos de color para cada píxel de la textura
            Color[] colorData = new Color[size * size];

            // Radio en píxeles
            float radius = size / 2f;
            float radiusSquared = radius * radius;

            // Calcular color para cada píxel
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distancia al centro al cuadrado
                    float distanceSquared = (x - radius) * (x - radius) + (y - radius) * (y - radius);

                    // Si el píxel está dentro del círculo, colorear de blanco
                    // Utilizar un degradado en el borde para suavizarlo
                    if (distanceSquared <= radiusSquared)
                    {
                        // Suavizado de bordes (anti-aliasing)
                        float distance = (float)Math.Sqrt(distanceSquared);
                        float alpha = 1.0f;

                        // Aplicar suavizado en los bordes
                        if (distance > radius - 1)
                        {
                            alpha = 1.0f - (distance - (radius - 1));
                        }

                        colorData[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        // Fuera del círculo es transparente
                        colorData[y * size + x] = Color.Transparent;
                    }
                }
            }

            // Actualizar la textura con los datos calculados
            circleTexture.SetData(colorData);
        }

        // Textura pre-renderizada del círculo
        private static Texture2D circleTexture;
    }
}