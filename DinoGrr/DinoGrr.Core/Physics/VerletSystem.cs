using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using DinoGrr.Core.Render;

namespace DinoGrr.Core.Physics
{
    public class VerletSystem
    {
        // Lista de puntos Verlet en el sistema
        private List<VerletPoint> points;

        // Gravedad del sistema
        private Vector2 gravity;

        // Dimensiones de la pantalla para restricciones
        private int screenWidth;
        private int screenHeight;

        /// <summary>
        /// Crea un nuevo sistema de física Verlet
        /// </summary>
        /// <param name="screenWidth">Ancho de la pantalla</param>
        /// <param name="screenHeight">Alto de la pantalla</param>
        /// <param name="gravity">Vector de gravedad (por defecto hacia abajo)</param>
        public VerletSystem(int screenWidth, int screenHeight, Vector2? gravity = null)
        {
            this.points = new List<VerletPoint>();
            this.gravity = gravity ?? new Vector2(0, 9.8f * 10); // Valor predeterminado de gravedad
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
        }

        /// <summary>
        /// Añade un punto Verlet al sistema
        /// </summary>
        /// <param name="point">Punto Verlet a añadir</param>
        public void AddPoint(VerletPoint point)
        {
            points.Add(point);
        }

        /// <summary>
        /// Crea y añade un nuevo punto Verlet al sistema
        /// </summary>
        /// <param name="position">Posición inicial</param>
        /// <param name="radius">Radio visual</param>
        /// <param name="mass">Masa del punto</param>
        /// <param name="color">Color visual</param>
        /// <param name="isFixed">Si el punto está fijo o no</param>
        /// <returns>El punto Verlet creado</returns>
        public VerletPoint CreatePoint(Vector2 position, float radius, float mass, Color color, bool isFixed = false)
        {
            VerletPoint point = new VerletPoint(position, radius, mass, color, isFixed);
            points.Add(point);
            return point;
        }

        /// <summary>
        /// Actualiza la física de todos los puntos en el sistema
        /// </summary>
        /// <param name="deltaTime">Tiempo transcurrido desde la última actualización</param>
        /// <param name="subSteps">Número de sub-pasos para mayor precisión</param>
        public void Update(float deltaTime, int subSteps = 1)
        {
            float subDeltaTime = deltaTime / subSteps;

            for (int step = 0; step < subSteps; step++)
            {
                // 1. Aplicar fuerzas externas (como la gravedad)
                ApplyForces();

                // 2. Actualizar posiciones de los puntos
                UpdatePoints(subDeltaTime);

                // 3. Resolver colisiones entre puntos
                ResolveCollisions();

                // 4. Aplicar restricciones (mantener dentro de la pantalla)
                ApplyConstraints();
            }
        }

        /// <summary>
        /// Aplica fuerzas externas (gravedad) a todos los puntos
        /// </summary>
        private void ApplyForces()
        {
            foreach (var point in points)
            {
                point.ApplyForce(gravity * point.Mass); // La gravedad se aplica proporcionalmente a la masa
            }
        }

        /// <summary>
        /// Actualiza las posiciones de todos los puntos
        /// </summary>
        /// <param name="deltaTime">Tiempo para este sub-paso</param>
        private void UpdatePoints(float deltaTime)
        {
            foreach (var point in points)
            {
                point.Update(deltaTime);
            }
        }

        /// <summary>
        /// Resuelve colisiones entre todos los puntos
        /// </summary>
        private void ResolveCollisions()
        {
            // Para cada par de puntos
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    VerletPoint p1 = points[i];
                    VerletPoint p2 = points[j];

                    // Vector desde p1 a p2
                    Vector2 delta = p2.Position - p1.Position;
                    float distance = delta.Length();

                    // Distancia mínima = suma de radios
                    float minDistance = p1.Radius + p2.Radius;

                    // Si hay colisión
                    if (distance < minDistance && distance > 0)
                    {
                        // Dirección normalizada
                        Vector2 direction = delta / distance;

                        // Cantidad de solapamiento
                        float overlap = minDistance - distance;

                        // Factor de corrección basado en la masa relativa
                        float totalMass = p1.Mass + p2.Mass;
                        float p1Factor = p1.IsFixed ? 0 : p2.Mass / totalMass;
                        float p2Factor = p2.IsFixed ? 0 : p1.Mass / totalMass;

                        // Corregir posiciones proporcionalmente a la masa
                        if (!p1.IsFixed)
                            p1.Position -= direction * overlap * p1Factor;

                        if (!p2.IsFixed)
                            p2.Position += direction * overlap * p2Factor;
                    }
                }
            }
        }

        /// <summary>
        /// Aplica restricciones de límites a todos los puntos
        /// </summary>
        private void ApplyConstraints()
        {
            foreach (var point in points)
            {
                point.ConstrainToBounds(screenWidth, screenHeight);
            }
        }

        /// <summary>
        /// Dibuja todos los puntos en el sistema
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch para dibujar</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var point in points)
            {
                Circle.Draw(spriteBatch, point.Position, point.Radius, point.Color);
            }
        }
    }
}