using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace DinoGrr.Core.UI
{
    /// <summary>
    /// Handles the main menu UI and navigation.
    /// </summary>
    public class MainMenu
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixelTexture;
        private readonly GraphicsDevice _graphicsDevice;

        private readonly List<string> _menuOptions;
        private int _selectedIndex = 0;
        private KeyboardState _previousKeyboardState;
        private KeyboardState _currentKeyboardState;

        // Colors
        private readonly Color _backgroundColor = Color.Black;
        private readonly Color _normalTextColor = Color.White;
        private readonly Color _selectedTextColor = Color.Yellow;
        private readonly Color _titleColor = Color.LightBlue;

        // Spacing
        private const int MENU_SPACING = 60;
        private const int TITLE_SPACING = 100;

        /// <summary>
        /// Event fired when a menu option is selected.
        /// </summary>
        public event Action<int> OnMenuOptionSelected;

        /// <summary>
        /// Creates a new MainMenu instance.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch to use for drawing.</param>
        /// <param name="font">The font to use for text rendering.</param>
        /// <param name="pixelTexture">A 1x1 white pixel texture for drawing backgrounds.</param>
        /// <param name="graphicsDevice">The graphics device for getting screen dimensions.</param>
        public MainMenu(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture, GraphicsDevice graphicsDevice)
        {
            _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
            _font = font ?? throw new ArgumentNullException(nameof(font));
            _pixelTexture = pixelTexture ?? throw new ArgumentNullException(nameof(pixelTexture));
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));

            _menuOptions = new List<string>
            {
                "Play",
                "Level Selector",
                "Options",
                "Exit"
            };

            _previousKeyboardState = Keyboard.GetState();
        }

        /// <summary>
        /// Updates the main menu input handling.
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

            // Handle menu selection
            if (IsKeyPressed(Keys.Enter) || IsKeyPressed(Keys.Space))
            {
                OnMenuOptionSelected?.Invoke(_selectedIndex);
            }

            _previousKeyboardState = _currentKeyboardState;
        }

        /// <summary>
        /// Draws the main menu.
        /// </summary>
        public void Draw()
        {
            // Clear the screen with background color
            _graphicsDevice.Clear(_backgroundColor);

            // Get screen dimensions
            int screenWidth = _graphicsDevice.Viewport.Width;
            int screenHeight = _graphicsDevice.Viewport.Height;
            Vector2 screenCenter = new Vector2(screenWidth / 2, screenHeight / 2);

            // Draw title
            string title = "DinoGrr";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePosition = new Vector2(screenCenter.X - titleSize.X / 2, screenCenter.Y - titleSize.Y / 2 - TITLE_SPACING);

            _spriteBatch.DrawString(_font, title, titlePosition, _titleColor);

            // Draw menu options
            for (int i = 0; i < _menuOptions.Count; i++)
            {
                string option = _menuOptions[i];
                Vector2 optionSize = _font.MeasureString(option);
                Vector2 optionPosition = new Vector2(
                    screenCenter.X - optionSize.X / 2,
                    screenCenter.Y - optionSize.Y / 2 + (i * MENU_SPACING)
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
