using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using DinoGrr.Core.Physics;
using DinoGrr.Core.Render;
using System;
using Color = Microsoft.Xna.Framework.Color;
using DinoGrr.Core.Builders;
using DinoGrr.Core.Entities;

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
        private SoftBody _softJelly, _trampoline;
        private NormalDinosaur _dino;

        private KeyboardState _currKeyboard, _prevKeyboard;

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

            // Create the dinosaur
            _dino = new NormalDinosaur(
                _verletSystem,
                new Vector2(VIRTUAL_WIDTH / 2, VIRTUAL_HEIGHT / 3),
                80, 120,
                name: "Dino");

            // Create a trampoline floor at the bottom
            _trampoline = RectangleSoftBodyBuilder.CreateRectangle(
                _verletSystem,
                new Vector2(VIRTUAL_WIDTH / 2, VIRTUAL_HEIGHT - 100),
                300, 20,
                0,
                pinTop: true,  // Pin the top corners of the trampoline
                stiffness: 0.005f);

            // Initialize the camera with current viewport and virtual size
            _camera = new Camera2D(GraphicsDevice.Viewport, VIRTUAL_WIDTH, VIRTUAL_HEIGHT);

            // Set initial camera follow smoothing
            _camera.FollowSmoothing = 0.05f;

            // Follow the dinosaur's center point
            _camera.Follow(_dino.Points[0]);

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

            // Track keyboard state for detecting key presses
            _prevKeyboard = _currKeyboard;
            _currKeyboard = Keyboard.GetState();

            if (IsKeyPressed(Keys.J) && _dino.CanJump)
            {
                _dino.Jump();
            }

            // Press N to follow the dinosaur
            if (IsKeyPressed(Keys.N) && _dino.Points.Count > 0)
            {
                _camera.Follow(_dino.Points[0]);
            }

            // Press F key to stop following and return to free camera
            if (IsKeyPressed(Keys.F))
            {
                _camera.Follow(null);
            }

            // Handle user input for camera movement and zoom
            _camera.HandleInput(gameTime);

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
            
            // Optionally draw debug visualization
            // _verletSystem.DrawDebugSoftBodyBounds(_spriteBatch);

            // Highlight the currently followed point if any
            if (_camera.FollowTarget != null)
            {
                // Draw a highlight ring around the followed point
                Circle.Draw(_spriteBatch,
                    _camera.FollowTarget.Position,
                    _camera.FollowTarget.Radius + 5,
                    Color.Yellow * 0.5f);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Checks if a key was just pressed this frame.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key was just pressed.</returns>
        private bool IsKeyPressed(Keys key)
        {
            return _currKeyboard.IsKeyDown(key) && _prevKeyboard.IsKeyUp(key);
        }
    }
}