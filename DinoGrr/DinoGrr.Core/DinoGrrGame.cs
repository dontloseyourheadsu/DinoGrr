using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using DinoGrr.Core.Physics;
using Color = Microsoft.Xna.Framework.Color;
using DinoGrr.Core.Builders;
using DinoGrr.Core.Rendering;
using DinoGrr.Core.Rendering.Textures;
using DinoGrr.Core.Rendering.Parallax;
using DinoGrr.Core.Entities.Dinosaurs;
using DinoGrr.Core.Entities.Player;
using DinoGrr.Core.UI;

namespace DinoGrr.Core
{
    /// <summary>
    /// Main entry point for the DinoGrr game using MonoGame.
    /// Manages the game loop, rendering, camera movement, user input, and physics simulation.
    /// 
    /// Controls:
    /// - Arrow Keys: Move DinoGirl (Left/Right to walk, Up to jump)
    /// - G: Follow DinoGirl with camera
    /// - N: Follow Random Dinosaur with camera
    /// - M: Follow Targeting Dinosaur with camera
    /// - F: Free camera (stop following)
    /// - R: Restart game when game over
    /// - Escape: Exit game
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
        private NormalDinosaur _targetingDino;
        private DinosaurRenderer _dinoRenderer;
        private DinosaurRenderer _targetingDinoRenderer;
        private Texture2D _dinoTexture;

        // DinoGirl character
        private DinoGirl _dinoGirl;
        private DinoGirlRenderer _dinoGirlRenderer;
        private Texture2D _dinoGirlTexture;

        // Random movement for dinosaur
        private RandomDinoMover _randomDinoMover;

        // Targeting AI for the second dinosaur
        private TargetingDinoAI _targetingDinoAI;

        // Parallax background
        private ParallaxBackground _parallaxBackground;
        private Texture2D[] _backgroundLayers;

        // UI System
        private GameUI _gameUI;
        private SpriteFont _font;
        private Texture2D _pixelTexture;

        // Rigid Body Drawing System
        private RigidBodySystem _rigidBodySystem;
        private MouseDrawingSystem _mouseDrawingSystem;

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

            // Initialize the rigid body physics system
            _rigidBodySystem = new RigidBodySystem(VIRTUAL_WIDTH, VIRTUAL_HEIGHT, _verletSystem);

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

            // Load dinosaur texture
            _dinoTexture = Content.Load<Texture2D>("Assets/Dinosaurs/triceratops_cyan");

            // Load DinoGirl texture (sprite sheet)
            _dinoGirlTexture = Content.Load<Texture2D>("Assets/DinoGirl/DinoGrr");

            // Load background textures
            _backgroundLayers = new Texture2D[2];
            _backgroundLayers[0] = Content.Load<Texture2D>("Assets/Backgrounds/mountains"); // Farthest (mountains)
            _backgroundLayers[1] = Content.Load<Texture2D>("Assets/Backgrounds/plants-background"); // Closer (trees)

            // Initialize parallax background with different movement factors
            float[] parallaxFactors = { 0.03f, 0.05f };
            Color[] tints = { Color.White, Color.White };
            float[] scales = { 0.8f, 1f };
            float[] verticalOffsets = { 0f, 50f }; // Further adjusted for higher positioning
            _parallaxBackground.Initialize(_backgroundLayers, parallaxFactors, tints, scales, verticalOffsets);

            // Set a very smooth value for parallax - this will make it much less 'bouncy'
            _parallaxBackground.SmoothingFactor = 0.01f;

            // Create the first dinosaur (positioned on the left side) - uses random movement
            _dino = new NormalDinosaur(
                _verletSystem,
                new Vector2(VIRTUAL_WIDTH / 4, VIRTUAL_HEIGHT / 3),
                120, 80,
                stiffness: 0.005f,
                name: "RandomDino"); // Set a lower speed limit for the dinosaur

            // Create the second dinosaur (positioned on the right side) - uses targeting AI
            _targetingDino = new NormalDinosaur(
                _verletSystem,
                new Vector2(VIRTUAL_WIDTH * 3 / 4, VIRTUAL_HEIGHT / 3),
                120, 80,
                stiffness: 0.005f,
                name: "TargetingDino");

            // Create DinoGirl (positioned on the right side)
            _dinoGirl = new DinoGirl(
                _verletSystem,
                new Vector2(VIRTUAL_WIDTH / 2, VIRTUAL_HEIGHT / 3),
                100, 180, // Match sprite sheet dimensions
                stiffness: 0.005f,
                name: "DinoGirl",
                maxSpeed: 1f); // Set a maximum speed limit for DinoGirl

            // Initialize random movement for the first dinosaur
                _randomDinoMover = new RandomDinoMover(_dino);

            // Initialize targeting AI for the second dinosaur (1/3 of virtual world distance)
            float maxTargetDistance = VIRTUAL_WIDTH / 3f;
            _targetingDinoAI = new TargetingDinoAI(_targetingDino, _dinoGirl, maxTargetDistance);

            // Create a trampoline floor at the bottom
            _trampoline = RectangleSoftBodyBuilder.CreateRectangle(
                _verletSystem,
                new Vector2(VIRTUAL_WIDTH / 2, VIRTUAL_HEIGHT - 100),
                width: 500, height: 50,
                angle: 0,
                pinTop: true,  // Pin the top corners of the trampoline
                stiffness: 0.005f,
                maxSpeed: 10.0f); // Set a higher speed limit for the trampoline for bouncier effects

            // Set trampoline tag for proper friction calculation
            _trampoline.Tag = "trampoline";

            // Initialize the camera with current viewport and virtual size
            _camera = new Camera2D(GraphicsDevice.Viewport, VIRTUAL_WIDTH, VIRTUAL_HEIGHT);

            // Set initial camera follow smoothing
            _camera.FollowSmoothing = 0.05f;

            // Initialize the mouse drawing system
            _mouseDrawingSystem = new MouseDrawingSystem(_camera);

            // Follow the DinoGirl instead of dinosaur
            _camera.Follow(_dinoGirl.Points[0]);

            // Reset the parallax background to the initial DinoGirl position
            _parallaxBackground.Reset(_dinoGirl.Points[0].Position);

            // Automatically update viewport and camera when window is resized
            Window.ClientSizeChanged += (_, __) =>
            {
                _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                _graphics.ApplyChanges();

                _camera.SetViewport(GraphicsDevice.Viewport);
            };

            // Create renderers
            _dinoRenderer = new DinosaurRenderer(GraphicsDevice, _dinoTexture, _dino);
            _targetingDinoRenderer = new DinosaurRenderer(GraphicsDevice, _dinoTexture, _targetingDino);
            _dinoGirlRenderer = new DinoGirlRenderer(GraphicsDevice, _dinoGirlTexture, _dinoGirl);

            // Load UI font and create pixel texture
            _font = Content.Load<SpriteFont>("Fonts/Hud");

            // Create a 1x1 white pixel texture for UI backgrounds
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // Initialize the UI system
            _gameUI = new GameUI(_spriteBatch, _font, _dinoGirl, _pixelTexture);
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

            // Handle restart when game is over
            if (_dinoGirl.CurrentLifePoints <= 0 && IsKeyPressed(Keys.R))
            {
                RestartGame();
                return;
            }

            // Get delta time
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle DinoGirl movement with arrow keys
            UpdateDinoGirlMovement();

            // Update random dinosaur movement
            _randomDinoMover.Update(dt);

            // Update targeting dinosaur AI
            _targetingDinoAI.Update(dt);

            // Press N to follow the random dinosaur
            if (IsKeyPressed(Keys.N) && _dino.Points.Count > 0)
            {
                _camera.Follow(_dino.Points[0]);
            }

            // Press M to follow the targeting dinosaur
            if (IsKeyPressed(Keys.M) && _targetingDino.Points.Count > 0)
            {
                _camera.Follow(_targetingDino.Points[0]);
            }

            // Press G to follow the DinoGirl
            if (IsKeyPressed(Keys.G) && _dinoGirl.Points.Count > 0)
            {
                _camera.Follow(_dinoGirl.Points[0]);
            }

            // Press F key to stop following and return to free camera
            if (IsKeyPressed(Keys.F))
            {
                _camera.Follow(null);
            }

            // Handle user input for camera movement and zoom
            _camera.HandleInput(gameTime);

            // Update the parallax background using DinoGirl's position for traditional parallax effect
            if (_dinoGirl.Points.Count > 0)
            {
                _parallaxBackground.Update(_dinoGirl.Points[0].Position);
            }

            // Advance the physics simulation
            _verletSystem.Update(dt, subSteps: 4); // More iterations = more stable

            // Update rigid body physics system
            _rigidBodySystem.Update(dt, subSteps: 4);

            // Update mouse drawing system
            _mouseDrawingSystem.Update(Mouse.GetState());

            // Check if a drawing was completed and create a rigid body
            var completedDrawing = _mouseDrawingSystem.GetCompletedDrawing();
            if (completedDrawing != null)
            {
                // Create a rigid body from the completed drawing with amber color
                var amberColor = new Color(255, 191, 0); // Amber color
                _rigidBodySystem.CreateRigidBodyFromDrawing(completedDrawing, amberColor, 8f, 1f); // Thicker lines (8f)
            }

            // Update DinoGirl's state (invincibility timer, etc.)
            _dinoGirl.Update(dt);

            // Update renderers
            _dinoRenderer.Update();
            _targetingDinoRenderer.Update();
            _dinoGirlRenderer.Update(dt); // Pass dt for animation timing

            base.Update(gameTime);
        }

        /// <summary>
        /// Handle DinoGirl movement based on arrow key input.
        /// </summary>
        private void UpdateDinoGirlMovement()
        {
            // Stop walking by default - we'll set it to true if needed
            _dinoGirl.StopWalking();

            // Handle walking and jumping
            // Handle left/right movement
            bool isMovingLeft = _currKeyboard.IsKeyDown(Keys.Left);
            bool isMovingRight = _currKeyboard.IsKeyDown(Keys.Right);
            bool isJumping = _currKeyboard.IsKeyDown(Keys.Up);

            if (_dinoGirl.CanJump)
            {
                // If the up key is pressed, jump
                if (isJumping)
                {
                    // Jump in the direction being pressed, or straight up if no direction
                    if (isMovingLeft)
                    {
                        _dinoGirl.JumpLeft();
                    }
                    else if (isMovingRight)
                    {
                        _dinoGirl.JumpRight();
                    }
                    else
                    {
                        _dinoGirl.Jump();
                    }
                }
                // Otherwise, walk if left/right keys are pressed
                else if (isMovingLeft)
                {
                    _dinoGirl.WalkLeft();
                }
                else if (isMovingRight)
                {
                    _dinoGirl.WalkRight();
                }
            }
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

            // Draw all rigid bodies
            _rigidBodySystem.Draw(_spriteBatch);

            // Draw current mouse drawing preview
            DrawMouseDrawingPreview();

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

            // Draw the characters with their own SpriteBatch begins/ends
            _dinoRenderer.Draw(_camera);
            _targetingDinoRenderer.Draw(_camera);
            _dinoGirlRenderer.Draw(_camera);

            // Draw the UI on top (without camera transformation)
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _gameUI.Draw();
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
        /// Draws the current mouse drawing preview while the user is drawing.
        /// </summary>
        private void DrawMouseDrawingPreview()
        {
            if (_mouseDrawingSystem.State == MouseDrawingSystem.DrawingState.Drawing &&
                _mouseDrawingSystem.CurrentDrawing.Count > 0)
            {
                var amberColor = new Color(255, 191, 0); // Amber color
                var points = _mouseDrawingSystem.CurrentDrawing;

                // Draw the current drawing path as connected lines (no closing)
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Line.Draw(_spriteBatch, points[i], points[i + 1], amberColor, 8f); // Thick amber lines
                }

                // Draw bigger circles at each point
                foreach (var point in points)
                {
                    Circle.Draw(_spriteBatch, point, 12f, amberColor); // Bigger amber circles
                }
            }
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

        /// <summary>
        /// Restarts the game by resetting DinoGirl's life points and position.
        /// </summary>
        private void RestartGame()
        {
            // Reset DinoGirl's life points and status
            _dinoGirl.Reset();

            // Reset DinoGirl's position
            Vector2 dinoGirlStartPosition = new Vector2(VIRTUAL_WIDTH / 2, VIRTUAL_HEIGHT / 3);
            for (int i = 0; i < _dinoGirl.Points.Count; i++)
            {
                var point = _dinoGirl.Points[i];
                // Reset to original position relative to start
                Vector2 offset = Vector2.Zero;
                if (i == 1) offset = new Vector2(0, -60); // Top point
                else if (i == 2) offset = new Vector2(50, -60); // Top-right
                else if (i == 3) offset = new Vector2(50, 0); // Bottom-right

                point.Position = dinoGirlStartPosition + offset;
                point.PreviousPosition = point.Position; // Reset velocity
            }

            // Reset random dinosaur position
            Vector2 randomDinoStartPosition = new Vector2(VIRTUAL_WIDTH / 4, VIRTUAL_HEIGHT / 3);
            for (int i = 0; i < _dino.Points.Count; i++)
            {
                var point = _dino.Points[i];
                Vector2 offset = Vector2.Zero;
                if (i == 1) offset = new Vector2(0, -40); // Top point
                else if (i == 2) offset = new Vector2(60, -40); // Top-right
                else if (i == 3) offset = new Vector2(60, 0); // Bottom-right

                point.Position = randomDinoStartPosition + offset;
                point.PreviousPosition = point.Position; // Reset velocity
            }

            // Reset targeting dinosaur position
            Vector2 targetingDinoStartPosition = new Vector2(VIRTUAL_WIDTH * 3 / 4, VIRTUAL_HEIGHT / 3);
            for (int i = 0; i < _targetingDino.Points.Count; i++)
            {
                var point = _targetingDino.Points[i];
                Vector2 offset = Vector2.Zero;
                if (i == 1) offset = new Vector2(0, -40); // Top point
                else if (i == 2) offset = new Vector2(60, -40); // Top-right
                else if (i == 3) offset = new Vector2(60, 0); // Bottom-right

                point.Position = targetingDinoStartPosition + offset;
                point.PreviousPosition = point.Position; // Reset velocity
            }
        }
    }
}