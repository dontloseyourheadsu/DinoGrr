using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DinoGrr.Core.Render;

internal static class Line
{
    /// <summary>
    /// Draws a line between two points.
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch used for drawing.</param>
    /// <param name="start">The starting point of the line.</param>
    /// <param name="end">The ending point of the line.</param>
    /// <param name="color">The color of the line.</param>
    /// <param name="thickness">The thickness of the line.</param>
    public static void Draw(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
    {
        // Calculate the distance and angle between the points.
        var distance = Vector2.Distance(start, end);
        var angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

        // Draw the line using the pixel texture.
        spriteBatch.Draw(
            pixelTexture,
            start,
            null,
            color,
            angle,
            Vector2.Zero,
            new Vector2(distance, thickness),
            SpriteEffects.None,
            0);
    }

    /// <summary>
    /// Initializes the pixel texture required for drawing.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the texture.</param>
    public static void Initialize(GraphicsDevice graphicsDevice)
    {
        if (pixelTexture == null)
        {
            pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
        }
    }

    /// <summary>
    /// Releases the pixel texture when no longer needed.
    /// </summary>
    private static Texture2D pixelTexture;
}
