using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using DinoGrr.Core.Database.Models;

namespace DinoGrr.Core.UI
{
    /// <summary>
    /// Basic level editor view - blank white screen for now, will be implemented later.
    /// </summary>
    public class LevelEditor
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixelTexture;
        private readonly GraphicsDevice _graphicsDevice;

        private KeyboardState _previousKeyboardState;
        private KeyboardState _currentKeyboardState;

        private readonly LevelEditorData _editorData;

        // UI Colors
        private readonly Color _backgroundColor = Color.White;
        private readonly Color _instructionTextColor = Color.Black;

        /// <summary>
        /// Event fired when the user wants to exit the level editor.
        /// </summary>
        public event Action OnExitEditor;

        /// <summary>
        /// Creates a new LevelEditor instance.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch to use for drawing.</param>
        /// <param name="font">The font to use for text rendering.</param>
        /// <param name="pixelTexture">A 1x1 white pixel texture for drawing backgrounds.</param>
        /// <param name="graphicsDevice">The graphics device for getting screen dimensions.</param>
        /// <param name="editorData">The level data to edit.</param>
        public LevelEditor(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture,
            GraphicsDevice graphicsDevice, LevelEditorData editorData)
        {
            _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
            _font = font ?? throw new ArgumentNullException(nameof(font));
            _pixelTexture = pixelTexture ?? throw new ArgumentNullException(nameof(pixelTexture));
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            _editorData = editorData ?? throw new ArgumentNullException(nameof(editorData));

            _previousKeyboardState = Keyboard.GetState();

            Console.WriteLine($"LevelEditor: {(_editorData.IsNewLevel ? "Creating new level" : $"Editing level #{_editorData.LevelId}: {_editorData.Name}")}");
        }

        /// <summary>
        /// Updates the level editor input handling.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        public void Update(GameTime gameTime)
        {
            _currentKeyboardState = Keyboard.GetState();

            // Handle escape key to exit editor
            if (IsKeyPressed(Keys.Escape))
            {
                OnExitEditor?.Invoke();
            }

            _previousKeyboardState = _currentKeyboardState;
        }

        /// <summary>
        /// Draws the level editor screen.
        /// </summary>
        public void Draw()
        {
            // Clear the screen with white background
            _graphicsDevice.Clear(_backgroundColor);

            int screenWidth = _graphicsDevice.Viewport.Width;
            int screenHeight = _graphicsDevice.Viewport.Height;

            // Draw exit instruction at the bottom
            string exitText = "Press Escape to go back";
            Vector2 exitTextSize = _font.MeasureString(exitText);
            Vector2 exitTextPosition = new Vector2(
                screenWidth - exitTextSize.X - 20, // 20 pixels from right edge
                screenHeight - exitTextSize.Y - 20 // 20 pixels from bottom edge
            );
            _spriteBatch.DrawString(_font, exitText, exitTextPosition, _instructionTextColor);
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
