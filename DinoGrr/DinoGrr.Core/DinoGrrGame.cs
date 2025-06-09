using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DinoGrr.Core.Physics;
using Color = Microsoft.Xna.Framework.Color;
using DinoGrr.Core.Builders;
using DinoGrr.Core.Rendering;
using DinoGrr.Core.Rendering.Textures;
using DinoGrr.Core.Rendering.Parallax;
using DinoGrr.Core.Entities.Dinosaurs;
using DinoGrr.Core.Entities.Player;

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

        // DinoGirl character
        private DinoGirl _dinoGirl;
        private DinoGirlRenderer _dinoGirlRenderer;
        private Texture2D _dinoGirlTexture;

        // Random movement for dinosaur
        private RandomDinoMover _randomDinoMover;

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
            float[] verticalOffsets = { -180f, -120f };
            _parallaxBackground.Initialize(_backgroundLayers, parallaxFactors, tints, scales, verticalOffsets);

            // Create the dinosaur (positioned on the left side)
            _dino = new NormalDinosaur(
                _verletSystem,
                new Vector2(VIRTUAL_WIDTH / 4, VIRTUAL_HEIGHT / 3),
                120, 80,
                stiffness: 0.005f,
                name: "Dino");

            // Create DinoGirl (positioned on the right side)
            _dinoGirl = new DinoGirl(
                _verletSystem,
                new Vector2(VIRTUAL_WIDTH / 2, VIRTUAL_HEIGHT / 3),
                100, 180, // Match sprite sheet dimensions
                stiffness: 0.005f,
                name: "DinoGirl");

            // Initialize random movement for the dinosaur
            _randomDinoMover = new RandomDinoMover(_dino);

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

            // Follow the DinoGirl instead of dinosaur
            _camera.Follow(_dinoGirl.Points[0]);

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

            // Create renderers
            _dinoRenderer = new DinosaurRenderer(GraphicsDevice, _dinoTexture, _dino);
            _dinoGirlRenderer = new DinoGirlRenderer(GraphicsDevice, _dinoGirlTexture, _dinoGirl);
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

            // Get delta time
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle DinoGirl movement with arrow keys
            UpdateDinoGirlMovement();

            // Update random dinosaur movement
            _randomDinoMover.Update(dt);

            // Press N to follow the dinosaur
            if (IsKeyPressed(Keys.N) && _dino.Points.Count > 0)
            {
                _camera.Follow(_dino.Points[0]);
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

            // Update the parallax background using the current camera position
            _parallaxBackground.Update(_camera.Position);

            // Advance the physics simulation
            _verletSystem.Update(dt, subSteps: 4); // More iterations = more stable

            // Update renderers
            _dinoRenderer.Update();
            _dinoGirlRenderer.Update(dt); // Pass dt for animation timing

            base.Update(gameTime);
        }

        /// <summary>
        /// Handle DinoGirl movement based on arrow key input.
        /// </summary>
        private void UpdateDinoGirlMovement()
        {
            if (_dinoGirl.CanJump)
            {
                if (_currKeyboard.IsKeyDown(Keys.Up))
                {
                    // Jump straight up
                    _dinoGirl.Jump();
                }
                else if (_currKeyboard.IsKeyDown(Keys.Left))
                {
                    // Jump left
                    _dinoGirl.JumpLeft();
                }
                else if (_currKeyboard.IsKeyDown(Keys.Right))
                {
                    // Jump right
                    _dinoGirl.JumpRight();
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
            _dinoGirlRenderer.Draw(_camera);

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