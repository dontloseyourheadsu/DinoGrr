using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using DinoGrr.Core.UI;
using DinoGrr.Core.Database;
using DinoGrr.Core.Database.Repositories;

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
        private OptionsMenu _optionsMenu;
        private SimpleLevelSelector _levelSelector;
        private LevelEditorSelect _levelEditorSelect;
        private LevelEditor _levelEditor;
        private SpriteFont _font;
        private Texture2D _pixelTexture;

        // Database System (manual dependency injection)
        // Removed for now - using simple level selector

        // Gameplay state
        private GameplayState _gameplayState;

        /// <summary>
        /// Gets the current music volume setting (0.0 to 1.0).
        /// </summary>
        public float MusicVolume => _optionsMenu?.MusicVolume ?? 0.5f;

        /// <summary>
        /// Gets the current sound volume setting (0.0 to 1.0).
        /// </summary>
        public float SoundVolume => _optionsMenu?.SoundVolume ?? 0.5f;

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

            // Initialize options menu
            _optionsMenu = new OptionsMenu(_spriteBatch, _font, _pixelTexture, GraphicsDevice);
            _optionsMenu.OnBackClicked += () => _currentGameState = GameState.MainMenu;

            // Initialize level selector
            _levelSelector = new SimpleLevelSelector(_spriteBatch, _font, _pixelTexture, GraphicsDevice);
            _levelSelector.OnBackClicked += () => _currentGameState = GameState.MainMenu;
            _levelSelector.OnLevelSelected += HandleLevelSelection;

            // Initialize level editor select
            _levelEditorSelect = new LevelEditorSelect(_spriteBatch, _font, _pixelTexture, GraphicsDevice);
            _levelEditorSelect.OnBackClicked += () => _currentGameState = GameState.MainMenu;
            _levelEditorSelect.OnEditLevel += HandleEditLevel;

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
                case 2: // Level Editor
                    _currentGameState = GameState.LevelEditorSelect;
                    break;
                case 3: // Options
                    _currentGameState = GameState.Options;
                    break;
                case 4: // Exit
                    Exit();
                    break;
            }
        }

        /// <summary>
        /// Handles level selection from the level selector.
        /// </summary>
        /// <param name="levelId">The ID of the selected level.</param>
        private void HandleLevelSelection(int levelId)
        {
            // TODO: Start the specific level
            // For now, just start the regular gameplay
            _currentGameState = GameState.Playing;
        }

        /// <summary>
        /// Handles editing a level in the level editor.
        /// </summary>
        /// <param name="editorData">The level data to edit.</param>
        private void HandleEditLevel(Database.Models.LevelEditorData editorData)
        {
            // Create level editor instance
            _levelEditor = new LevelEditor(_spriteBatch, _font, _pixelTexture, GraphicsDevice, editorData);
            _levelEditor.OnExitEditor += () => _currentGameState = GameState.LevelEditorSelect;

            // Switch to level editor state
            _currentGameState = GameState.LevelEditor;
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
                    _levelSelector.Update(gameTime);
                    break;
                case GameState.LevelEditorSelect:
                    _levelEditorSelect.Update(gameTime);
                    break;
                case GameState.LevelEditor:
                    _levelEditor?.Update(gameTime);
                    break;
                case GameState.Options:
                    _optionsMenu.Update(gameTime);
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
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    _levelSelector.Draw();
                    _spriteBatch.End();
                    break;
                case GameState.LevelEditorSelect:
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    _levelEditorSelect.Draw();
                    _spriteBatch.End();
                    break;
                case GameState.LevelEditor:
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    _levelEditor?.Draw();
                    _spriteBatch.End();
                    break;
                case GameState.Options:
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    _optionsMenu.Draw();
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

        /// <summary>
        /// Called when the game is exiting to save any pending data.
        /// </summary>
        protected override void OnExiting(object sender, ExitingEventArgs args)
        {
            // No longer saving level progress with simple level selector
            base.OnExiting(sender, args);
        }
    }
}