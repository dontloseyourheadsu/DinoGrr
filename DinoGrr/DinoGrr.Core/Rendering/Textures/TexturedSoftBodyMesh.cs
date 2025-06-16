using System;
using System.Collections.Generic;
using DinoGrr.Core.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DinoGrr.Core.Rendering.Textures;

/// <summary>
/// Represents a textured mesh for a soft body that can be animated using sprite sheets.
/// </summary>
public class TexturedSoftBodyMesh
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Texture2D _texture;
    private readonly SoftBody _softBody;
    private Rectangle _sourceRectangle;
    private SpriteBatch _spriteBatch;

    // Default to using the entire texture
    private bool _useSourceRectangle = false;

    // Sprite direction properties
    private bool _flipHorizontally = false;

    /// <summary>
    /// Initializes a new instance of the TexturedSoftBodyMesh class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <param name="texture">The texture to use.</param>
    /// <param name="softBody">The soft body to texture.</param>
    public TexturedSoftBodyMesh(GraphicsDevice graphicsDevice, Texture2D texture, SoftBody softBody)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _texture = texture ?? throw new ArgumentNullException(nameof(texture));
        _softBody = softBody ?? throw new ArgumentNullException(nameof(softBody));
        _sourceRectangle = new Rectangle(0, 0, _texture.Width, _texture.Height);
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _useSourceRectangle = false;
    }

    /// <summary>
    /// Sets the source rectangle to use for texture mapping.
    /// </summary>
    /// <param name="sourceRectangle">Rectangle defining the area of the texture to use.</param>
    public void SetSourceRectangle(Rectangle sourceRectangle)
    {
        _sourceRectangle = sourceRectangle;
        _useSourceRectangle = true;
    }

    /// <summary>
    /// Updates the mesh to match the current state of the soft body.
    /// </summary>
    public void Update()
    {
        // This method can be expanded if any additional updates are needed
        // for the mesh based on the soft body's state
    }

    /// <summary>
    /// Sets whether the sprite should be flipped horizontally.
    /// </summary>
    /// <param name="flip">True to flip horizontally, false otherwise.</param>
    public void SetFlip(bool flip)
    {
        _flipHorizontally = flip;
    }

    /// <summary>
    /// Draws the textured mesh using the specified camera.
    /// </summary>
    /// <param name="camera">The camera to use for rendering.</param>
    public void Draw(Camera2D camera)
    {
        // Calculate the center position from the soft body's points
        Vector2 center = CalculateCenter();

        // Calculate dimensions for drawing
        float width = CalculateWidth();
        float height = CalculateHeight();

        // Calculate rotation from the soft body's orientation
        float rotation = CalculateRotation();

        // Begin sprite batch with camera transformation
        _spriteBatch.Begin(transformMatrix: camera.GetMatrix(),
            samplerState: SamplerState.PointClamp);

        // Draw the sprite using the current source rectangle
        _spriteBatch.Draw(
            _texture,                              // The texture
            center,                                // Position to draw at
            _useSourceRectangle ? _sourceRectangle : null, // Source rectangle (null = entire texture)
            Color.White,                           // Color tint
            rotation,                              // Rotation
            new Vector2(                           // Origin (center of source rect)
                _sourceRectangle.Width / 2f,
                _sourceRectangle.Height / 2f),
            new Vector2(                           // Scale to match soft body size
                width / _sourceRectangle.Width,
                height / _sourceRectangle.Height),
            _flipHorizontally ? SpriteEffects.FlipHorizontally : SpriteEffects.None, // Flip sprite if needed
            0f);                                   // Layer depth

        _spriteBatch.End();
    }

    /// <summary>
    /// Calculates the center position of the soft body.
    /// </summary>
    private Vector2 CalculateCenter()
    {
        Vector2 sum = Vector2.Zero;
        foreach (var point in _softBody.Points)
        {
            sum += point.Position;
        }
        return sum / _softBody.Points.Count;
    }

    /// <summary>
    /// Calculates the approximate width of the soft body.
    /// </summary>
    private float CalculateWidth()
    {
        // For a rectangular soft body, we assume points are in order and can measure width
        // This is a simple implementation - you might need to adjust based on your soft body structure
        if (_softBody.Points.Count >= 4)
        {
            return Vector2.Distance(_softBody.Points[0].Position, _softBody.Points[1].Position);
        }
        return 100f; // Default width if we can't determine
    }

    /// <summary>
    /// Calculates the approximate height of the soft body.
    /// </summary>
    private float CalculateHeight()
    {
        // For a rectangular soft body, we assume points are in order and can measure height
        // This is a simple implementation - you might need to adjust based on your soft body structure
        if (_softBody.Points.Count >= 4)
        {
            return Vector2.Distance(_softBody.Points[0].Position, _softBody.Points[3].Position);
        }
        return 180f; // Default height if we can't determine
    }

    /// <summary>
    /// Calculates the rotation of the soft body.
    /// </summary>
    private float CalculateRotation()
    {
        // For a rectangular soft body, calculate rotation based on top edge
        if (_softBody.Points.Count >= 4)
        {
            Vector2 edge = _softBody.Points[1].Position - _softBody.Points[0].Position;
            return (float)Math.Atan2(edge.Y, edge.X);
        }
        return 0f; // Default rotation if we can't determine
    }
}