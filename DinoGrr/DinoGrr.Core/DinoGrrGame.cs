using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DinoGrr.Core.Physics;
using DinoGrr.Core.Render;
using System;
using Microsoft.Xna.Framework.Input.Touch;

namespace DinoGrr.Core
{
    public class DinoGrrGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private VerletSystem _verletSystem;
        private Random _random;

        // Estado del mouse para detectar clicks
        private MouseState _currentMouseState;
        private MouseState _previousMouseState;

        public DinoGrrGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Configurar tamaño de ventana
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
        }

        protected override void Initialize()
        {
            // Inicializar el sistema Verlet con las dimensiones de la ventana
            _verletSystem = new VerletSystem(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight
            );

            _random = new Random();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Inicializar la textura de píxel para dibujar círculos
            Circle.Initialize(GraphicsDevice);

            // Inicializar la textura de línea para dibujar resortes
            Line.Initialize(GraphicsDevice);

            // Crear algunos puntos Verlet iniciales como ejemplo
            CreateRandomVerletPoint(new Vector2(200, 100));
            CreateRandomVerletPoint(new Vector2(300, 200));
            CreateRandomVerletPoint(new Vector2(500, 150));

            // Crear un resorte entre dos puntos existentes
            var pA = _verletSystem.CreatePoint(new Vector2(200, 100), 15, 5, Color.Cyan);
            var pB = _verletSystem.CreatePoint(new Vector2(300, 150), 15, 5, Color.Magenta);

            // Crear un resorte entre ellos
            _verletSystem.CreateSpring(pA, pB, stiffness: 0.001f, thickness: 10f);

            // Añadir un punto fijo en el centro como ancla
            _verletSystem.CreatePoint(
                new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2),
                15,
                10,
                Color.White,
                true
            );
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Capturar estado del mouse
            _previousMouseState = _currentMouseState;
            _currentMouseState = Mouse.GetState();

            // Detectar click del mouse para crear nuevo punto Verlet
            if (_currentMouseState.LeftButton == ButtonState.Pressed &&
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 mousePosition = new Vector2(_currentMouseState.X, _currentMouseState.Y);
                CreateRandomVerletPoint(mousePosition);
            }

            // Lo mismo peroo para touch input
            TouchCollection touchState = TouchPanel.GetState();
            if (touchState.Count > 0)
            {
                TouchLocation touch = touchState[0];
                if (touch.State == TouchLocationState.Pressed)
                {
                    Vector2 touchPosition = touch.Position;
                    CreateRandomVerletPoint(touchPosition);
                }
            }

            // Actualizar física (deltaTime en segundos)
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _verletSystem.Update(deltaTime, 4); // 4 sub-pasos para mayor estabilidad

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();

            // Dibujar todos los puntos Verlet
            _verletSystem.Draw(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Crea un punto Verlet con parámetros aleatorios en la posición especificada
        /// </summary>
        /// <param name="position">Posición donde crear el punto</param>
        private void CreateRandomVerletPoint(Vector2 position)
        {
            // Generar radio aleatorio entre 10 y 30
            float radius = _random.Next(10, 31);

            // Generar masa proporcional al radio (r^2 * densidad)
            float mass = radius * radius * 0.01f;

            // Generar color aleatorio
            Color color = new Color(
                (float)_random.NextDouble(),
                (float)_random.NextDouble(),
                (float)_random.NextDouble()
            );

            // Crear el punto Verlet
            _verletSystem.CreatePoint(position, radius, mass, color);
        }
    }
}