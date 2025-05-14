using Microsoft.Xna.Framework;

namespace DinoGrr.Core.Physics
{
    public class VerletPoint
    {
        // Posición actual
        public Vector2 Position { get; set; }

        // Posición anterior (usada para calcular la velocidad implícita)
        public Vector2 PreviousPosition { get; set; }

        // Aceleración actual
        public Vector2 Acceleration { get; set; }

        // Masa del punto
        public float Mass { get; set; }

        // Radio visual para renderizado
        public float Radius { get; set; }

        // Color para renderizado
        public Color Color { get; set; }

        // Determina si el punto está fijo (no se mueve)
        public bool IsFixed { get; set; }

        /// <summary>
        /// Crea un nuevo punto Verlet con los parámetros especificados
        /// </summary>
        /// <param name="position">Posición inicial</param>
        /// <param name="radius">Radio para visualización</param>
        /// <param name="mass">Masa del punto</param>
        /// <param name="color">Color para visualización</param>
        /// <param name="isFixed">Si el punto debe permanecer fijo</param>
        public VerletPoint(Vector2 position, float radius, float mass, Color color, bool isFixed = false)
        {
            Position = position;
            PreviousPosition = position; // Inicialmente sin velocidad
            Acceleration = Vector2.Zero;
            Mass = mass <= 0 ? 1.0f : mass; // Evitar masa cero o negativa
            Radius = radius;
            Color = color;
            IsFixed = isFixed;
        }

        /// <summary>
        /// Actualiza la posición del punto usando la integración de Verlet
        /// </summary>
        /// <param name="deltaTime">Tiempo transcurrido desde la última actualización</param>
        public void Update(float deltaTime)
        {
            if (IsFixed)
                return;

            // Guardamos la posición actual
            Vector2 temp = Position;

            // Calculamos la velocidad implícita
            Vector2 velocity = Position - PreviousPosition;

            // Aplicamos la integración de Verlet: x' = x + v + a*dt^2
            Position = Position + velocity + Acceleration * deltaTime * deltaTime;

            // Actualizamos la posición anterior
            PreviousPosition = temp;

            // Reseteamos la aceleración
            Acceleration = Vector2.Zero;
        }

        /// <summary>
        /// Aplica una fuerza al punto
        /// </summary>
        /// <param name="force">Vector fuerza a aplicar</param>
        public void ApplyForce(Vector2 force)
        {
            if (IsFixed)
                return;

            // F = ma, entonces a = F/m
            Acceleration += force / Mass;
        }

        /// <summary>
        /// Ajusta directamente la velocidad implícita modificando la posición anterior
        /// </summary>
        /// <param name="velocityChange">Cambio en la velocidad a aplicar</param>
        public void AdjustVelocity(Vector2 velocityChange)
        {
            if (IsFixed)
                return;

            // Ajustamos la posición anterior para reflejar el cambio de velocidad
            PreviousPosition = Position - (Position - PreviousPosition + velocityChange);
        }

        /// <summary>
        /// Aplica una restricción para mantener el punto dentro de los límites de la pantalla
        /// </summary>
        /// <param name="width">Ancho de la pantalla</param>
        /// <param name="height">Alto de la pantalla</param>
        /// <param name="bounceFactor">Factor de rebote (0.0 a 1.0)</param>
        public void ConstrainToBounds(int width, int height, float bounceFactor = 0.8f)
        {
            if (IsFixed)
                return;

            // Velocidad actual implícita
            Vector2 velocity = Position - PreviousPosition;
            Vector2 newVelocity = velocity;
            bool collided = false;

            // Restricción para los bordes horizontales
            if (Position.X < Radius)
            {
                Position = new Vector2(Radius, Position.Y);
                newVelocity.X = -velocity.X * bounceFactor;
                collided = true;
            }
            else if (Position.X > width - Radius)
            {
                Position = new Vector2(width - Radius, Position.Y);
                newVelocity.X = -velocity.X * bounceFactor;
                collided = true;
            }

            // Restricción para los bordes verticales
            if (Position.Y < Radius)
            {
                Position = new Vector2(Position.X, Radius);
                newVelocity.Y = -velocity.Y * bounceFactor;
                collided = true;
            }
            else if (Position.Y > height - Radius)
            {
                Position = new Vector2(Position.X, height - Radius);
                newVelocity.Y = -velocity.Y * bounceFactor;
                collided = true;
            }

            // Si hubo colisión, actualizamos la velocidad
            if (collided)
            {
                // Actualizar posición anterior para reflejar la nueva velocidad
                PreviousPosition = Position - newVelocity;

                // Añadir una pequeña fricción en las superficies
                if (Position.Y >= height - Radius)
                {
                    // Fricción en el suelo
                    float frictionFactor = 0.98f;
                    Vector2 horizontalVelocity = new Vector2(newVelocity.X * frictionFactor, newVelocity.Y);
                    PreviousPosition = Position - horizontalVelocity;
                }
            }
        }

        /// <summary>
        /// Calcula la velocidad actual implícita del punto
        /// </summary>
        /// <returns>Vector de velocidad</returns>
        public Vector2 GetVelocity()
        {
            return Position - PreviousPosition;
        }

        /// <summary>
        /// Establece la velocidad del punto
        /// </summary>
        /// <param name="velocity">Nueva velocidad</param>
        public void SetVelocity(Vector2 velocity)
        {
            if (IsFixed)
                return;

            PreviousPosition = Position - velocity;
        }
    }
}