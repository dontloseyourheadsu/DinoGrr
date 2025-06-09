using System;
using DinoGrr.Core.Entities.Player;
using Microsoft.Xna.Framework.Graphics;

namespace DinoGrr.Core.Rendering.Textures;

/// <summary>
/// Represents the renderer for the DinoGirl character in the game.
/// </summary>
public class DinoGirlRenderer
{
    private readonly TexturedSoftBodyMesh _texturedMesh;
    private readonly DinoGirl _dinoGirl;

    /// <summary>
    /// Initializes a new instance of the <see cref="DinoGirlRenderer"/> class.
    /// </summary>
    /// <param name="dinoGirl">The DinoGirl entity to render.</param>
    public DinoGirlRenderer(GraphicsDevice graphicsDevice, Texture2D texture, DinoGirl dinoGirl)
    {
        _dinoGirl = dinoGirl ?? throw new ArgumentNullException(nameof(dinoGirl));
        _texturedMesh = new TexturedSoftBodyMesh(graphicsDevice, texture, _dinoGirl.Body);
    }

    /// <summary>
    /// Updates the renderer to match the current state of the DinoGirl.
    /// </summary>
    public void Update()
    {
        _texturedMesh.Update();
    }
    
    /// <summary>
    /// Draws the DinoGirl character using the current camera.
    /// </summary>
    public void Draw(Camera2D camera)
    {
        _texturedMesh.Draw(camera);
    }
}
