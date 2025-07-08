using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
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
    /// Contains the actual gameplay logic for DinoGrr.
    /// This class handles the game world, physics, and gameplay mechanics.
    /// </summary>
    public class GameplayState
    {
        // The logical size of the virtual world (independent of actual window size)
        // Expanded to accommodate more dinosaurs and give them room to move
        private const int VIRTUAL_WIDTH = 5000;
        private const int VIRTUAL_HEIGHT = 800;

        private readonly GraphicsDeviceManager _graphics;
        private readonly SpriteBatch _spriteBatch;
        private readonly Game _game;

        private VerletSystem _verletSystem;
        private Camera2D _camera;
        private SoftBody _trampoline;

        // New dinosaur management system
        private DinosaurManager _dinosaurManager;

        // DinoGirl character
        private DinoGirl _dinoGirl;
        private DinoGirlRenderer _dinoGirlRenderer;
        private Texture2D _dinoGirlTexture;

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
        /// Initializes a new instance of the GameplayState class.
        /// </summary>
        /// <param name="graphics">The graphics device manager.</param>
        /// <param name="spriteBatch">The sprite batch for drawing.</param>
        /// <param name="game">The main game instance.</param>
        public GameplayState(GraphicsDeviceManager graphics, SpriteBatch spriteBatch, Game game)
        {
            _graphics = graphics;
            _spriteBatch = spriteBatch;
            _game = game;
        }

        /// <summary>
        /// Initializes the gameplay state.
        /// </summary>
        public void Initialize()
        {
            // Initialize the Verlet physics system using virtual dimensions
            _verletSystem = new VerletSystem(VIRTUAL_WIDTH, VIRTUAL_HEIGHT);

            // Initialize the rigid body physics system
            _rigidBodySystem = new RigidBodySystem(VIRTUAL_WIDTH, VIRTUAL_HEIGHT, _verletSystem);

            // Initialize the parallax background
            _parallaxBackground = new ParallaxBackground(VIRTUAL_WIDTH, VIRTUAL_HEIGHT);
        }

        /// <summary>
        /// Loads content for the gameplay state.
        /// </summary>
        public void LoadContent()
        {
            // Initialize rendering helpers for primitives
            Circle.Initialize(_graphics.GraphicsDevice);
            Line.Initialize(_graphics.GraphicsDevice);

            // Load DinoGirl texture (sprite sheet)
            _dinoGirlTexture = _game.Content.Load<Texture2D>("Assets/DinoGirl/DinoGrr");

            // Load background textures
            _backgroundLayers = new Texture2D[2];
            _backgroundLayers[0] = _game.Content.Load<Texture2D>("Assets/Backgrounds/mountains"); // Farthest (mountains)
            _backgroundLayers[1] = _game.Content.Load<Texture2D>("Assets/Backgrounds/plants-background"); // Closer (trees)

            // Initialize parallax background with different movement factors
            float[] parallaxFactors = { 0.03f, 0.05f };
            Color[] tints = { Color.White, Color.White };
            float[] scales = { 0.8f, 1f };
            float[] verticalOffsets = { 0f, 50f }; // Further adjusted for higher positioning
            _parallaxBackground.Initialize(_backgroundLayers, parallaxFactors, tints, scales, verticalOffsets);

            // Set a very smooth value for parallax - this will make it much less 'bouncy'
            _parallaxBackground.SmoothingFactor = 0.01f;

            // Create DinoGirl (positioned in the center)
            _dinoGirl = new DinoGirl(
                _verletSystem,
                new Vector2(VIRTUAL_WIDTH / 2, VIRTUAL_HEIGHT / 2),
                100, 180, // Match sprite sheet dimensions
                stiffness: 0.005f,
                name: "DinoGirl",
                maxSpeed: 1f); // Set a maximum speed limit for DinoGirl

            // Initialize the dinosaur manager
            _dinosaurManager = new DinosaurManager(_verletSystem, _graphics.GraphicsDevice, _dinoGirl);
            _dinosaurManager.LoadTextures(_game.Content);
            _dinosaurManager.PopulateWorld(VIRTUAL_WIDTH, VIRTUAL_HEIGHT);

            // Create a trampoline floor at the bottom
            _trampoline = RectangleSoftBodyBuilder.CreateRectangle(
                _verletSystem,
                new Vector2(VIRTUAL_WIDTH / 2, VIRTUAL_HEIGHT - 100),
                width: 800, height: 50, // Made wider for the larger world
                angle: 0,
                pinTop: true,  // Pin the top corners of the trampoline
                stiffness: 0.005f,
                maxSpeed: 10.0f); // Set a higher speed limit for the trampoline for bouncier effects

            // Set trampoline tag for proper friction calculation
            _trampoline.Tag = "trampoline";

            // Initialize the camera with current viewport and virtual size
            _camera = new Camera2D(_graphics.GraphicsDevice.Viewport, VIRTUAL_WIDTH, VIRTUAL_HEIGHT);

            // Set initial camera follow smoothing - make it more responsive for better gameplay
            _camera.FollowSmoothing = 0.15f; // Increased from 0.05f for more responsive following

            // Initialize the mouse drawing system
            _mouseDrawingSystem = new MouseDrawingSystem(_camera);

            // Follow the DinoGirl
            _camera.Follow(_dinoGirl.Points[0]);

            // Reset the parallax background to the initial DinoGirl position
            _parallaxBackground.Reset(_dinoGirl.Points[0].Position);

            // Create DinoGirl renderer
            _dinoGirlRenderer = new DinoGirlRenderer(_graphics.GraphicsDevice, _dinoGirlTexture, _dinoGirl);

            // Load UI font and create pixel texture
            _font = _game.Content.Load<SpriteFont>("Fonts/Hud");

            // Create a 1x1 white pixel texture for UI backgrounds
            _pixelTexture = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // Initialize the UI system
            _gameUI = new GameUI(_spriteBatch, _font, _dinoGirl, _pixelTexture);
        }

        /// <summary>
        /// Updates the gameplay state.
        /// </summary>
        /// <param name="gameTime">Time since last update.</param>
        public void Update(GameTime gameTime)
        {
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

            // Update all dinosaurs
            _dinosaurManager.Update(dt);

            // Press N to cycle through dinosaurs for camera following
            if (IsKeyPressed(Keys.N))
            {
                var dinosaurPoints = _dinosaurManager.GetAllDinosaurPoints().ToList();
                if (dinosaurPoints.Count > 0)
                {
                    // Find currently followed dinosaur and switch to next one
                    var currentIndex = dinosaurPoints.FindIndex(p => p == _camera.FollowTarget);
                    var nextIndex = (currentIndex + 1) % dinosaurPoints.Count;
                    _camera.Follow(dinosaurPoints[nextIndex]);
                }
            }

            // Press M to follow a random dinosaur
            if (IsKeyPressed(Keys.M))
            {
                var dinosaurPoints = _dinosaurManager.GetAllDinosaurPoints().ToList();
                if (dinosaurPoints.Count > 0)
                {
                    var randomIndex = new Random().Next(dinosaurPoints.Count);
                    _camera.Follow(dinosaurPoints[randomIndex]);
                }
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
            _dinoGirlRenderer.Update(dt); // Pass dt for animation timing

            // Update viewport when window is resized
            _camera.SetViewport(_graphics.GraphicsDevice.Viewport);
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
        /// Draws the gameplay state.
        /// </summary>
        /// <param name="gameTime">Time snapshot for the current frame.</param>
        public void Draw(GameTime gameTime)
        {
            // Clear screen to white before drawing
            _graphics.GraphicsDevice.Clear(Color.White);

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

            // Draw all dinosaurs using the manager
            _dinosaurManager.Draw(_camera);

            // Draw DinoGirl
            _dinoGirlRenderer.Draw(_camera);

            // Draw the UI on top (without camera transformation)
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _gameUI.Draw();
            _spriteBatch.End();
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

            // Reset DinoGirl's position to center of the world
            Vector2 dinoGirlStartPosition = new Vector2(VIRTUAL_WIDTH / 2, VIRTUAL_HEIGHT / 2);
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

            // Reset all dinosaurs to their original positions (handled by the manager)
            _dinosaurManager.ResetPositions();
        }
    }
}
