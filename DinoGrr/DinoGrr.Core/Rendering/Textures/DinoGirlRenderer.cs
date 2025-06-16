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
        private DinoGirlAnimation _previousAnimation;
        private int _currentFrame;
        private float _timeSinceLastFrame;
        private float _frameTime;
        private bool _facingLeft;
        private float _transitionTime = 0.0f;
        private const float TRANSITION_DURATION = 0.15f; // Time in seconds for animation transitions

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
            _previousAnimation = DinoGirlAnimation.Idle; // Start with no transition
            _currentFrame = 0;
            _timeSinceLastFrame = 0;
            _frameTime = 0.1f; // Default animation speed (seconds per frame)
            _facingLeft = false;
            _transitionTime = 0.0f;

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

                // Maintain facing direction while jumping
                _facingLeft = _dinoGirl.FacingLeft;
                _texturedMesh.SetFlip(_facingLeft);

                // Reset to default animation speed
                _frameTime = 0.1f;
            }
            else if (_dinoGirl.IsWalking)
            {
                // Using the IsWalking property from DinoGirl
                SetAnimation(DinoGirlAnimation.Running);

                // Use the FacingLeft property from DinoGirl for direction
                _facingLeft = _dinoGirl.FacingLeft;

                // Apply the flip to the textured mesh
                _texturedMesh.SetFlip(_facingLeft);

                // Adjust animation speed based on movement speed
                // Faster movement = faster animation (within reasonable limits)
                float baseFrameTime = 0.1f;
                float normalizedSpeed = _dinoGirl.WalkSpeed / 1.5f; // Normalize relative to default walk speed
                float speedFactor = 1.0f / normalizedSpeed;
                _frameTime = MathHelper.Clamp(baseFrameTime * speedFactor, 0.05f, 0.15f);
            }
            else
            {
                // Not moving - idle animation (first frame of Hit animation)
                SetAnimation(DinoGirlAnimation.Idle);

                // Maintain the facing direction even when idle
                _facingLeft = _dinoGirl.FacingLeft;

                // Apply the flip to the textured mesh
                _texturedMesh.SetFlip(_facingLeft);

                // Reset to default animation speed for idle
                _frameTime = 0.1f;
            }

            // Update animation transitions
            if (_transitionTime < TRANSITION_DURATION)
            {
                _transitionTime += deltaTime;
            }

            // Update animation frames
            _timeSinceLastFrame += deltaTime;
            if (_timeSinceLastFrame >= _frameTime)
            {
                _timeSinceLastFrame -= _frameTime;

                // Only advance frames if not idle
                if (_currentAnimation != DinoGirlAnimation.Idle)
                {
                    // Calculate new frame with a smooth transition when changing animations
                    if (_transitionTime < TRANSITION_DURATION)
                    {
                        // During transition, adjust frame advancement speed based on the animation types
                        float transitionFactor = _transitionTime / TRANSITION_DURATION;

                        // Smooth incremental advancement during transitions
                        if ((_previousAnimation == DinoGirlAnimation.Running && _currentAnimation == DinoGirlAnimation.Hit) ||
                            (_previousAnimation == DinoGirlAnimation.Hit && _currentAnimation == DinoGirlAnimation.Running))
                        {
                            // For transitions between Run and Hit (or vice versa), slow down the animation slightly
                            if (transitionFactor < 0.5f)
                            {
                                // First half of transition - pause briefly
                                // Do nothing here, keeping the frame the same
                            }
                            else
                            {
                                // Second half - advance normally
                                _currentFrame = (_currentFrame + 1) % FRAMES_PER_ANIMATION;
                            }
                        }
                        else
                        {
                            // For other transitions, proceed normally
                            _currentFrame = (_currentFrame + 1) % FRAMES_PER_ANIMATION;
                        }
                    }
                    else
                    {
                        // Normal animation (no transition)
                        _currentFrame = (_currentFrame + 1) % FRAMES_PER_ANIMATION;
                    }
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
                // Track previous animation for transition effects
                _previousAnimation = _currentAnimation;
                _currentAnimation = animation;

                // Start transition timer
                _transitionTime = 0.0f;

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