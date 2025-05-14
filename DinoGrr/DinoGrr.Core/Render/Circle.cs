using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DinoGrr.Core.Render
{
    internal static class Circle
    {
        // Flag para mostrar los bordes de colisión
        public static bool ShowCollisionBorders { get; set; } = false;

        // Color del borde de colisión para debug
        public static Color DebugBorderColor { get; set; } = Color.Red;

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

            // Asegurarnos de que el radio sea al menos 1 para evitar problemas de dibujado
            radius = Math.Max(1.0f, radius);

            // ¡IMPORTANTE! Usar exactamente el mismo tamaño que los cálculos de física
            // Evitamos el cast a int hasta el último momento para prevenir errores de redondeo
            float diameter = radius * 2;

            // Dibujar la textura del círculo pre-renderizada con precisión de píxeles
            spriteBatch.Draw(
                circleTexture,
                center,
                null,
                color,
                0f,
                new Vector2(circleTexture.Width / 2, circleTexture.Height / 2),
                new Vector2(diameter / circleTexture.Width, diameter / circleTexture.Height),
                SpriteEffects.None,
                0);

            // Dibujar borde de colisión si está activado
            if (ShowCollisionBorders)
            {
                DrawDebugBorder(spriteBatch, center, radius, DebugBorderColor);
            }
        }

        /// <summary>
        /// Dibuja un borde de debug alrededor del círculo para verificar los límites físicos.
        /// </summary>
        private static void DrawDebugBorder(SpriteBatch spriteBatch, Vector2 center, float radius, Color borderColor)
        {
            // Dibujar un borde de colisión preciso usando un círculo
            const int segments = 32;
            const float thickness = 1.0f;

            // Dibujar un círculo de líneas para mostrar exactamente el límite de colisión
            Vector2[] points = new Vector2[segments + 1];

            for (int i = 0; i <= segments; i++)
            {
                float angle = ((float)i / segments) * MathHelper.TwoPi;
                points[i] = new Vector2(
                    center.X + (float)Math.Cos(angle) * radius,
                    center.Y + (float)Math.Sin(angle) * radius
                );

                // Dibujar el punto actual conectado al anterior
                if (i > 0)
                {
                    DrawLine(spriteBatch, points[i - 1], points[i], borderColor, thickness);
                }
            }
        }

        /// <summary>
        /// Dibuja una línea entre dos puntos.
        /// </summary>
        private static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness = 1.0f)
        {
            if (pixel == null)
            {
                // Crear una textura de 1x1 píxel blanco si no existe
                pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                pixel.SetData(new[] { Color.White });
            }

            // Calcular la longitud y el ángulo de la línea
            Vector2 delta = end - start;
            float length = delta.Length();
            float angle = (float)Math.Atan2(delta.Y, delta.X);

            // Dibujar la línea como un rectángulo rotado
            spriteBatch.Draw(
                pixel,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(length, thickness),
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

            // Radio en píxeles de la textura (centro de la textura)
            float radius = size / 2f;
            float radiusSquared = radius * radius;

            // Calcular color para cada píxel
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distancia al centro al cuadrado
                    float distanceSquared = (x - radius) * (x - radius) + (y - radius) * (y - radius);

                    // Si el píxel está dentro del círculo
                    if (distanceSquared <= radiusSquared)
                    {
                        // Suavizado de bordes (anti-aliasing)
                        float distance = (float)Math.Sqrt(distanceSquared);
                        float alpha = 1.0f;

                        // Aplicar suavizado en los bordes (máximo 2 píxeles de suavizado)
                        float edgeWidth = Math.Min(2.0f, radius * 0.1f); // 10% del radio o 2 píxeles, lo que sea menor
                        if (distance > radius - edgeWidth)
                        {
                            alpha = 1.0f - (distance - (radius - edgeWidth)) / edgeWidth;
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

        // Textura de 1x1 píxel para dibujar líneas
        private static Texture2D pixel;
    }
}