using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DinoGrr.Core.Rendering.Animations;

/// <summary>
/// Handles sprite sheet animation for DinoGirl character.
/// </summary>
public class DinoGirlSpriteSheet
{
    private Texture2D _texture;
    private int _currentFrame;
    private int _totalFrames;
    private float _timeSinceLastFrame;
    private float _frameTime;
    private DinoGirlAnimation _currentAnimation;

    // Animation frame dimensions
    private const int FRAME_WIDTH = 100;
    private const int FRAME_HEIGHT = 180;
    private const int FRAME_SPACING = 20;
    private const int FRAMES_PER_ANIMATION = 8;

    // Rows in the sprite sheet (0 = Run, 1 = Hit)
    private const int RUN_ROW = 0;
    private const int HIT_ROW = 1;

    /// <summary>
    /// Gets the current source rectangle for the animation frame.
    /// </summary>
    public Rectangle SourceRectangle { get; private set; }

    /// <summary>
    /// Gets or sets whether the character is facing left.
    /// </summary>
    public bool FacingLeft { get; set; }

    /// <summary>
    /// Gets the appropriate sprite effects based on facing direction.
    /// </summary>
    public SpriteEffects SpriteEffect => FacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

    /// <summary>
    /// Creates a new DinoGirl sprite sheet handler.
    /// </summary>
    /// <param name="texture">The sprite sheet texture.</param>
    public DinoGirlSpriteSheet(Texture2D texture)
    {
        _texture = texture;
        _totalFrames = FRAMES_PER_ANIMATION;
        _currentFrame = 0;
        _timeSinceLastFrame = 0;
        _frameTime = 0.1f; // Animation speed (seconds per frame)
        _currentAnimation = DinoGirlAnimation.Running;

        UpdateSourceRectangle();
    }

    /// <summary>
    /// Sets the current animation.
    /// </summary>
    public void SetAnimation(DinoGirlAnimation animation)
    {
        // Only reset frame if changing animation
        if (_currentAnimation != animation)
        {
            _currentAnimation = animation;
            _currentFrame = 0;
            _timeSinceLastFrame = 0;
            UpdateSourceRectangle();
        }
    }

    /// <summary>
    /// Updates the animation frame based on elapsed time.
    /// </summary>
    public void Update(float deltaTime)
    {
        _timeSinceLastFrame += deltaTime;

        if (_timeSinceLastFrame >= _frameTime)
        {
            _timeSinceLastFrame -= _frameTime;
            _currentFrame = (_currentFrame + 1) % _totalFrames;
            UpdateSourceRectangle();
        }
    }

    /// <summary>
    /// Updates the source rectangle based on current animation and frame.
    /// </summary>
    private void UpdateSourceRectangle()
    {
        int row = _currentAnimation switch
        {
            DinoGirlAnimation.Running => RUN_ROW,
            DinoGirlAnimation.Hit => HIT_ROW,
            DinoGirlAnimation.Idle => RUN_ROW, // Use first frame of run as idle
            _ => RUN_ROW
        };

        // For idle, always use the first frame
        int frame = _currentAnimation == DinoGirlAnimation.Idle ? 0 : _currentFrame;

        int x = frame * (FRAME_WIDTH + FRAME_SPACING);
        int y = row * (FRAME_HEIGHT + FRAME_SPACING);

        SourceRectangle = new Rectangle(x, y, FRAME_WIDTH, FRAME_HEIGHT);
    }
}