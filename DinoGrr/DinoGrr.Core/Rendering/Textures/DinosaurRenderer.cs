using System;
using DinoGrr.Core.Entities.Dinosaurs;
using Microsoft.Xna.Framework.Graphics;

namespace DinoGrr.Core.Rendering.Textures;

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