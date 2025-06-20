using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DinoGrr.Core.Rendering.Parallax;

/// <summary>
/// Manages a multi-layered parallax background system that creates an illusion of depth
/// by moving background layers at different speeds relative to the camera movement.
/// </summary>
public class ParallaxBackground
{
    private class Layer
    {
        public Texture2D Texture;
        public float ParallaxFactor;
        public Vector2 Position;
        public Color Tint;
        public float Scale;
        public float VerticalOffset;  // Added vertical offset property

        public Layer(Texture2D texture, float parallaxFactor, Color tint, float scale = 1.0f, float verticalOffset = 0f)
        {
            Texture = texture;
            ParallaxFactor = parallaxFactor;
            Tint = tint;
            Scale = scale;
            VerticalOffset = verticalOffset;
            Position = Vector2.Zero;
        }
    }

    private Layer[] _layers;
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;
    private Vector2 _lastCameraPosition;

    // Smoothed position for parallax effect
    private Vector2 _smoothedPosition = Vector2.Zero;

    // How quickly the smoothed position moves toward the target position (0-1)
    // Lower values create smoother, more dampened movement
    private float _smoothingFactor = 0.05f;

    /// <summary>
    /// Gets or sets how quickly the background moves toward the target position (0-1).
    /// Lower values create smoother, more dampened movement.
    /// </summary>
    public float SmoothingFactor
    {
        get => _smoothingFactor;
        set => _smoothingFactor = MathHelper.Clamp(value, 0.001f, 1f);
    }

    /// <summary>
    /// Creates a new parallax background system with specified layers.
    /// </summary>
    /// <param name="virtualWidth">Virtual width of the game world</param>
    /// <param name="virtualHeight">Virtual height of the game world</param>
    public ParallaxBackground(int virtualWidth, int virtualHeight)
    {
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
        _lastCameraPosition = Vector2.Zero;
        _smoothedPosition = Vector2.Zero;
    }

    /// <summary>
    /// Initializes the parallax layers with textures, each with a different parallax factor.
    /// Lower parallax factors (closer to 0) move slower, creating the illusion of being farther away.
    /// </summary>
    /// <param name="textures">Array of textures to use for layers, ordered from back to front</param>
    /// <param name="parallaxFactors">Array of parallax factors for each layer</param>
    /// <param name="tints">Array of tint colors for each layer</param>
    /// <param name="scales">Array of scale factors for each layer</param>
    /// <param name="verticalOffsets">Array of vertical offsets (positive values move up, negative move down)</param>
    public void Initialize(Texture2D[] textures, float[] parallaxFactors, Color[] tints = null, float[] scales = null, float[] verticalOffsets = null)
    {
        if (textures.Length != parallaxFactors.Length)
            throw new ArgumentException("Number of textures must match number of parallax factors");

        _layers = new Layer[textures.Length];

        for (int i = 0; i < textures.Length; i++)
        {
            Color tint = (tints != null && i < tints.Length) ? tints[i] : Color.White;
            float scale = (scales != null && i < scales.Length) ? scales[i] : 1.0f;
            float verticalOffset = (verticalOffsets != null && i < verticalOffsets.Length) ? verticalOffsets[i] : 0f;
            _layers[i] = new Layer(textures[i], parallaxFactors[i], tint, scale, verticalOffset);
        }
    }

    /// <summary>
    /// Updates the position of each parallax layer based on player position.
    /// </summary>
    /// <param name="playerPosition">Current player position</param>
    public void Update(Vector2 playerPosition)
    {
        // Only update horizontal position from the player - keep vertical position limited
        Vector2 constrainedPosition = new Vector2(
            playerPosition.X,
            // Limit the vertical position change to keep backgrounds more stable
            MathHelper.Clamp(playerPosition.Y, -100, 300)
        );

        // Smoothly move the position toward the target
        // This eliminates the bouncy, physics-based movement from being visible in the background
        _smoothedPosition = Vector2.Lerp(_smoothedPosition, constrainedPosition, _smoothingFactor);

        // Use the smoothed position for all parallax calculations
        // Update each layer position based on its parallax factor
        foreach (var layer in _layers)
        {
            // Calculate layer position based on smoothed position
            // Lower parallax factor (closer to 0) will result in a slower moving background,
            // creating the illusion that it is farther away
            layer.Position.X = -_smoothedPosition.X * layer.ParallaxFactor;

            // For vertical parallax, use an extremely small factor for minimal movement
            // We want horizontal parallax but barely any vertical movement
            float verticalFactor = layer.ParallaxFactor * 0.05f; // Very small vertical factor
            layer.Position.Y = -_smoothedPosition.Y * verticalFactor;
        }

        // Store last position for Reset functionality
        _lastCameraPosition = constrainedPosition;
    }    /// <summary>
         /// Draws all parallax layers with horizontal repetition.
         /// </summary>
         /// <param name="spriteBatch">SpriteBatch to use for drawing</param>
         /// <param name="camera">Camera instance for calculating visible area</param>
    public void Draw(SpriteBatch spriteBatch, Camera2D camera)
    {
        // End the current sprite batch if one is active
        spriteBatch.End();

        // Start a new sprite batch without camera transformation for background
        spriteBatch.Begin(samplerState: SamplerState.LinearWrap);

        // Get screen dimensions
        Viewport viewport = spriteBatch.GraphicsDevice.Viewport;
        float screenWidth = viewport.Width;
        float screenHeight = viewport.Height;

        // Define screen center
        Vector2 screenCenter = new Vector2(screenWidth * 0.5f, screenHeight * 0.5f);

        foreach (var layer in _layers)
        {
            // Apply camera zoom to the texture scale and make it smaller overall
            float sizeReductionFactor = 0.8f; // Reduce the size to 80% of original
            float zoomedScale = layer.Scale * camera.Zoom * sizeReductionFactor;

            // Calculate the position offset from the parallax effect
            float parallaxOffsetX = layer.Position.X;
            float parallaxOffsetY = layer.Position.Y + layer.VerticalOffset;

            // Calculate the destination rectangle centered on screen
            float texWidth = layer.Texture.Width;
            float texHeight = layer.Texture.Height;

            // The background should be centered vertically in the screen
            // 0.4f positions it slightly above the center (lower value = higher position)
            float verticalScreenPosition = screenHeight * 0.4f;

            // Calculate the destination rectangle, keeping the image centered
            Rectangle destRect = new Rectangle(
                (int)(screenCenter.X - (texWidth * zoomedScale / 2)),  // Center horizontally
                (int)(verticalScreenPosition - (texHeight * zoomedScale / 2)),  // Position vertically
                (int)(texWidth * zoomedScale),
                (int)(texHeight * zoomedScale)
            );

            // Calculate source rectangle that handles the parallax scrolling
            // Using horizontal wrapping by offsetting the source rectangle
            float textureOffsetX = parallaxOffsetX / zoomedScale;
            float textureOffsetY = parallaxOffsetY / zoomedScale;

            // Create source rectangle that handles tiling
            // This is the key for smooth, seamless tiling with the LinearWrap sampler state
            Rectangle sourceRect = new Rectangle(
                (int)textureOffsetX,
                (int)textureOffsetY,
                (int)(screenWidth / zoomedScale),
                (int)(texHeight)
            );

            // Draw the entire background as one piece, letting LinearWrap handle the tiling
            spriteBatch.Draw(
                layer.Texture,
                destRect,
                sourceRect,
                layer.Tint
            );
        }

        // Restore the original sprite batch with camera transform
        spriteBatch.End();
        spriteBatch.Begin(transformMatrix: camera.GetMatrix(), samplerState: SamplerState.PointClamp);
    }

    /// <summary>
    /// Resets the parallax system to a new player position without transitioning.
    /// Useful when teleporting the player or initializing.
    /// </summary>
    /// <param name="playerPosition">New player position</param>
    public void Reset(Vector2 playerPosition)
    {
        // Apply same vertical constraints as in Update method
        Vector2 constrainedPosition = new Vector2(
            playerPosition.X,
            MathHelper.Clamp(playerPosition.Y, -100, 300)
        );

        // Immediately set smoothed position to match player position (no smoothing on reset)
        _smoothedPosition = constrainedPosition;
        _lastCameraPosition = constrainedPosition;

        // Initialize layer positions based on player position
        if (_layers != null)
        {
            foreach (var layer in _layers)
            {
                layer.Position.X = -constrainedPosition.X * layer.ParallaxFactor;

                // For vertical parallax, use a smaller factor (same as in Update)
                float verticalFactor = layer.ParallaxFactor * 0.05f;
                layer.Position.Y = -constrainedPosition.Y * verticalFactor;
            }
        }
    }
}