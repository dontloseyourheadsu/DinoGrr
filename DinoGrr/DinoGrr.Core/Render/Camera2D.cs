using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Drawing;

namespace DinoGrr.Core.Render
{
    /// <summary>
    /// Represents a 2D camera system that maintains a virtual resolution and allows for:
    /// - Automatic base zoom to ensure the virtual world always fits in the window.
    /// - Additional user zoom (via mouse wheel) layered on top of the base zoom.
    /// - View manipulation such as panning and zooming with optional built-in controls.
    /// - Following a specific point with smooth interpolation.
    /// </summary>
    public sealed class Camera2D
    {
        // The virtual resolution of the game world.
        private readonly int _virtualW;
        private readonly int _virtualH;

        // The viewport the camera is rendering into.
        private Viewport _vp;

        // Zoom factor that keeps the virtual world visible within the viewport.
        private float _baseZoom = 1f;

        // Zoom factor applied by the user (e.g. mouse wheel).
        private float _userZoom = 1f;

        // The scroll wheel value from the last frame (used to calculate delta).
        private int _lastWheel;

        // The point being followed by the camera (if any).
        private Physics.VerletPoint _followTarget;

        // How quickly the camera moves toward its target (0-1, where 1 is instant).
        private float _followSmoothing = 0.1f;

        /// <summary>
        /// Gets the total zoom applied (base zoom × user zoom).
        /// </summary>
        public float Zoom => _baseZoom * _userZoom;

        /// <summary>
        /// Gets the position of the camera in world space.
        /// </summary>
        public Vector2 Position { get; private set; }

        /// <summary>
        /// Gets the rotation of the camera in radians.
        /// </summary>
        public float Rotation { get; private set; }

        /// <summary>
        /// Gets the point the camera is currently following (if any).
        /// </summary>
        public Physics.VerletPoint FollowTarget => _followTarget;

        /// <summary>
        /// Gets or sets how quickly the camera moves toward its target (0-1, where 1 is instant).
        /// </summary>
        public float FollowSmoothing
        {
            get => _followSmoothing;
            set => _followSmoothing = MathHelper.Clamp(value, 0.001f, 1f);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Camera2D"/> class.
        /// </summary>
        /// <param name="vp">The viewport to render into.</param>
        /// <param name="virtualWidth">The virtual width of the world.</param>
        /// <param name="virtualHeight">The virtual height of the world.</param>
        public Camera2D(Viewport vp, int virtualWidth, int virtualHeight)
        {
            _vp = vp;
            _virtualW = virtualWidth;
            _virtualH = virtualHeight;
            RecalculateBaseZoom();
        }

        /// <summary>
        /// Sets a new viewport and recalculates the base zoom.
        /// Call this when the window or resolution changes.
        /// </summary>
        /// <param name="vp">The new viewport.</param>
        public void SetViewport(Viewport vp)
        {
            _vp = vp;
            RecalculateBaseZoom();
        }

        /// <summary>
        /// Recalculates the base zoom to ensure the virtual world fits the viewport.
        /// Maintains aspect ratio by choosing the smaller scale factor.
        /// </summary>
        private void RecalculateBaseZoom()
        {
            _baseZoom = MathF.Min(_vp.Width / (float)_virtualW, _vp.Height / (float)_virtualH);
        }

        /// <summary>
        /// Moves the camera's position to look at a specific world position.
        /// </summary>
        /// <param name="worldPos">The target world position to center the camera on.</param>
        public void LookAt(Vector2 worldPos)
        {
            // Stop following any target when manually setting position
            _followTarget = null;
            Position = worldPos;
        }

        /// <summary>
        /// Sets the camera to follow a specific Verlet point.
        /// Pass null to stop following.
        /// </summary>
        /// <param name="point">The point to follow, or null to stop following.</param>
        public void Follow(Physics.VerletPoint point)
        {
            _followTarget = point;
            if (point != null)
            {
                // Immediately center on the point
                Position = point.Position;
            }
        }

        /// <summary>
        /// Moves the camera's position by a world-space offset.
        /// </summary>
        /// <param name="deltaWorld">The amount to move the camera by, in world units.</param>
        public void Move(Vector2 deltaWorld)
        {
            // Stop following any target when manually moving
            _followTarget = null;
            Position += deltaWorld;
        }

        /// <summary>
        /// Adjusts the user zoom level by a delta.
        /// </summary>
        /// <param name="delta">Positive to zoom in, negative to zoom out.</param>
        public void ZoomBy(float delta)
        {
            // Clamp to avoid extreme zoom levels
            _userZoom = MathHelper.Clamp(_userZoom + delta, 0.1f, 10f);
        }

        /// <summary>
        /// Constructs the transformation matrix used to convert world coordinates to screen coordinates.
        /// </summary>
        /// <returns>The transformation matrix for rendering.</returns>
        public Matrix GetMatrix()
        {
            // Origin is the center of the screen
            var origin = new Vector2(_vp.Width * 0.5f, _vp.Height * 0.5f);

            // Compose the transform matrix in order: translate -> rotate -> scale -> translate to screen center
            return Matrix.CreateTranslation(new Vector3(-Position, 0)) *
                   Matrix.CreateRotationZ(Rotation) *
                   Matrix.CreateScale(Zoom, Zoom, 1) *
                   Matrix.CreateTranslation(new Vector3(origin, 0));
        }

        /// <summary>
        /// Converts screen coordinates to world coordinates.
        /// Useful for mouse interaction.
        /// </summary>
        /// <param name="screen">Screen position in pixels.</param>
        /// <returns>World position.</returns>
        public Vector2 ScreenToWorld(Vector2 screen) =>
            Vector2.Transform(screen, Matrix.Invert(GetMatrix()));

        /// <summary>
        /// Updates the camera's position to follow the target point if one is set.
        /// </summary>
        /// <param name="gt">GameTime for delta calculations.</param>
        public void Update(GameTime gt)
        {
            if (_followTarget != null)
            {
                // Smoothly interpolate toward the target position
                Vector2 targetPos = _followTarget.Position;
                Position = Vector2.Lerp(Position, targetPos, _followSmoothing);
            }
        }

        /// <summary>
        /// Handles input for camera panning and zooming.
        /// WASD keys pan the camera, and the mouse wheel zooms in/out around the cursor.
        /// </summary>
        /// <param name="gt">GameTime instance for frame timing.</param>
        public void HandleInput(GameTime gt)
        {
            KeyboardState kb = Keyboard.GetState();
            MouseState ms = Mouse.GetState();

            // Handle panning with WASD keys
            Vector2 pan = Vector2.Zero;
            if (kb.IsKeyDown(Keys.A)) pan.X -= 400;
            if (kb.IsKeyDown(Keys.D)) pan.X += 400;
            if (kb.IsKeyDown(Keys.W)) pan.Y -= 400;
            if (kb.IsKeyDown(Keys.S)) pan.Y += 400;

            if (pan != Vector2.Zero)
            {
                // Stop following when manually panning
                _followTarget = null;

                // Adjust movement speed by elapsed time and inverse zoom (so speed feels consistent regardless of zoom)
                Move(pan * (float)gt.ElapsedGameTime.TotalSeconds / Zoom);
            }

            // Handle mouse wheel zooming
            int wheelDelta = ms.ScrollWheelValue - _lastWheel;
            _lastWheel = ms.ScrollWheelValue;

            if (wheelDelta != 0)
            {
                // Get the world position under the mouse cursor before zooming
                Vector2 before = ScreenToWorld(ms.Position.ToVector2());

                // Apply zoom delta
                ZoomBy(wheelDelta > 0 ? 0.1f : -0.1f);

                // Get the world position under the mouse cursor after zooming
                Vector2 after = ScreenToWorld(ms.Position.ToVector2());

                // Move the camera to keep the cursor over the same world position
                Position += (before - after);
            }

            // Update the camera to follow target if set
            Update(gt);
        }

        /// <summary>
        /// Gets the bounding box of the currently visible world area.
        /// </summary>
        /// <returns>A RectangleF representing the world-space bounds visible on screen.</returns>
        public RectangleF GetVisibleWorldBounds()
        {
            // Convert top-left and bottom-right screen corners to world coordinates
            Vector2 tl = ScreenToWorld(Vector2.Zero);
            Vector2 br = ScreenToWorld(new Vector2(_vp.Width, _vp.Height));

            float left = MathF.Min(tl.X, br.X);
            float right = MathF.Max(tl.X, br.X);
            float top = MathF.Min(tl.Y, br.Y);
            float bottom = MathF.Max(tl.Y, br.Y);

            // Construct a rectangle from those bounds
            return new RectangleF(left, top, right - left, bottom - top);
        }
    }
}