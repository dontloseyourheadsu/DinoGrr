using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using DinoGrr.Core.Database;
using DinoGrr.Core.Database.Models;
using DinoGrr.Core.Database.Repositories;

namespace DinoGrr.Core.UI
{
    /// <summary>
    /// Level editor selection screen with options for New, Edit Local, and Back.
    /// </summary>
    public class LevelEditorSelect
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixelTexture;
        private readonly GraphicsDevice _graphicsDevice;

        private KeyboardState _previousKeyboardState;
        private KeyboardState _currentKeyboardState;

        private readonly List<string> _menuOptions;
        private IGameLevelsRepository _levelsRepository;
        private IReadOnlyList<GameLevel> _availableLevels;
        private int _selectedIndex = 0;
        private bool _showingLevelList = false;
        private int _levelListSelectedIndex = 0;

        // UI Colors
        private readonly Color _backgroundColor = Color.Black;
        private readonly Color _titleColor = Color.LightBlue;
        private readonly Color _normalTextColor = Color.White;
        private readonly Color _selectedTextColor = Color.Yellow;
        private readonly Color _instructionTextColor = Color.Gray;

        // UI Layout
        private const int TITLE_Y = 100;
        private const int MENU_START_Y = 200;
        private const int MENU_SPACING = 50;
        private const int LEVEL_LIST_START_Y = 250;
        private const int LEVEL_LIST_SPACING = 40;

        /// <summary>
        /// Event fired when the back button is clicked.
        /// </summary>
        public event Action OnBackClicked;

        /// <summary>
        /// Event fired when a level should be created/edited in the editor.
        /// </summary>
        public event Action<LevelEditorData> OnEditLevel;

        /// <summary>
        /// Creates a new LevelEditorSelect instance.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch to use for drawing.</param>
        /// <param name="font">The font to use for text rendering.</param>
        /// <param name="pixelTexture">A 1x1 white pixel texture for drawing rectangles.</param>
        /// <param name="graphicsDevice">The graphics device for getting screen dimensions.</param>
        public LevelEditorSelect(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture,
            GraphicsDevice graphicsDevice)
        {
            _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
            _font = font ?? throw new ArgumentNullException(nameof(font));
            _pixelTexture = pixelTexture ?? throw new ArgumentNullException(nameof(pixelTexture));
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));

            _previousKeyboardState = Keyboard.GetState();

            // Initialize menu options
            _menuOptions = new List<string>
            {
                "New",
                "Edit Local",
                "Back"
            };

            // Initialize repository and load levels
            try
            {
                var factory = new DatabaseFactory();
                _levelsRepository = new GameLevelsRepository(factory);
                _availableLevels = _levelsRepository.GetAllLevels() ?? new List<GameLevel>();
                Console.WriteLine($"LevelEditorSelect: found {_availableLevels.Count} levels available for editing");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LevelEditorSelect: failed to load levels: {ex.Message}");
                _availableLevels = new List<GameLevel>();
            }
        }

        /// <summary>
        /// Updates the level editor select input handling.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        public void Update(GameTime gameTime)
        {
            _currentKeyboardState = Keyboard.GetState();

            if (_showingLevelList)
            {
                HandleLevelListInput();
            }
            else
            {
                HandleMainMenuInput();
            }

            _previousKeyboardState = _currentKeyboardState;
        }

        /// <summary>
        /// Handles input for the main menu options.
        /// </summary>
        private void HandleMainMenuInput()
        {
            // Handle menu navigation
            if (IsKeyPressed(Keys.Up))
            {
                _selectedIndex = (_selectedIndex - 1 + _menuOptions.Count) % _menuOptions.Count;
            }
            else if (IsKeyPressed(Keys.Down))
            {
                _selectedIndex = (_selectedIndex + 1) % _menuOptions.Count;
            }

            // Handle selection
            if (IsKeyPressed(Keys.Enter) || IsKeyPressed(Keys.Space))
            {
                switch (_selectedIndex)
                {
                    case 0: // New
                        HandleNewLevel();
                        break;
                    case 1: // Edit Local
                        HandleEditLocal();
                        break;
                    case 2: // Back
                        OnBackClicked?.Invoke();
                        break;
                }
            }

            // Handle escape key
            if (IsKeyPressed(Keys.Escape))
            {
                OnBackClicked?.Invoke();
            }
        }

        /// <summary>
        /// Handles input for the level list when editing local levels.
        /// </summary>
        private void HandleLevelListInput()
        {
            // Handle navigation
            if (IsKeyPressed(Keys.Up))
            {
                _levelListSelectedIndex = (_levelListSelectedIndex - 1 + _availableLevels.Count + 1) % (_availableLevels.Count + 1);
            }
            else if (IsKeyPressed(Keys.Down))
            {
                _levelListSelectedIndex = (_levelListSelectedIndex + 1) % (_availableLevels.Count + 1);
            }

            // Handle selection
            if (IsKeyPressed(Keys.Enter) || IsKeyPressed(Keys.Space))
            {
                if (_levelListSelectedIndex == _availableLevels.Count) // Back option
                {
                    _showingLevelList = false;
                    _levelListSelectedIndex = 0;
                }
                else if (_levelListSelectedIndex < _availableLevels.Count)
                {
                    // Edit selected level
                    var selectedLevel = _availableLevels[_levelListSelectedIndex];
                    var editorData = new LevelEditorData(
                        selectedLevel.Id,
                        selectedLevel.Name,
                        selectedLevel.Description,
                        "{}" // TODO: Load actual level data JSON when level format is defined
                    );
                    OnEditLevel?.Invoke(editorData);
                }
            }

            // Handle escape key
            if (IsKeyPressed(Keys.Escape))
            {
                _showingLevelList = false;
                _levelListSelectedIndex = 0;
            }
        }

        /// <summary>
        /// Handles creating a new level.
        /// </summary>
        private void HandleNewLevel()
        {
            var editorData = new LevelEditorData(); // Creates a new level with default values
            OnEditLevel?.Invoke(editorData);
        }

        /// <summary>
        /// Handles showing the edit local levels menu.
        /// </summary>
        private void HandleEditLocal()
        {
            if (_availableLevels.Count == 0)
            {
                Console.WriteLine("No levels available to edit");
                return;
            }

            _showingLevelList = true;
            _levelListSelectedIndex = 0;
        }

        /// <summary>
        /// Draws the level editor select screen.
        /// </summary>
        public void Draw()
        {
            // Clear the screen with background color
            _graphicsDevice.Clear(_backgroundColor);

            int screenWidth = _graphicsDevice.Viewport.Width;
            int screenHeight = _graphicsDevice.Viewport.Height;
            Vector2 screenCenter = new Vector2(screenWidth / 2, screenHeight / 2);

            if (_showingLevelList)
            {
                DrawLevelList(screenCenter, screenHeight);
            }
            else
            {
                DrawMainMenu(screenCenter, screenHeight);
            }
        }

        /// <summary>
        /// Draws the main menu options.
        /// </summary>
        private void DrawMainMenu(Vector2 screenCenter, int screenHeight)
        {
            // Draw title
            string title = "Level Editor Select";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePosition = new Vector2(screenCenter.X - titleSize.X / 2, TITLE_Y);
            _spriteBatch.DrawString(_font, title, titlePosition, _titleColor);

            // Draw menu options
            for (int i = 0; i < _menuOptions.Count; i++)
            {
                string option = _menuOptions[i];
                Vector2 optionSize = _font.MeasureString(option);
                Vector2 optionPosition = new Vector2(
                    screenCenter.X - optionSize.X / 2,
                    MENU_START_Y + (i * MENU_SPACING)
                );

                // Determine color based on selection
                Color textColor = (i == _selectedIndex) ? _selectedTextColor : _normalTextColor;

                // Draw selection indicator
                if (i == _selectedIndex)
                {
                    string indicator = "> ";
                    Vector2 indicatorSize = _font.MeasureString(indicator);
                    Vector2 indicatorPosition = new Vector2(
                        optionPosition.X - indicatorSize.X - 10,
                        optionPosition.Y
                    );
                    _spriteBatch.DrawString(_font, indicator, indicatorPosition, _selectedTextColor);
                }

                // Draw the menu option
                _spriteBatch.DrawString(_font, option, optionPosition, textColor);
            }

            // Draw instructions at the bottom
            string instructions = "Use Arrow Keys to navigate, Enter to select, Escape to go back";
            Vector2 instructionsSize = _font.MeasureString(instructions);
            Vector2 instructionsPosition = new Vector2(
                screenCenter.X - instructionsSize.X / 2,
                screenHeight - 100
            );
            _spriteBatch.DrawString(_font, instructions, instructionsPosition, _instructionTextColor);
        }

        /// <summary>
        /// Draws the level list for editing local levels.
        /// </summary>
        private void DrawLevelList(Vector2 screenCenter, int screenHeight)
        {
            // Draw title
            string title = "Select Level to Edit";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePosition = new Vector2(screenCenter.X - titleSize.X / 2, TITLE_Y);
            _spriteBatch.DrawString(_font, title, titlePosition, _titleColor);

            // Draw available levels
            for (int i = 0; i < _availableLevels.Count; i++)
            {
                var level = _availableLevels[i];
                string levelText = $"#{level.Id}: {level.Name}";
                Vector2 levelTextSize = _font.MeasureString(levelText);
                Vector2 levelPosition = new Vector2(
                    screenCenter.X - levelTextSize.X / 2,
                    LEVEL_LIST_START_Y + (i * LEVEL_LIST_SPACING)
                );

                // Determine color based on selection
                Color textColor = (i == _levelListSelectedIndex) ? _selectedTextColor : _normalTextColor;

                // Draw selection indicator
                if (i == _levelListSelectedIndex)
                {
                    string indicator = "> ";
                    Vector2 indicatorSize = _font.MeasureString(indicator);
                    Vector2 indicatorPosition = new Vector2(
                        levelPosition.X - indicatorSize.X - 10,
                        levelPosition.Y
                    );
                    _spriteBatch.DrawString(_font, indicator, indicatorPosition, _selectedTextColor);
                }

                // Draw the level option
                _spriteBatch.DrawString(_font, levelText, levelPosition, textColor);
            }

            // Draw Back option
            string backText = "Back";
            Vector2 backTextSize = _font.MeasureString(backText);
            Vector2 backPosition = new Vector2(
                screenCenter.X - backTextSize.X / 2,
                LEVEL_LIST_START_Y + (_availableLevels.Count * LEVEL_LIST_SPACING)
            );

            Color backColor = (_levelListSelectedIndex == _availableLevels.Count) ? _selectedTextColor : _normalTextColor;

            // Draw selection indicator for back
            if (_levelListSelectedIndex == _availableLevels.Count)
            {
                string indicator = "> ";
                Vector2 indicatorSize = _font.MeasureString(indicator);
                Vector2 indicatorPosition = new Vector2(
                    backPosition.X - indicatorSize.X - 10,
                    backPosition.Y
                );
                _spriteBatch.DrawString(_font, indicator, indicatorPosition, _selectedTextColor);
            }

            _spriteBatch.DrawString(_font, backText, backPosition, backColor);

            // Draw instructions at the bottom
            string instructions = "Use Arrow Keys to navigate, Enter to select, Escape to go back";
            Vector2 instructionsSize = _font.MeasureString(instructions);
            Vector2 instructionsPosition = new Vector2(
                screenCenter.X - instructionsSize.X / 2,
                screenHeight - 100
            );
            _spriteBatch.DrawString(_font, instructions, instructionsPosition, _instructionTextColor);
        }

        /// <summary>
        /// Checks if a key was just pressed this frame.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key was just pressed.</returns>
        private bool IsKeyPressed(Keys key)
        {
            return _currentKeyboardState.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);
        }
    }
}
