using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using DinoGrr.Core.UI;

namespace DinoGrr.Core
{
    /// <summary>
    /// Main entry point for the DinoGrr game using MonoGame.
    /// Manages different game states including main menu and gameplay.
    /// 
    /// Controls:
    /// Main Menu:
    /// - Up/Down Arrow Keys: Navigate menu
    /// - Enter/Space: Select menu option
    /// - Escape: Exit game
    /// 
    /// Gameplay:
    /// - Arrow Keys: Move DinoGirl (Left/Right to walk, Up to jump)
    /// - G: Follow DinoGirl with camera
    /// - N: Cycle through dinosaurs for camera following
    /// - M: Follow a random dinosaur with camera
    /// - F: Free camera (stop following)
    /// - R: Restart game when game over
    /// - Escape: Return to main menu
    /// 
    /// Features:
    /// - Expanded world (5000x800) with multiple dinosaur species
    /// - Realistic dinosaur sizes and behaviors
    /// - Different AI types: Aggressive, Defensive, Passive, Territorial
    /// - 18 total dinosaurs across 10 different species
    /// - Smart spawning system to prevent overlapping
    /// </summary>
    public class DinoGrrGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private GameState _currentGameState;
        private KeyboardState _currKeyboard, _prevKeyboard;

        // UI System
        private MainMenu _mainMenu;
        private SpriteFont _font;
        private Texture2D _pixelTexture;

        // Gameplay state
        private GameplayState _gameplayState;

        /// <summary>
        /// Constructs the game and initializes graphics settings.
        /// </summary>
        public DinoGrrGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set default window size independent of virtual world size
            // Use a reasonable window size that fits on most screens
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;

            // Allow the user to resize the window freely
            Window.AllowUserResizing = true;

            // Start with main menu
            _currentGameState = GameState.MainMenu;
        }

        /// <summary>
        /// Initializes core systems.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// Loads game content and initializes graphics objects.
        /// </summary>
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load UI font and create pixel texture
            _font = Content.Load<SpriteFont>("Fonts/Hud");

            // Create a 1x1 white pixel texture for UI backgrounds
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // Initialize main menu
            _mainMenu = new MainMenu(_spriteBatch, _font, _pixelTexture, GraphicsDevice);
            _mainMenu.OnMenuOptionSelected += HandleMenuSelection;

            // Initialize gameplay state
            _gameplayState = new GameplayState(_graphics, _spriteBatch, this);
            _gameplayState.Initialize();
            _gameplayState.LoadContent();

            // Automatically update viewport and camera when window is resized
            Window.ClientSizeChanged += (_, __) =>
            {
                _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                _graphics.ApplyChanges();
            };
        }

        /// <summary>
        /// Handles main menu selection.
        /// </summary>
        /// <param name="selectedIndex">The index of the selected menu item.</param>
        private void HandleMenuSelection(int selectedIndex)
        {
            switch (selectedIndex)
            {
                case 0: // Play
                    _currentGameState = GameState.Playing;
                    break;
                case 1: // Level Selector
                    _currentGameState = GameState.LevelSelector;
                    break;
                case 2: // Options
                    _currentGameState = GameState.Options;
                    break;
                case 3: // Exit
                    Exit();
                    break;
            }
        }

        /// <summary>
        /// Updates the game state: handles input and current game state.
        /// </summary>
        /// <param name="gameTime">Time since last update.</param>
        protected override void Update(GameTime gameTime)
        {
            // Exit on Back button (GamePad) or Escape key (in main menu)
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            // Track keyboard state for detecting key presses
            _prevKeyboard = _currKeyboard;
            _currKeyboard = Keyboard.GetState();

            // Handle escape key to return to main menu (except when already in main menu)
            if (IsKeyPressed(Keys.Escape))
            {
                if (_currentGameState == GameState.MainMenu)
                {
                    Exit();
                }
                else
                {
                    _currentGameState = GameState.MainMenu;
                }
            }

            // Update based on current game state
            switch (_currentGameState)
            {
                case GameState.MainMenu:
                    _mainMenu.Update(gameTime);
                    break;
                case GameState.Playing:
                    _gameplayState.Update(gameTime);
                    break;
                case GameState.LevelSelector:
                    // TODO: Implement level selector
                    break;
                case GameState.Options:
                    // TODO: Implement options menu
                    break;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Renders the game based on current state.
        /// </summary>
        /// <param name="gameTime">Time snapshot for the current frame.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Draw based on current game state
            switch (_currentGameState)
            {
                case GameState.MainMenu:
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    _mainMenu.Draw();
                    _spriteBatch.End();
                    break;
                case GameState.Playing:
                    _gameplayState.Draw(gameTime);
                    break;
                case GameState.LevelSelector:
                    // TODO: Draw level selector
                    GraphicsDevice.Clear(Color.Blue);
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    _spriteBatch.DrawString(_font, "Level Selector - Press Escape to return to main menu", new Vector2(100, 100), Color.White);
                    _spriteBatch.End();
                    break;
                case GameState.Options:
                    // TODO: Draw options menu
                    GraphicsDevice.Clear(Color.Green);
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    _spriteBatch.DrawString(_font, "Options - Press Escape to return to main menu", new Vector2(100, 100), Color.White);
                    _spriteBatch.End();
                    break;
            }

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