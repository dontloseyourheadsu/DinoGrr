using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using DinoGrr.Core.Physics;
using DinoGrr.Core.Render;
using System;
using System.Collections.Generic;
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
        private SoftBody _softJelly, _trampoline;
        private Random _rnd = new();

        private MouseState _currMouse, _prevMouse;
        private KeyboardState _currKeyboard, _prevKeyboard;

        // List to keep track of created points for easy selection
        private List<VerletPoint> _trackablePoints = new();

        // Currently selected point index (for cycling through with tab key)
        private int _selectedPointIndex = -1;

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
            _trackablePoints.Add(CreateRandomPoint(new Vector2(200, 100)));
            _trackablePoints.Add(CreateRandomPoint(new Vector2(300, 200)));
            _trackablePoints.Add(CreateRandomPoint(new Vector2(500, 150)));

            // Create a spring between two points
            var pA = _verletSystem.CreatePoint(new Vector2(200, 100), 15, 5, Color.Cyan);
            var pB = _verletSystem.CreatePoint(new Vector2(300, 150), 15, 5, Color.Magenta);
            _trackablePoints.Add(pA);
            _trackablePoints.Add(pB);
            _verletSystem.CreateSpring(pA, pB, stiffness: 0.001f, thickness: 10f);

            // Create a static point in the center of the virtual world
            var staticPoint = _verletSystem.CreatePoint(
                new Vector2(VIRTUAL_WIDTH / 2f, VIRTUAL_HEIGHT / 2f),
                radius: 15, mass: 10, color: Color.White, isFixed: true);
            _trackablePoints.Add(staticPoint);

            // a falling jelly block
            _softJelly = SoftBody.CreateRectangle(_verletSystem,
                           center: new Vector2(400, 50),
                           w: 180, h: 100,
                           edgeStiffness: 0.4f, shearStiffness: 0.2f);

            // Add corner points from the jelly to trackable points
            if (_softJelly.Points.Count > 0)
            {
                _trackablePoints.Add(_softJelly.Points[0]); // Top-left corner
                if (_softJelly.Points.Count > 2)
                    _trackablePoints.Add(_softJelly.Points[2]); // Bottom-right corner
            }

            // a trampoline floor. top two corners are fixed
            _trampoline = SoftBody.CreateRectangle(_verletSystem,
                           center: new Vector2(400, 550),
                           w: 350, h: 60,
                           edgeStiffness: 0.9f, shearStiffness: 0.5f,
                           pinTop: true);

            // Add center point of trampoline to trackable points
            if (_trampoline.Points.Count > 0)
            {
                int centerIndex = _trampoline.Points.Count / 2;
                _trackablePoints.Add(_trampoline.Points[centerIndex]);
            }

            // Initialize the camera with current viewport and virtual size
            _camera = new Camera2D(GraphicsDevice.Viewport, VIRTUAL_WIDTH, VIRTUAL_HEIGHT);

            // Set initial camera follow smoothing
            _camera.FollowSmoothing = 0.05f; // Lower value = smoother but slower follow

            // Follow the first point in the list by default
            if (_trackablePoints.Count > 0)
            {
                _selectedPointIndex = 0;
                _camera.Follow(_trackablePoints[_selectedPointIndex]);
            }

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

            // Track mouse state for detecting new clicks
            _prevMouse = _currMouse;
            _currMouse = Mouse.GetState();

            // Handle TAB key to cycle through tracked points
            if (IsKeyPressed(Keys.Tab) && _trackablePoints.Count > 0)
            {
                _selectedPointIndex = (_selectedPointIndex + 1) % _trackablePoints.Count;
                _camera.Follow(_trackablePoints[_selectedPointIndex]);
            }

            // Press F key to stop following and return to free camera
            if (IsKeyPressed(Keys.F))
            {
                _camera.Follow(null);
                _selectedPointIndex = -1;
            }

            // Handle user input for camera movement and zoom
            _camera.HandleInput(gameTime);

            // Handle left mouse button press
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
                    var newPoint = CreateRandomPoint(worldPos);
                    _trackablePoints.Add(newPoint);

                    // Optionally automatically follow the newly created point
                    if (_currKeyboard.IsKeyDown(Keys.LeftShift) || _currKeyboard.IsKeyDown(Keys.RightShift))
                    {
                        _selectedPointIndex = _trackablePoints.Count - 1;
                        _camera.Follow(newPoint);
                    }
                }
            }

            // Handle right mouse button press to select a point to follow
            bool rightClick = _currMouse.RightButton == ButtonState.Pressed &&
                              _prevMouse.RightButton == ButtonState.Released;

            if (rightClick)
            {
                // Try to select a point under the cursor to follow
                Vector2 worldPos = _camera.ScreenToWorld(_currMouse.Position.ToVector2());
                VerletPoint closestPoint = FindClosestPoint(worldPos, 50f); // 50 pixels selection radius

                if (closestPoint != null)
                {
                    // Find index of point for tracking
                    _selectedPointIndex = _trackablePoints.IndexOf(closestPoint);
                    _camera.Follow(closestPoint);
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
                    var newPoint = CreateRandomPoint(worldPos);
                    _trackablePoints.Add(newPoint);
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
        /// Finds the closest point to a given world position within a max distance.
        /// </summary>
        /// <param name="worldPos">The position to search from.</param>
        /// <param name="maxDistance">Maximum distance to consider.</param>
        /// <returns>The closest VerletPoint or null if none found within range.</returns>
        private VerletPoint FindClosestPoint(Vector2 worldPos, float maxDistance)
        {
            VerletPoint closest = null;
            float closestDistSq = maxDistance * maxDistance;

            foreach (var point in _trackablePoints)
            {
                float distSq = Vector2.DistanceSquared(point.Position, worldPos);
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closest = point;
                }
            }

            return closest;
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
        /// Creates a new randomly colored and sized point at the specified world position.
        /// </summary>
        /// <param name="worldPos">The position in world space to create the point.</param>
        /// <returns>The newly created VerletPoint.</returns>
        private VerletPoint CreateRandomPoint(Vector2 worldPos)
        {
            // Radius between 10 and 30
            float r = _rnd.Next(10, 31);

            // Mass is proportional to area (simplified)
            float m = r * r * 0.01f;

            // Generate a random color
            Color c = new((float)_rnd.NextDouble(),
                          (float)_rnd.NextDouble(),
                          (float)_rnd.NextDouble());

            // Create and return the point in the physics system
            return _verletSystem.CreatePoint(worldPos, r, m, c);
        }
    }
}