using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using DinoGrr.Core.Physics;
using System;
using Color = Microsoft.Xna.Framework.Color;
using DinoGrr.Core.Builders;
using DinoGrr.Core.Entities;
using DinoGrr.Core.Rendering;
using DinoGrr.Core.Rendering.Textures;
using DinoGrr.Core.Rendering.Parallax;

namespace DinoGrr.Core
{
    /// <summary>
    /// Main entry point for the DinoGrr game using MonoGame.
    /// Manages the game loop, rendering, camera movement, user input, and physics simulation.
    /// </summary>
    public class DinoGrrGame : Game
    {
        // The logical size of the virtual world (independent of actual window size)
        private const int VIRTUAL_WIDTH = 2500;
        private const int VIRTUAL_HEIGHT = 600;

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private VerletSystem _verletSystem;
        private Camera2D _camera;
        private SoftBody _trampoline;
        private NormalDinosaur _dino;
        private DinosaurRenderer _dinoRenderer;
        private Texture2D _dinoTexture;

        // Parallax background
        private ParallaxBackground _parallaxBackground;
        private Texture2D[] _backgroundLayers;

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

            // Initialize the parallax background
            _parallaxBackground = new ParallaxBackground(VIRTUAL_WIDTH, VIRTUAL_HEIGHT);

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

            _dinoTexture = Content.Load<Texture2D>("Assets/Dinosaurs/triceratops_cyan");

            // Load background textures
            // Note: You'll need to add these images to your Content project
            _backgroundLayers = new Texture2D[2];
            _backgroundLayers[0] = Content.Load<Texture2D>("Assets/Backgrounds/mountains"); // Farthest (mountains)
            _backgroundLayers[1] = Content.Load<Texture2D>("Assets/Backgrounds/plants-background"); // Closer (trees)

            // Initialize parallax background with different movement factors
            // Lower values move slower (appear farther away)
            float[] parallaxFactors = { 0.03f, 0.05f };
            Color[] tints = { Color.White, Color.White };
            float[] scales = { 0.8f, 1f };
            float[] verticalOffsets = { -180f, -120f }; // Adjust this value to move background up
            _parallaxBackground.Initialize(_backgroundLayers, parallaxFactors, tints, scales, verticalOffsets);

            // Create the dinosaur
            _dino = new NormalDinosaur(
                _verletSystem,
                new Vector2(VIRTUAL_WIDTH / 2, VIRTUAL_HEIGHT / 3),
                120, 80,
                stiffness: 0.005f,
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

            // Reset the parallax background to the initial camera position
            _parallaxBackground.Reset(_camera.Position);

            // Automatically update viewport and camera when window is resized
            Window.ClientSizeChanged += (_, __) =>
            {
                _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                _graphics.ApplyChanges();

                _camera.SetViewport(GraphicsDevice.Viewport);
            };

            _dinoRenderer = new DinosaurRenderer(GraphicsDevice, _dinoTexture, _dino);
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

            // Handle dinosaur jumping
            if (_dino.CanJump)
            {
                if (IsKeyPressed(Keys.J))
                {
                    // Jump straight up
                    _dino.Jump();
                }
                else if (IsKeyPressed(Keys.A) || IsKeyPressed(Keys.Left))
                {
                    // Jump left
                    _dino.JumpLeft();
                }
                else if (IsKeyPressed(Keys.D) || IsKeyPressed(Keys.Right))
                {
                    // Jump right
                    _dino.JumpRight();
                }
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

            // Update the parallax background using the current camera position
            _parallaxBackground.Update(_camera.Position);

            // Advance the physics simulation
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _verletSystem.Update(dt, subSteps: 4); // More iterations = more stable

            _dinoRenderer.Update();

            base.Update(gameTime);
        }

        /// <summary>
        /// Renders the game world and all objects.
        /// </summary>
        /// <param name="gameTime">Time snapshot for the current frame.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Clear screen to white before drawing
            GraphicsDevice.Clear(Color.White);

            // First, draw the parallax background without camera transform
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _parallaxBackground.Draw(_spriteBatch, _camera);
            _spriteBatch.End();

            // Begin 2D sprite rendering using the camera's transformation matrix
            _spriteBatch.Begin(transformMatrix: _camera.GetMatrix(),
                            samplerState: SamplerState.PointClamp);

            // Draw the virtual world boundaries
            DrawWorldBoundaries();

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

            _dinoRenderer.Draw(_camera);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Draws the boundaries of the virtual world as a black rectangle.
        /// </summary>
        private void DrawWorldBoundaries()
        {
            // Draw top border
            DrawLine(new Vector2(0, 0), new Vector2(VIRTUAL_WIDTH, 0), Color.Black, 2f);

            // Draw right border
            DrawLine(new Vector2(VIRTUAL_WIDTH, 0), new Vector2(VIRTUAL_WIDTH, VIRTUAL_HEIGHT), Color.Black, 2f);

            // Draw bottom border
            DrawLine(new Vector2(VIRTUAL_WIDTH, VIRTUAL_HEIGHT), new Vector2(0, VIRTUAL_HEIGHT), Color.Black, 2f);

            // Draw left border
            DrawLine(new Vector2(0, VIRTUAL_HEIGHT), new Vector2(0, 0), Color.Black, 2f);
        }

        /// <summary>
        /// Helper method to draw a line with specified thickness.
        /// </summary>
        private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f)
        {
            Line.Draw(_spriteBatch, start, end, color, thickness);
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