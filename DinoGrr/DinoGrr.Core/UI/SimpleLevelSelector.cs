using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace DinoGrr.Core.UI
{
    /// <summary>
    /// Simple level selector menu that shows numbered levels and a back option.
    /// </summary>
    public class SimpleLevelSelector
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixelTexture;
        private readonly GraphicsDevice _graphicsDevice;

        private KeyboardState _previousKeyboardState;
        private KeyboardState _currentKeyboardState;

        private readonly List<string> _menuOptions;
        private int _selectedIndex = 0;

        // UI Colors
        private readonly Color _backgroundColor = Color.Black;
        private readonly Color _titleColor = Color.LightBlue;
        private readonly Color _normalTextColor = Color.White;
        private readonly Color _selectedTextColor = Color.Yellow;

        // UI Layout
        private const int TITLE_Y = 100;
        private const int MENU_START_Y = 200;
        private const int MENU_SPACING = 50;

        /// <summary>
        /// Event fired when the back button is clicked.
        /// </summary>
        public event Action OnBackClicked;

        /// <summary>
        /// Event fired when a level is selected to play.
        /// </summary>
        public event Action<int> OnLevelSelected;

        /// <summary>
        /// Creates a new SimpleLevelSelector instance.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch to use for drawing.</param>
        /// <param name="font">The font to use for text rendering.</param>
        /// <param name="pixelTexture">A 1x1 white pixel texture for drawing rectangles.</param>
        /// <param name="graphicsDevice">The graphics device for getting screen dimensions.</param>
        public SimpleLevelSelector(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture,
            GraphicsDevice graphicsDevice)
        {
            _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
            _font = font ?? throw new ArgumentNullException(nameof(font));
            _pixelTexture = pixelTexture ?? throw new ArgumentNullException(nameof(pixelTexture));
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));

            _previousKeyboardState = Keyboard.GetState();

            // Create simple menu options (no levels for now, just empty placeholders and back)
            _menuOptions = new List<string>
            {
                "Level 1 - [Coming Soon]",
                "Level 2 - [Coming Soon]",
                "Level 3 - [Coming Soon]",
                "Level 4 - [Coming Soon]",
                "Level 5 - [Coming Soon]",
                "Back"
            };
        }

        /// <summary>
        /// Updates the level selector input handling.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        public void Update(GameTime gameTime)
        {
            _currentKeyboardState = Keyboard.GetState();

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
                if (_selectedIndex == _menuOptions.Count - 1) // Back option
                {
                    OnBackClicked?.Invoke();
                }
                else
                {
                    // For now, don't trigger level selection since levels are coming soon
                    // OnLevelSelected?.Invoke(_selectedIndex + 1);
                }
            }

            // Handle escape key
            if (IsKeyPressed(Keys.Escape))
            {
                OnBackClicked?.Invoke();
            }

            _previousKeyboardState = _currentKeyboardState;
        }

        /// <summary>
        /// Draws the level selector.
        /// </summary>
        public void Draw()
        {
            // Clear the screen with background color
            _graphicsDevice.Clear(_backgroundColor);

            int screenWidth = _graphicsDevice.Viewport.Width;
            int screenHeight = _graphicsDevice.Viewport.Height;
            Vector2 screenCenter = new Vector2(screenWidth / 2, screenHeight / 2);

            // Draw title
            string title = "Level Selector";
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
            _spriteBatch.DrawString(_font, instructions, instructionsPosition, Color.Gray);
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
