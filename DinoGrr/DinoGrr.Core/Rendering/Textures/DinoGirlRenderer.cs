using System;
using DinoGrr.Core.Entities.Player;
using DinoGrr.Core.Rendering.Animations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DinoGrr.Core.Rendering.Textures
{
    /// <summary>
    /// Represents the renderer for the DinoGirl character in the game.
    /// </summary>
    public class DinoGirlRenderer
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Texture2D _texture;
        private readonly DinoGirl _dinoGirl;
        private readonly TexturedSoftBodyMesh _texturedMesh;

        // Animation state
        private DinoGirlAnimation _currentAnimation;
        private int _currentFrame;
        private float _timeSinceLastFrame;
        private float _frameTime;
        private bool _facingLeft;

        // Animation frame dimensions
        private const int FRAME_WIDTH = 100;
        private const int FRAME_HEIGHT = 180;
        private const int FRAME_SPACING = 20;
        private const int FRAMES_PER_ANIMATION = 8;

        // Rows in the sprite sheet (0 = Run, 1 = Hit)
        private const int RUN_ROW = 0;
        private const int HIT_ROW = 1;

        // Track previous position for determining direction
        private Vector2 _lastPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="DinoGirlRenderer"/> class.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device for rendering.</param>
        /// <param name="texture">The DinoGirl texture (sprite sheet).</param>
        /// <param name="dinoGirl">The DinoGirl entity to render.</param>
        public DinoGirlRenderer(GraphicsDevice graphicsDevice, Texture2D texture, DinoGirl dinoGirl)
        {
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            _texture = texture ?? throw new ArgumentNullException(nameof(texture));
            _dinoGirl = dinoGirl ?? throw new ArgumentNullException(nameof(dinoGirl));

            // Initialize the textured mesh for soft body rendering
            _texturedMesh = new TexturedSoftBodyMesh(graphicsDevice, texture, dinoGirl.Body);

            // Initialize animation state
            _currentAnimation = DinoGirlAnimation.Idle;
            _currentFrame = 0;
            _timeSinceLastFrame = 0;
            _frameTime = 0.1f; // Animation speed (seconds per frame)
            _facingLeft = false;

            // Set initial animation frame
            UpdateSourceRectangle();

            // Track position for direction
            _lastPosition = CalculateCenter(dinoGirl.Points);
        }

        /// <summary>
        /// Updates the renderer to match the current state of the DinoGirl.
        /// </summary>
        public void Update(float deltaTime)
        {
            // Update the soft body mesh
            _texturedMesh.Update();

            // Calculate center for direction checking
            Vector2 center = CalculateCenter(_dinoGirl.Points);

            // Determine animation based on state
            if (!_dinoGirl.CanJump)
            {
                // In the air - use Hit animation as jumping
                SetAnimation(DinoGirlAnimation.Hit);
            }
            else
            {
                // Check if moving horizontally
                float xDiff = center.X - _lastPosition.X;

                if (Math.Abs(xDiff) > 0.5f)
                {
                    // Moving horizontally - run animation
                    SetAnimation(DinoGirlAnimation.Running);

                    // Update facing direction based on movement
                    _facingLeft = xDiff < 0;
                }
                else
                {
                    // Not moving - idle animation (first frame of Hit animation)
                    SetAnimation(DinoGirlAnimation.Idle);
                }
            }

            // Update animation frames
            _timeSinceLastFrame += deltaTime;
            if (_timeSinceLastFrame >= _frameTime)
            {
                _timeSinceLastFrame -= _frameTime;

                // Only advance frames if not idle
                if (_currentAnimation != DinoGirlAnimation.Idle)
                {
                    _currentFrame = (_currentFrame + 1) % FRAMES_PER_ANIMATION;
                }

                UpdateSourceRectangle();
            }

            // Store position for next update
            _lastPosition = center;
        }

        /// <summary>
        /// Sets the current animation if it's different from the current one.
        /// </summary>
        /// <param name="animation">The animation to set.</param>
        private void SetAnimation(DinoGirlAnimation animation)
        {
            if (_currentAnimation != animation)
            {
                _currentAnimation = animation;

                // Reset frame for new animation, except for idle which uses a specific frame
                if (animation != DinoGirlAnimation.Idle)
                {
                    _currentFrame = 0;
                }
                else
                {
                    // For idle, use the first frame of the Hit animation (second row)
                    _currentFrame = 0;
                }

                UpdateSourceRectangle();
            }
        }

        /// <summary>
        /// Updates the source rectangle based on current animation and frame.
        /// </summary>
        private void UpdateSourceRectangle()
        {
            int row;
            int frame;

            switch (_currentAnimation)
            {
                case DinoGirlAnimation.Running:
                    row = RUN_ROW;
                    frame = _currentFrame;
                    break;
                case DinoGirlAnimation.Hit:
                    row = HIT_ROW;
                    frame = _currentFrame;
                    break;
                case DinoGirlAnimation.Idle:
                    // Idle uses the first frame of the Hit animation (second row)
                    row = HIT_ROW;
                    frame = 0;
                    break;
                default:
                    row = RUN_ROW;
                    frame = 0;
                    break;
            }

            int x = frame * (FRAME_WIDTH + FRAME_SPACING);
            int y = row * (FRAME_HEIGHT + FRAME_SPACING);

            Rectangle sourceRect = new Rectangle(x, y, FRAME_WIDTH, FRAME_HEIGHT);
            _texturedMesh.SetSourceRectangle(sourceRect);
        }

        /// <summary>
        /// Draws the DinoGirl character using the current camera.
        /// </summary>
        public void Draw(Camera2D camera)
        {
            _texturedMesh.Draw(camera);
        }

        /// <summary>
        /// Calculates the center position of a collection of points.
        /// </summary>
        private Vector2 CalculateCenter(System.Collections.Generic.IReadOnlyList<Physics.VerletPoint> points)
        {
            if (points.Count == 0)
                return Vector2.Zero;

            Vector2 sum = Vector2.Zero;
            foreach (var point in points)
            {
                sum += point.Position;
            }
            return sum / points.Count;
        }
    }
}