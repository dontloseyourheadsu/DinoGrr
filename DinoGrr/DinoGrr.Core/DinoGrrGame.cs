using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using DinoGrr.Core.Physics;
using DinoGrr.Core.Render;
using System;

namespace DinoGrr.Core;

/// <summary>
/// Main game class for DinoGrr using MonoGame framework.
/// Handles rendering, input, and Verlet physics.
/// </summary>
public class DinoGrrGame : Game
{
    /// <summary>
    /// Manages graphics device settings and window properties.
    /// </summary>
    private GraphicsDeviceManager _graphics;

    /// <summary>
    /// SpriteBatch used for drawing 2D textures.
    /// </summary>
    private SpriteBatch _spriteBatch;

    /// <summary>
    /// Instance of the Verlet physics system.
    /// </summary>
    private VerletSystem _verletSystem;

    /// <summary>
    /// Random number generator for randomizing point attributes.
    /// </summary>
    private Random _random;

    /// <summary>
    /// Current mouse state for input handling.
    /// </summary>
    private MouseState _currentMouseState;

    /// <summary>
    /// Previous mouse state to detect mouse button transitions.
    /// </summary>
    private MouseState _previousMouseState;

    /// <summary>
    /// Initializes the game and graphics manager.
    /// </summary>
    public DinoGrrGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // Set window size
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 600;
    }

    /// <summary>
    /// Initializes the game systems and sets up the Verlet system.
    /// </summary>
    protected override void Initialize()
    {
        _verletSystem = new VerletSystem(
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );

        _random = new Random();

        base.Initialize();
    }

    /// <summary>
    /// Loads game content such as textures and sets up initial points and springs.
    /// </summary>
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Initialize circle and line rendering utilities
        Circle.Initialize(GraphicsDevice);
        Line.Initialize(GraphicsDevice);

        // Create example points
        CreateRandomVerletPoint(new Vector2(200, 100));
        CreateRandomVerletPoint(new Vector2(300, 200));
        CreateRandomVerletPoint(new Vector2(500, 150));

        // Create a spring between two custom points
        var pA = _verletSystem.CreatePoint(new Vector2(200, 100), 15, 5, Color.Cyan);
        var pB = _verletSystem.CreatePoint(new Vector2(300, 150), 15, 5, Color.Magenta);
        _verletSystem.CreateSpring(pA, pB, stiffness: 0.001f, thickness: 10f);

        // Add a fixed anchor point at the center
        _verletSystem.CreatePoint(
            new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2),
            15,
            10,
            Color.White,
            true
        );
    }

    /// <summary>
    /// Handles user input, updates physics system, and creates points on interaction.
    /// </summary>
    /// <param name="gameTime">Provides timing values.</param>
    protected override void Update(GameTime gameTime)
    {
        // Exit on Back or Escape
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // Capture mouse state
        _previousMouseState = _currentMouseState;
        _currentMouseState = Mouse.GetState();

        // Create point on left click
        if (_currentMouseState.LeftButton == ButtonState.Pressed &&
            _previousMouseState.LeftButton == ButtonState.Released)
        {
            Vector2 mousePosition = new Vector2(_currentMouseState.X, _currentMouseState.Y);
            CreateRandomVerletPoint(mousePosition);
        }

        // Touch input (first finger only)
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

        // Update Verlet physics
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _verletSystem.Update(deltaTime, 4); // 4 sub-steps for stability

        base.Update(gameTime);
    }

    /// <summary>
    /// Draws all points and springs in the physics system.
    /// </summary>
    /// <param name="gameTime">Provides timing values.</param>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();

        _verletSystem.Draw(_spriteBatch);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    /// <summary>
    /// Creates a Verlet point with random radius, mass, and color at the given position.
    /// </summary>
    /// <param name="position">The position to place the point.</param>
    private void CreateRandomVerletPoint(Vector2 position)
    {
        float radius = _random.Next(10, 31);
        float mass = radius * radius * 0.01f;
        Color color = new Color(
            (float)_random.NextDouble(),
            (float)_random.NextDouble(),
            (float)_random.NextDouble()
        );

        _verletSystem.CreatePoint(position, radius, mass, color);
    }
}
