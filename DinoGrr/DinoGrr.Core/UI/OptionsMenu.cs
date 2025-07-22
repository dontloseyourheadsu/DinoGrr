using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DinoGrr.Core.UI
{
    /// <summary>
    /// Handles the options menu UI with music and sound volume sliders.
    /// </summary>
    public class OptionsMenu
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixelTexture;
        private readonly GraphicsDevice _graphicsDevice;

        private KeyboardState _previousKeyboardState;
        private KeyboardState _currentKeyboardState;
        private MouseState _previousMouseState;
        private MouseState _currentMouseState;

        // Audio settings (stored as simple variables for now)
        public float MusicVolume { get; set; } = 0.5f; // Default 50%
        public float SoundVolume { get; set; } = 0.5f; // Default 50%

        // UI Colors
        private readonly Color _backgroundColor = Color.Black;
        private readonly Color _titleColor = Color.LightBlue;
        private readonly Color _labelColor = Color.White;
        private readonly Color _sliderBarColor = Color.Gray;
        private readonly Color _sliderFillColor = Color.Yellow;
        private readonly Color _sliderHandleColor = Color.White;
        private readonly Color _backButtonColor = Color.Red;
        private readonly Color _backButtonSelectedColor = Color.Yellow;

        // UI Layout
        private const int TITLE_Y = 100;
        private const int FIRST_SLIDER_Y = 250;
        private const int SLIDER_SPACING = 100;
        private const int SLIDER_WIDTH = 300;
        private const int SLIDER_HEIGHT = 20;
        private const int SLIDER_HANDLE_WIDTH = 10;
        private const int BACK_BUTTON_Y = 500;

        // Interaction state
        private bool _isDraggingMusic = false;
        private bool _isDraggingSound = false;
        private bool _isBackButtonHovered = false;

        /// <summary>
        /// Event fired when the back button is clicked.
        /// </summary>
        public event Action OnBackClicked;

        /// <summary>
        /// Creates a new OptionsMenu instance.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch to use for drawing.</param>
        /// <param name="font">The font to use for text rendering.</param>
        /// <param name="pixelTexture">A 1x1 white pixel texture for drawing rectangles.</param>
        /// <param name="graphicsDevice">The graphics device for getting screen dimensions.</param>
        public OptionsMenu(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture, GraphicsDevice graphicsDevice)
        {
            _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
            _font = font ?? throw new ArgumentNullException(nameof(font));
            _pixelTexture = pixelTexture ?? throw new ArgumentNullException(nameof(pixelTexture));
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));

            _previousKeyboardState = Keyboard.GetState();
            _previousMouseState = Mouse.GetState();
        }

        /// <summary>
        /// Updates the options menu input handling.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        public void Update(GameTime gameTime)
        {
            _currentKeyboardState = Keyboard.GetState();
            _currentMouseState = Mouse.GetState();

            int screenWidth = _graphicsDevice.Viewport.Width;
            int screenHeight = _graphicsDevice.Viewport.Height;
            Vector2 mousePosition = new Vector2(_currentMouseState.X, _currentMouseState.Y);

            // Update sliders
            float musicVolume = MusicVolume;
            float soundVolume = SoundVolume;
            UpdateSlider(ref _isDraggingMusic, ref musicVolume, mousePosition, screenWidth, FIRST_SLIDER_Y);
            UpdateSlider(ref _isDraggingSound, ref soundVolume, mousePosition, screenWidth, FIRST_SLIDER_Y + SLIDER_SPACING);
            MusicVolume = musicVolume;
            SoundVolume = soundVolume;

            // Update back button
            Rectangle backButtonRect = GetBackButtonRectangle(screenWidth, screenHeight);
            _isBackButtonHovered = backButtonRect.Contains(mousePosition);

            // Handle back button click
            if (_isBackButtonHovered && IsMouseClicked())
            {
                OnBackClicked?.Invoke();
            }

            // Handle escape key
            if (IsKeyPressed(Keys.Escape))
            {
                OnBackClicked?.Invoke();
            }

            _previousKeyboardState = _currentKeyboardState;
            _previousMouseState = _currentMouseState;
        }

        /// <summary>
        /// Updates a slider's value based on mouse input.
        /// </summary>
        private void UpdateSlider(ref bool isDragging, ref float value, Vector2 mousePosition, int screenWidth, int sliderY)
        {
            Rectangle sliderRect = GetSliderRectangle(screenWidth, sliderY);

            // Check if mouse is over the slider
            bool mouseOverSlider = sliderRect.Contains(mousePosition);

            // Start dragging
            if (mouseOverSlider && _currentMouseState.LeftButton == ButtonState.Pressed && 
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                isDragging = true;
            }

            // Stop dragging
            if (_currentMouseState.LeftButton == ButtonState.Released)
            {
                isDragging = false;
            }

            // Update value while dragging
            if (isDragging)
            {
                float relativeX = mousePosition.X - sliderRect.X;
                float normalizedValue = MathHelper.Clamp(relativeX / sliderRect.Width, 0f, 1f);
                value = normalizedValue;
            }
        }

        /// <summary>
        /// Draws the options menu.
        /// </summary>
        public void Draw()
        {
            // Clear the screen with background color
            _graphicsDevice.Clear(_backgroundColor);

            int screenWidth = _graphicsDevice.Viewport.Width;
            int screenHeight = _graphicsDevice.Viewport.Height;

            // Draw title
            string title = "Options";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePosition = new Vector2(screenWidth / 2 - titleSize.X / 2, TITLE_Y);
            _spriteBatch.DrawString(_font, title, titlePosition, _titleColor);

            // Draw music volume slider
            DrawSlider("Music Volume", MusicVolume, screenWidth, FIRST_SLIDER_Y);

            // Draw sound volume slider
            DrawSlider("Sound Volume", SoundVolume, screenWidth, FIRST_SLIDER_Y + SLIDER_SPACING);

            // Draw back button
            DrawBackButton(screenWidth, screenHeight);
        }

        /// <summary>
        /// Draws a volume slider with label and percentage.
        /// </summary>
        private void DrawSlider(string label, float value, int screenWidth, int sliderY)
        {
            // Draw label
            Vector2 labelSize = _font.MeasureString(label);
            Vector2 labelPosition = new Vector2(screenWidth / 2 - labelSize.X / 2, sliderY - 30);
            _spriteBatch.DrawString(_font, label, labelPosition, _labelColor);

            // Get slider rectangle
            Rectangle sliderRect = GetSliderRectangle(screenWidth, sliderY);

            // Draw slider background
            _spriteBatch.Draw(_pixelTexture, sliderRect, _sliderBarColor);

            // Draw slider fill
            Rectangle fillRect = new Rectangle(
                sliderRect.X,
                sliderRect.Y,
                (int)(sliderRect.Width * value),
                sliderRect.Height
            );
            _spriteBatch.Draw(_pixelTexture, fillRect, _sliderFillColor);

            // Draw slider handle
            int handleX = sliderRect.X + (int)(sliderRect.Width * value) - SLIDER_HANDLE_WIDTH / 2;
            Rectangle handleRect = new Rectangle(
                handleX,
                sliderRect.Y - 5,
                SLIDER_HANDLE_WIDTH,
                sliderRect.Height + 10
            );
            _spriteBatch.Draw(_pixelTexture, handleRect, _sliderHandleColor);

            // Draw percentage
            string percentage = $"{(int)(value * 100)}%";
            Vector2 percentageSize = _font.MeasureString(percentage);
            Vector2 percentagePosition = new Vector2(screenWidth / 2 - percentageSize.X / 2, sliderY + SLIDER_HEIGHT + 10);
            _spriteBatch.DrawString(_font, percentage, percentagePosition, _labelColor);
        }

        /// <summary>
        /// Draws the back button.
        /// </summary>
        private void DrawBackButton(int screenWidth, int screenHeight)
        {
            string backText = "Back to Main Menu";
            Vector2 backTextSize = _font.MeasureString(backText);
            Vector2 backTextPosition = new Vector2(screenWidth / 2 - backTextSize.X / 2, BACK_BUTTON_Y);

            Color backColor = _isBackButtonHovered ? _backButtonSelectedColor : _backButtonColor;
            _spriteBatch.DrawString(_font, backText, backTextPosition, backColor);
        }

        /// <summary>
        /// Gets the rectangle for a slider at the specified Y position.
        /// </summary>
        private Rectangle GetSliderRectangle(int screenWidth, int sliderY)
        {
            return new Rectangle(
                screenWidth / 2 - SLIDER_WIDTH / 2,
                sliderY,
                SLIDER_WIDTH,
                SLIDER_HEIGHT
            );
        }

        /// <summary>
        /// Gets the rectangle for the back button.
        /// </summary>
        private Rectangle GetBackButtonRectangle(int screenWidth, int screenHeight)
        {
            string backText = "Back to Main Menu";
            Vector2 backTextSize = _font.MeasureString(backText);
            return new Rectangle(
                (int)(screenWidth / 2 - backTextSize.X / 2),
                BACK_BUTTON_Y,
                (int)backTextSize.X,
                (int)backTextSize.Y
            );
        }

        /// <summary>
        /// Checks if a key was just pressed this frame.
        /// </summary>
        private bool IsKeyPressed(Keys key)
        {
            return _currentKeyboardState.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);
        }

        /// <summary>
        /// Checks if the mouse was just clicked this frame.
        /// </summary>
        private bool IsMouseClicked()
        {
            return _currentMouseState.LeftButton == ButtonState.Pressed && 
                   _previousMouseState.LeftButton == ButtonState.Released;
        }
    }
}
