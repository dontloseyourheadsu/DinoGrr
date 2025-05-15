using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using DinoGrr.Core.Physics;
using DinoGrr.Core.Render;
using System;
using Color = Microsoft.Xna.Framework.Color;

namespace DinoGrr.Core
{
    /// <summary>
    /// Main entry point for the DinoGrr game using MonoGame.
    /// Manages the game loop, rendering, camera movement, user input, and physics simulation.
    /// </summary>
    public class DinoGrrGame : Game
    {
        // The logical size of the virtual world (independent of actual window size)
        private const int VIRTUAL_WIDTH = 800;
        private const int VIRTUAL_HEIGHT = 600;

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private VerletSystem _verletSystem;
        private Camera2D _camera;
        private Random _rnd = new();

        private MouseState _currMouse, _prevMouse;

        /// <summary>
        /// Constructs the game and initializes graphics settings.
        /// </summary>
        public DinoGrrGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set default window size to match virtual resolution
            _graphics.PreferredBackBufferWidth = VIRTUAL_WIDTH;
            _graphics.PreferredBackBufferHeight = VIRTUAL_HEIGHT;

            // Allow the user to resize the window freely
            Window.AllowUserResizing = true;
        }

        /// <summary>
        /// Initializes core systems such as the physics simulation.
        /// </summary>
        protected override void Initialize()
        {
            // Initialize the Verlet physics system using virtual dimensions
            _verletSystem = new VerletSystem(VIRTUAL_WIDTH, VIRTUAL_HEIGHT);

            base.Initialize();
        }

        /// <summary>
        /// Loads game content and initializes graphics objects.
        /// </summary>
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Initialize rendering helpers for primitives
            Circle.Initialize(GraphicsDevice);
            Line.Initialize(GraphicsDevice);

            // Create a few sample points for demo purposes
            CreateRandomPoint(new Vector2(200, 100));
            CreateRandomPoint(new Vector2(300, 200));
            CreateRandomPoint(new Vector2(500, 150));

            // Create a spring between two points
            var pA = _verletSystem.CreatePoint(new Vector2(200, 100), 15, 5, Color.Cyan);
            var pB = _verletSystem.CreatePoint(new Vector2(300, 150), 15, 5, Color.Magenta);
            _verletSystem.CreateSpring(pA, pB, stiffness: 0.001f, thickness: 10f);

            // Create a static point in the center of the virtual world
            _verletSystem.CreatePoint(
                new Vector2(VIRTUAL_WIDTH / 2f, VIRTUAL_HEIGHT / 2f),
                radius: 15, mass: 10, color: Color.White, isFixed: true);

            // Initialize the camera with current viewport and virtual size
            _camera = new Camera2D(GraphicsDevice.Viewport, VIRTUAL_WIDTH, VIRTUAL_HEIGHT);

            // Automatically update viewport and camera when window is resized
            Window.ClientSizeChanged += (_, __) =>
            {
                _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                _graphics.ApplyChanges();

                _camera.SetViewport(GraphicsDevice.Viewport);
            };
        }

        /// <summary>
        /// Updates the game state: handles input and physics.
        /// </summary>
        /// <param name="gameTime">Time since last update.</param>
        protected override void Update(GameTime gameTime)
        {
            // Exit on Back button (GamePad) or Escape key
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Handle user input for camera movement and zoom
            _camera.HandleInput(gameTime);

            // Track mouse state for detecting new clicks
            _prevMouse = _currMouse;
            _currMouse = Mouse.GetState();

            bool click = _currMouse.LeftButton == ButtonState.Pressed &&
                         _prevMouse.LeftButton == ButtonState.Released;

            if (click)
            {
                // Convert clicked screen coordinates to world coordinates
                Vector2 worldPos = _camera.ScreenToWorld(_currMouse.Position.ToVector2());

                // Only accept clicks within the virtual world bounds
                if (worldPos.X >= 0 && worldPos.X <= VIRTUAL_WIDTH &&
                    worldPos.Y >= 0 && worldPos.Y <= VIRTUAL_HEIGHT)
                {
                    CreateRandomPoint(worldPos);
                }
            }

            // Handle touch input (first finger only)
            TouchCollection touches = TouchPanel.GetState();
            if (touches.Count > 0 && touches[0].State == TouchLocationState.Pressed)
            {
                Vector2 worldPos = _camera.ScreenToWorld(touches[0].Position);
                if (worldPos.X >= 0 && worldPos.X <= VIRTUAL_WIDTH &&
                    worldPos.Y >= 0 && worldPos.Y <= VIRTUAL_HEIGHT)
                {
                    CreateRandomPoint(worldPos);
                }
            }

            // Advance the physics simulation
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _verletSystem.Update(dt, subSteps: 4); // More iterations = more stable

            base.Update(gameTime);
        }

        /// <summary>
        /// Renders the game world and all objects.
        /// </summary>
        /// <param name="gameTime">Time snapshot for the current frame.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Clear screen to black before drawing
            GraphicsDevice.Clear(Color.Black);

            // Begin 2D sprite rendering using the camera's transformation matrix
            _spriteBatch.Begin(transformMatrix: _camera.GetMatrix(),
                               samplerState: SamplerState.PointClamp);

            // Draw all points, springs, and visual elements in the physics system
            _verletSystem.Draw(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Creates a new randomly colored and sized point at the specified world position.
        /// </summary>
        /// <param name="worldPos">The position in world space to create the point.</param>
        private void CreateRandomPoint(Vector2 worldPos)
        {
            // Radius between 10 and 30
            float r = _rnd.Next(10, 31);

            // Mass is proportional to area (simplified)
            float m = r * r * 0.01f;

            // Generate a random color
            Color c = new((float)_rnd.NextDouble(),
                          (float)_rnd.NextDouble(),
                          (float)_rnd.NextDouble());

            // Create the point in the physics system
            _verletSystem.CreatePoint(worldPos, r, m, c);
        }
    }
}
