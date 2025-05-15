using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DinoGrr.Core.Render;

internal static class Circle
{
    /// <summary>
    /// Flag to show collision borders
    /// </summary>
    public static bool ShowCollisionBorders { get; set; } = false;

    /// <summary>
    /// Debug color for collision border
    /// </summary>
    public static Color DebugBorderColor { get; set; } = Color.Red;

    /// <summary>
    /// Draws a solid circle.
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch used for drawing.</param>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="color">The color of the circle.</param>
    /// <param name="segments">The number of segments to use (more segments = smoother).</param>
    public static void Draw(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int segments = 32)
    {
        if (circleTexture == null)
            throw new InvalidOperationException("Circle texture not initialized. Call Initialize first.");

        // Ensure radius is at least 1 to avoid rendering issues
        radius = Math.Max(1.0f, radius);

        // IMPORTANT: Use the exact same size as physics calculations
        // Avoid casting to int until the last moment to prevent rounding errors
        float diameter = radius * 2;

        // Draw the pre-rendered circle texture with pixel precision
        spriteBatch.Draw(
            circleTexture,
            center,
            null,
            color,
            0f,
            new Vector2(circleTexture.Width / 2, circleTexture.Height / 2),
            new Vector2(diameter / circleTexture.Width, diameter / circleTexture.Height),
            SpriteEffects.None,
            0);

        // Draw collision border if enabled
        if (ShowCollisionBorders)
        {
            DrawDebugBorder(spriteBatch, center, radius, DebugBorderColor);
        }
    }

    /// <summary>
    /// Draws a debug border around the circle to verify physical boundaries.
    /// </summary>
    private static void DrawDebugBorder(SpriteBatch spriteBatch, Vector2 center, float radius, Color borderColor)
    {
        // Draw an accurate collision border using a circle
        const int segments = 32;
        const float thickness = 1.0f;

        // Draw a circle made of lines to show the exact collision limit
        Vector2[] points = new Vector2[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float angle = ((float)i / segments) * MathHelper.TwoPi;
            points[i] = new Vector2(
                center.X + (float)Math.Cos(angle) * radius,
                center.Y + (float)Math.Sin(angle) * radius
            );

            // Draw the current point connected to the previous one
            if (i > 0)
            {
                DrawLine(spriteBatch, points[i - 1], points[i], borderColor, thickness);
            }
        }
    }

    /// <summary>
    /// Draws a line between two points.
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch used for drawing.</param>
    /// <param name="start">The start point of the line.</param>
    /// <param name="end">The end point of the line.</param>
    /// <param name="color">The color of the line.</param>
    /// <param name="thickness">The thickness of the line.</param>
    private static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness = 1.0f)
    {
        if (pixel == null)
        {
            // Create a 1x1 white pixel texture if not already created
            pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        // Calculate length and angle of the line
        Vector2 delta = end - start;
        float length = delta.Length();
        float angle = (float)Math.Atan2(delta.Y, delta.X);

        // Draw the line as a rotated rectangle
        spriteBatch.Draw(
            pixel,
            start,
            null,
            color,
            angle,
            Vector2.Zero,
            new Vector2(length, thickness),
            SpriteEffects.None,
            0);
    }

    /// <summary>
    /// Initializes the circle texture needed for drawing.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to create the texture.</param>
    /// <param name="size">The size of the circle texture (larger = more detailed).</param>
    public static void Initialize(GraphicsDevice graphicsDevice, int size = 256)
    {
        // Create a square texture for the circle
        circleTexture = new Texture2D(graphicsDevice, size, size);

        // Color data for each pixel of the texture
        Color[] colorData = new Color[size * size];

        // Radius in pixels of the texture (center of the texture)
        float radius = size / 2f;
        float radiusSquared = radius * radius;

        // Compute color for each pixel
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Distance squared from center
                float distanceSquared = (x - radius) * (x - radius) + (y - radius) * (y - radius);

                // If the pixel is within the circle
                if (distanceSquared <= radiusSquared)
                {
                    // Edge smoothing (anti-aliasing)
                    float distance = (float)Math.Sqrt(distanceSquared);
                    float alpha = 1.0f;

                    // Apply smoothing on the edges (max 2 pixels of smoothing)
                    float edgeWidth = Math.Min(2.0f, radius * 0.1f); // 10% of radius or 2 pixels, whichever is smaller
                    if (distance > radius - edgeWidth)
                    {
                        alpha = 1.0f - (distance - (radius - edgeWidth)) / edgeWidth;
                    }

                    colorData[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    // Outside the circle is transparent
                    colorData[y * size + x] = Color.Transparent;
                }
            }
        }

        // Update the texture with the computed data
        circleTexture.SetData(colorData);
    }

    /// <summary>
    /// Pre-rendered circle texture
    /// </summary>
    private static Texture2D circleTexture;

    /// <summary>
    /// 1x1 pixel texture used for drawing lines
    /// </summary>
    private static Texture2D pixel;
}
