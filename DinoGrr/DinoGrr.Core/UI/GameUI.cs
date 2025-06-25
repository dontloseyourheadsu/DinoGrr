using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DinoGrr.Core.Entities.Player;
using System;

namespace DinoGrr.Core.UI;

/// <summary>
/// Handles the user interface for displaying game information like life points.
/// </summary>
public class GameUI
{
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly DinoGirl _dinoGirl;
    private readonly Texture2D _pixelTexture;

    // UI colors
    private readonly Color _lifeTextColor = Color.White;
    private readonly Color _lifeBackgroundColor = Color.Black * 0.5f;
    private readonly Color _invincibilityColor = Color.Red;

    // UI positioning
    private readonly Vector2 _lifePosition = new Vector2(20, 20);
    private readonly Vector2 _invincibilityPosition = new Vector2(20, 50);

    /// <summary>
    /// Creates a new GameUI instance.
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch to use for drawing.</param>
    /// <param name="font">The font to use for text rendering.</param>
    /// <param name="dinoGirl">The DinoGirl to monitor for UI updates.</param>
    /// <param name="pixelTexture">A 1x1 white pixel texture for drawing backgrounds.</param>
    public GameUI(SpriteBatch spriteBatch, SpriteFont font, DinoGirl dinoGirl, Texture2D pixelTexture)
    {
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _font = font ?? throw new ArgumentNullException(nameof(font));
        _dinoGirl = dinoGirl ?? throw new ArgumentNullException(nameof(dinoGirl));
        _pixelTexture = pixelTexture ?? throw new ArgumentNullException(nameof(pixelTexture));
    }

    /// <summary>
    /// Draws the game UI elements.
    /// </summary>
    public void Draw()
    {
        // Draw life points
        string lifeText = $"Life: {_dinoGirl.CurrentLifePoints}/{_dinoGirl.MaxLifePoints}";
        Vector2 lifeTextSize = _font.MeasureString(lifeText);

        // Draw background for life text
        Rectangle lifeBackground = new Rectangle(
            (int)_lifePosition.X - 5,
            (int)_lifePosition.Y - 5,
            (int)lifeTextSize.X + 10,
            (int)lifeTextSize.Y + 10
        );
        _spriteBatch.Draw(_pixelTexture, lifeBackground, _lifeBackgroundColor);

        // Draw life text
        _spriteBatch.DrawString(_font, lifeText, _lifePosition, _lifeTextColor);

        // Draw invincibility status if active
        if (_dinoGirl.IsInvincible)
        {
            string invincibilityText = "INVINCIBLE!";
            Vector2 invincibilityTextSize = _font.MeasureString(invincibilityText);

            // Draw background for invincibility text
            Rectangle invincibilityBackground = new Rectangle(
                (int)_invincibilityPosition.X - 5,
                (int)_invincibilityPosition.Y - 5,
                (int)invincibilityTextSize.X + 10,
                (int)invincibilityTextSize.Y + 10
            );
            _spriteBatch.Draw(_pixelTexture, invincibilityBackground, _lifeBackgroundColor);

            // Draw invincibility text with pulsing effect
            float pulse = (float)(Math.Sin(DateTime.Now.Millisecond * 0.01) * 0.3 + 0.7);
            Color pulsedColor = _invincibilityColor * pulse;
            _spriteBatch.DrawString(_font, invincibilityText, _invincibilityPosition, pulsedColor);
        }

        // Draw game over message if life is 0
        if (_dinoGirl.CurrentLifePoints <= 0)
        {
            string gameOverText = "GAME OVER - Press R to Restart";
            Vector2 gameOverTextSize = _font.MeasureString(gameOverText);
            Vector2 screenCenter = new Vector2(400, 300); // Adjust based on your screen size
            Vector2 gameOverPosition = screenCenter - gameOverTextSize / 2;

            // Draw background
            Rectangle gameOverBackground = new Rectangle(
                (int)gameOverPosition.X - 10,
                (int)gameOverPosition.Y - 10,
                (int)gameOverTextSize.X + 20,
                (int)gameOverTextSize.Y + 20
            );
            _spriteBatch.Draw(_pixelTexture, gameOverBackground, Color.Black * 0.8f);

            // Draw game over text
            _spriteBatch.DrawString(_font, gameOverText, gameOverPosition, Color.Red);
        }
    }
}
