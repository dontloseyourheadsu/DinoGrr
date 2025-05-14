using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using DinoGrr.Core.Render;
using System;

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

        // Factor de amortiguación para las colisiones (0.0 a 1.0)
        private float dampingFactor;

        /// <summary>
        /// Crea un nuevo sistema de física Verlet
        /// </summary>
        /// <param name="screenWidth">Ancho de la pantalla</param>
        /// <param name="screenHeight">Alto de la pantalla</param>
        /// <param name="gravity">Vector de gravedad (por defecto hacia abajo)</param>
        /// <param name="dampingFactor">Factor de amortiguación (0.0 a 1.0, donde 1.0 es rebote perfecto)</param>
        public VerletSystem(int screenWidth, int screenHeight, Vector2? gravity = null, float dampingFactor = 0.5f)
        {
            this.points = new List<VerletPoint>();
            this.gravity = gravity ?? new Vector2(0, 9.8f * 10); // Valor predeterminado de gravedad
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.dampingFactor = MathHelper.Clamp(dampingFactor, 0.0f, 1.0f);
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
        public void Update(float deltaTime, int subSteps = 8)
        {
            // Aumentamos el número de subSteps predeterminado para mayor estabilidad
            float subDeltaTime = deltaTime / subSteps;

            for (int step = 0; step < subSteps; step++)
            {
                // 1. Aplicar fuerzas externas (como la gravedad)
                ApplyForces();

                // 2. Actualizar posiciones de los puntos
                UpdatePoints(subDeltaTime);

                // 3. Aplicar restricciones (mantener dentro de la pantalla)
                ApplyConstraints();

                // 4. Resolver colisiones entre puntos (después de las restricciones)
                ResolveCollisions();
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
            // Primero, actualizamos los overlap de cada par para prevenir casos de múltiples colisiones
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    VerletPoint p1 = points[i];
                    VerletPoint p2 = points[j];

                    // Vector desde p1 a p2
                    Vector2 delta = p2.Position - p1.Position;
                    float distanceSquared = delta.LengthSquared(); // Más eficiente que calcular la raíz cuadrada

                    // Distancia mínima al cuadrado
                    float minDistance = p1.Radius + p2.Radius;
                    float minDistanceSquared = minDistance * minDistance;

                    // Calculamos colisión con rapidez usando distancia al cuadrado
                    if (distanceSquared < minDistanceSquared && distanceSquared > 0)
                    {
                        float distance = (float)Math.Sqrt(distanceSquared);

                        // Dirección normalizada
                        Vector2 direction = delta / distance;

                        // Cantidad de solapamiento
                        float overlap = minDistance - distance;

                        // Factor de corrección basado en la masa relativa
                        float totalMass = p1.Mass + p2.Mass;
                        float p1Factor = p1.IsFixed ? 0 : p2.Mass / totalMass;
                        float p2Factor = p2.IsFixed ? 0 : p1.Mass / totalMass;

                        // Estimar las velocidades para un rebote más realista
                        Vector2 v1 = p1.GetVelocity();
                        Vector2 v2 = p2.GetVelocity();
                        Vector2 relativeVelocity = v2 - v1;

                        // Calcular la velocidad relativa a lo largo de la normal
                        float velocityAlongNormal = Vector2.Dot(relativeVelocity, direction);

                        // Sólo aplicamos impulso si los objetos se acercan
                        if (velocityAlongNormal < 0)
                        {
                            // Impulso basado en la conservación de la cantidad de movimiento
                            float restitution = dampingFactor; // Coeficiente de restitución
                            float impulseMagnitude = -(1.0f + restitution) * velocityAlongNormal;
                            impulseMagnitude /= (1.0f / p1.Mass) + (1.0f / p2.Mass);

                            // Aplicar impulso
                            Vector2 impulse = direction * impulseMagnitude;

                            if (!p1.IsFixed)
                            {
                                // Mover para evitar solapamiento
                                p1.Position -= direction * overlap * p1Factor;
                                // Ajustar velocidad
                                p1.AdjustVelocity(-impulse / p1.Mass);
                            }

                            if (!p2.IsFixed)
                            {
                                // Mover para evitar solapamiento
                                p2.Position += direction * overlap * p2Factor;
                                // Ajustar velocidad
                                p2.AdjustVelocity(impulse / p2.Mass);
                            }
                        }
                        else
                        {
                            // Si no se acercan, solo corregimos posición para evitar solapamiento
                            if (!p1.IsFixed)
                                p1.Position -= direction * overlap * p1Factor;

                            if (!p2.IsFixed)
                                p2.Position += direction * overlap * p2Factor;
                        }
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
                // Usamos damping para el rebote en los bordes
                point.ConstrainToBounds(screenWidth, screenHeight, dampingFactor);
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

        /// <summary>
        /// Activa o desactiva la visualización de los bordes de colisión para debug
        /// </summary>
        /// <param name="show">Si se deben mostrar los bordes de colisión</param>
        /// <param name="borderColor">Color opcional para los bordes</param>
        public void ShowCollisionBorders(bool show, Color? borderColor = null)
        {
            Circle.ShowCollisionBorders = show;
            if (borderColor.HasValue)
            {
                Circle.DebugBorderColor = borderColor.Value;
            }
        }
    }
}