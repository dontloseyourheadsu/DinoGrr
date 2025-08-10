using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DinoGrr.Core.UI
{
    /// <summary>
    /// Level editor select screen - empty for now, will be implemented later.
    /// </summary>
    public class LevelEditorSelect
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixelTexture;
        private readonly GraphicsDevice _graphicsDevice;

        private KeyboardState _previousKeyboardState;
        private KeyboardState _currentKeyboardState;

        // UI Colors
        private readonly Color _backgroundColor = Color.Black;
        private readonly Color _titleColor = Color.LightBlue;
        private readonly Color _textColor = Color.White;
        private readonly Color _backButtonColor = Color.Red;
        private readonly Color _backButtonSelectedColor = Color.Yellow;

        // UI Layout
        private const int TITLE_Y = 100;
        private const int MESSAGE_Y = 300;
        private const int BACK_BUTTON_Y = 500;

        /// <summary>
        /// Event fired when the back button is clicked.
        /// </summary>
        public event Action OnBackClicked;

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
        }

        /// <summary>
        /// Updates the level editor select input handling.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        public void Update(GameTime gameTime)
        {
            _currentKeyboardState = Keyboard.GetState();

            // Handle escape key or Enter to go back
            if (IsKeyPressed(Keys.Escape) || IsKeyPressed(Keys.Enter) || IsKeyPressed(Keys.Space))
            {
                OnBackClicked?.Invoke();
            }

            _previousKeyboardState = _currentKeyboardState;
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

            // Draw title
            string title = "Level Editor Select";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePosition = new Vector2(screenCenter.X - titleSize.X / 2, TITLE_Y);
            _spriteBatch.DrawString(_font, title, titlePosition, _titleColor);

            // Draw placeholder message
            string message = "Level Editor Select - Coming Soon!";
            Vector2 messageSize = _font.MeasureString(message);
            Vector2 messagePosition = new Vector2(screenCenter.X - messageSize.X / 2, MESSAGE_Y);
            _spriteBatch.DrawString(_font, message, messagePosition, _textColor);

            // Draw back instruction
            string backText = "Press Escape, Enter, or Space to go back";
            Vector2 backTextSize = _font.MeasureString(backText);
            Vector2 backTextPosition = new Vector2(screenCenter.X - backTextSize.X / 2, BACK_BUTTON_Y);
            _spriteBatch.DrawString(_font, backText, backTextPosition, _backButtonColor);

            // Draw instructions at the bottom
            string instructions = "This screen will contain level editor selection in the future";
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
