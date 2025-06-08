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
    /// Updates the position of each parallax layer based on camera movement.
    /// </summary>
    /// <param name="cameraPosition">Current camera position</param>
    public void Update(Vector2 cameraPosition)
    {
        // Calculate camera movement since last frame
        Vector2 cameraMovement = cameraPosition - _lastCameraPosition; // REVERSED! Now getting movement in correct direction
        _lastCameraPosition = cameraPosition;

        // Update each layer position based on its parallax factor
        foreach (var layer in _layers)
        {
            // Move in the opposite direction of the camera movement to create parallax effect
            // Multiply by parallax factor to control speed
            layer.Position += cameraMovement * layer.ParallaxFactor;
        }
    }

    /// <summary>
    /// Draws all parallax layers with horizontal repetition.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch to use for drawing</param>
    /// <param name="camera">Camera instance for calculating visible area</param>
    public void Draw(SpriteBatch spriteBatch, Camera2D camera)
    {
        // Get the visible area of the world
        var visibleBounds = camera.GetVisibleWorldBounds();

        foreach (var layer in _layers)
        {
            // Calculate how much of the texture needs to be tiled horizontally
            float effectiveWidth = layer.Texture.Width * layer.Scale;

            // Calculate the horizontal offset for repeating
            float offsetX = layer.Position.X % effectiveWidth;
            if (offsetX > 0) offsetX -= effectiveWidth;

            // Calculate how many copies of the texture we need to cover the visible area
            float startX = visibleBounds.Left + offsetX;
            int copies = (int)Math.Ceiling(visibleBounds.Width / effectiveWidth) + 2; // +2 to ensure coverage during scrolling

            // Draw the texture repeatedly to cover the visible area horizontally
            for (int i = 0; i < copies; i++)
            {
                float xPos = startX + (i * effectiveWidth);

                // Determine vertical position with the vertical offset
                // Using visibleBounds.Bottom - textureHeight as the baseline position (bottom of screen)
                // Then applying the vertical offset (positive values move the layer up)
                float yPos = visibleBounds.Bottom - (layer.Texture.Height * layer.Scale) + layer.VerticalOffset;

                spriteBatch.Draw(
                    layer.Texture,
                    new Vector2(xPos, yPos),
                    null,
                    layer.Tint,
                    0f,
                    Vector2.Zero,
                    layer.Scale,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }

    /// <summary>
    /// Resets the parallax system to follow a new camera position without transitioning.
    /// Useful when teleporting the camera or initializing.
    /// </summary>
    /// <param name="cameraPosition">New camera position</param>
    public void Reset(Vector2 cameraPosition)
    {
        _lastCameraPosition = cameraPosition;
    }
}