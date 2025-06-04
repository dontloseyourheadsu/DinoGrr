using System;
using System.Collections.Generic;
using DinoGrr.Core.Entities;
using DinoGrr.Core.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DinoGrr.Core.Rendering;

/// <summary>
/// Renders a texture that deforms with a SoftBody.
/// </summary>
public class TexturedSoftBodyMesh
{
    // The texture to render
    private readonly Texture2D _texture;

    // The soft body to follow
    private readonly SoftBody _softBody;

    // Vertices for the mesh
    private VertexPositionTexture[] _vertices;

    // Indices for the triangle list
    private int[] _indices;

    // Effect for rendering
    private BasicEffect _effect;

    /// <summary>
    /// Creates a new TexturedSoftBodyMesh that will render a texture deformed according to a SoftBody.
    /// </summary>
    /// <param name="graphicsDevice">The GraphicsDevice to use for rendering.</param>
    /// <param name="texture">The texture to render.</param>
    /// <param name="softBody">The SoftBody that will deform the texture.</param>
    public TexturedSoftBodyMesh(GraphicsDevice graphicsDevice, Texture2D texture, SoftBody softBody)
    {
        _texture = texture ?? throw new ArgumentNullException(nameof(texture));
        _softBody = softBody ?? throw new ArgumentNullException(nameof(softBody));

        // Create the effect for rendering
        _effect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = true,
            Texture = texture,
            VertexColorEnabled = false
        };

        // Generate the mesh based on the soft body's points
        CreateMeshFromSoftBody();
    }

    /// <summary>
    /// Sets up the vertices and indices for the mesh based on the soft body's points.
    /// This assumes the SoftBody is a rectangle, similar to what RectangleSoftBodyBuilder creates.
    /// </summary>
    private void CreateMeshFromSoftBody()
    {
        // Get the points from the soft body
        var points = _softBody.Points;

        // For a rectangle soft body, we expect at least 4 points
        if (points.Count < 4)
        {
            throw new InvalidOperationException("SoftBody must have at least 4 points to create a textured mesh.");
        }

        // Create vertices - for now assuming a simple rectangular mesh
        // We'll map texture coordinates (0,0) to (1,1) across the mesh
        _vertices = new VertexPositionTexture[points.Count];

        // If the soft body is a rectangle with points ordered as in RectangleSoftBodyBuilder:
        // 0: top-left, 1: top-right, 2: bottom-right, 3: bottom-left
        _vertices[0] = new VertexPositionTexture(new Vector3(points[0].Position, 0), new Vector2(0, 0));
        _vertices[1] = new VertexPositionTexture(new Vector3(points[1].Position, 0), new Vector2(1, 0));
        _vertices[2] = new VertexPositionTexture(new Vector3(points[2].Position, 0), new Vector2(1, 1));
        _vertices[3] = new VertexPositionTexture(new Vector3(points[3].Position, 0), new Vector2(0, 1));

        // Add additional vertices if the soft body has more points
        // This would need custom texture coordinate mapping based on their positions
        for (int i = 4; i < points.Count; i++)
        {
            // For additional points, you'll need to assign appropriate texture coordinates
            // This is just a placeholder - you'll need to map these based on your specific needs
            _vertices[i] = new VertexPositionTexture(
                new Vector3(points[i].Position, 0),
                new Vector2(points[i].Position.X / 100f, points[i].Position.Y / 100f)
            );
        }

        // Create indices for triangle list
        if (points.Count == 4)
        {
            // For a rectangle with 4 points, we create 2 triangles
            _indices = new int[6]
            {
                0, 1, 2, // First triangle: top-left, top-right, bottom-right
                0, 2, 3  // Second triangle: top-left, bottom-right, bottom-left
            };
        }
        else
        {
            // For more complex shapes, you'd need a triangulation algorithm
            // This is just a placeholder assuming a convex polygon
            _indices = new int[(points.Count - 2) * 3];

            for (int i = 0; i < points.Count - 2; i++)
            {
                _indices[i * 3] = 0;
                _indices[i * 3 + 1] = i + 1;
                _indices[i * 3 + 2] = i + 2;
            }
        }
    }

    /// <summary>
    /// Updates the vertex positions to match the current state of the soft body.
    /// </summary>
    public void Update()
    {
        var points = _softBody.Points;

        // Update vertex positions from the soft body's current point positions
        for (int i = 0; i < points.Count && i < _vertices.Length; i++)
        {
            _vertices[i].Position = new Vector3(points[i].Position, 0);
        }
    }

    /// <summary>
    /// Renders the textured mesh using the current camera view.
    /// </summary>
    /// <param name="camera">The camera to use for rendering.</param>
    public void Draw(Camera2D camera)
    {
        // Update the effect's view and projection
        _effect.View = Matrix.CreateTranslation(new Vector3(0, 0, 0));
        _effect.Projection = Matrix.CreateOrthographicOffCenter(
            0, _effect.GraphicsDevice.Viewport.Width,
            _effect.GraphicsDevice.Viewport.Height, 0,
            0, 1
        );

        // Apply the camera's transformation
        _effect.World = camera.GetMatrix();

        // Apply the effect
        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();

            // Draw the mesh
            _effect.GraphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _vertices,
                0,
                _vertices.Length,
                _indices,
                0,
                _indices.Length / 3
            );
        }
    }
}

/// <summary>
/// Handles rendering a dinosaur entity with its texture.
/// </summary>
public class DinosaurRenderer
{
    private readonly TexturedSoftBodyMesh _texturedMesh;
    private readonly NormalDinosaur _dinosaur;

    /// <summary>
    /// Creates a new DinosaurRenderer.
    /// </summary>
    /// <param name="graphicsDevice">The GraphicsDevice to use for rendering.</param>
    /// <param name="dinosaurTexture">The texture to render for the dinosaur.</param>
    /// <param name="dinosaur">The dinosaur entity to render.</param>
    public DinosaurRenderer(GraphicsDevice graphicsDevice, Texture2D dinosaurTexture, NormalDinosaur dinosaur)
    {
        _dinosaur = dinosaur ?? throw new ArgumentNullException(nameof(dinosaur));
        _texturedMesh = new TexturedSoftBodyMesh(graphicsDevice, dinosaurTexture, dinosaur.Body);
    }

    /// <summary>
    /// Updates the renderer to match the current state of the dinosaur.
    /// </summary>
    public void Update()
    {
        _texturedMesh.Update();
    }

    /// <summary>
    /// Draws the dinosaur.
    /// </summary>
    /// <param name="camera">The camera to use for rendering.</param>
    public void Draw(Camera2D camera)
    {
        _texturedMesh.Draw(camera);
    }
}