using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DinoGrr.Core.Physics;
using DinoGrr.Core.Entities.Player;
using DinoGrr.Core.Rendering.Textures;
using DinoGrr.Core.Rendering;

namespace DinoGrr.Core.Entities.Dinosaurs;

/// <summary>
/// Manages all dinosaurs in the game world, including their creation, update, and rendering.
/// </summary>
public class DinosaurManager
{
    private readonly List<DinosaurInstance> _dinosaurs;
    private readonly VerletSystem _verletSystem;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<DinosaurSpecies, Texture2D> _textures;
    private readonly DinoGirl _dinoGirl;
    private readonly Random _random;

    /// <summary>
    /// Gets the collection of all dinosaur instances.
    /// </summary>
    public IReadOnlyList<DinosaurInstance> Dinosaurs => _dinosaurs;

    /// <summary>
    /// Initializes a new DinosaurManager.
    /// </summary>
    public DinosaurManager(VerletSystem verletSystem, GraphicsDevice graphicsDevice, DinoGirl dinoGirl)
    {
        _dinosaurs = new List<DinosaurInstance>();
        _verletSystem = verletSystem ?? throw new ArgumentNullException(nameof(verletSystem));
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _dinoGirl = dinoGirl ?? throw new ArgumentNullException(nameof(dinoGirl));
        _textures = new Dictionary<DinosaurSpecies, Texture2D>();
        _random = new Random();
    }

    /// <summary>
    /// Loads textures for all dinosaur species.
    /// </summary>
    public void LoadTextures(Microsoft.Xna.Framework.Content.ContentManager content)
    {
        foreach (DinosaurSpecies species in Enum.GetValues<DinosaurSpecies>())
        {
            string texturePath = DinosaurSpeciesData.GetTexturePath(species);
            _textures[species] = content.Load<Texture2D>(texturePath);
        }
    }

    /// <summary>
    /// Creates a dinosaur of the specified species at the given position.
    /// </summary>
    public DinosaurInstance CreateDinosaur(DinosaurSpecies species, Vector2 position, string name = null)
    {
        var behavior = DinosaurSpeciesData.GetBehavior(species);
        var (width, height) = DinosaurSpeciesData.GetSize(species);
        var texture = _textures[species];

        name ??= $"{species}_{_dinosaurs.Count}";

        // Create the dinosaur entity
        var dinosaur = new NormalDinosaur(
            _verletSystem,
            position,
            width,
            height,
            name: name,
            jumpForce: behavior.JumpForce,
            stiffness: 0.005f,
            maxSpeed: behavior.Speed * 2f // Convert to max speed
        );

        // Create the renderer
        var renderer = new DinosaurRenderer(_graphicsDevice, texture, dinosaur);

        // Create the AI controller based on behavior type
        IDinosaurAI ai = behavior.Type switch
        {
            BehaviorType.Random => new RandomDinoMover(dinosaur),
            BehaviorType.Aggressive => new AggressiveDinoAI(dinosaur, _dinoGirl, behavior),
            BehaviorType.Defensive => new DefensiveDinoAI(dinosaur, _dinoGirl, behavior),
            BehaviorType.Passive => new PassiveDinoAI(dinosaur, behavior),
            BehaviorType.Territorial => new TerritorialDinoAI(dinosaur, _dinoGirl, position, behavior),
            _ => new RandomDinoMover(dinosaur)
        };

        var instance = new DinosaurInstance(dinosaur, renderer, ai, species, behavior);
        _dinosaurs.Add(instance);

        return instance;
    }

    /// <summary>
    /// Creates multiple dinosaurs distributed across the world.
    /// </summary>
    public void PopulateWorld(int worldWidth, int worldHeight)
    {
        // Define dinosaur distribution - reduced numbers for better spacing
        var distribution = new Dictionary<DinosaurSpecies, int>
        {
            { DinosaurSpecies.TRex, 1 },           // Apex predator - only one
            { DinosaurSpecies.Allosaurus, 2 },     // Large predators
            { DinosaurSpecies.Velociraptor, 3 },   // Fast pack hunters
            { DinosaurSpecies.Triceratops, 2 },    // Heavy herbivores
            { DinosaurSpecies.Brontosaurus, 1 },   // Massive herbivore - only one
            { DinosaurSpecies.Parasaurolophus, 2 }, // Medium herbivores
            { DinosaurSpecies.Anchylosaurus, 2 },  // Armored herbivores
            { DinosaurSpecies.Polacanthus, 1 },    // Spiky herbivore
            { DinosaurSpecies.Dimetrodon, 1 },     // Territorial synapsid
            { DinosaurSpecies.Othnielia, 3 }       // Small herbivores
        };

        // Create zones for different dinosaur types across the expanded world
        var zones = new List<(Rectangle area, DinosaurSpecies[] allowedSpecies)>
        {
            // Far left zone - Small herbivores and fast predators
            (new Rectangle(100, 100, worldWidth / 6, worldHeight - 200),
             new[] { DinosaurSpecies.Othnielia, DinosaurSpecies.Velociraptor }),
            
            // Left zone - Medium herbivores
            (new Rectangle(worldWidth / 6 + 100, 100, worldWidth / 6, worldHeight - 200),
             new[] { DinosaurSpecies.Parasaurolophus, DinosaurSpecies.Anchylosaurus }),
            
            // Center-left zone - Large herbivores
            (new Rectangle(2 * worldWidth / 6 + 100, 100, worldWidth / 6, worldHeight - 200),
             new[] { DinosaurSpecies.Brontosaurus, DinosaurSpecies.Triceratops }),
            
            // Center-right zone - Territorial species
            (new Rectangle(3 * worldWidth / 6 + 100, 100, worldWidth / 6, worldHeight - 200),
             new[] { DinosaurSpecies.Dimetrodon, DinosaurSpecies.Polacanthus }),
            
            // Right zone - Medium carnivores
            (new Rectangle(4 * worldWidth / 6 + 100, 100, worldWidth / 6, worldHeight - 200),
             new[] { DinosaurSpecies.Allosaurus, DinosaurSpecies.Velociraptor }),
            
            // Far right zone - Apex predators
            (new Rectangle(5 * worldWidth / 6 + 100, 100, worldWidth / 6 - 100, worldHeight - 200),
             new[] { DinosaurSpecies.TRex })
        };

        // Track used positions to avoid overlapping
        var usedPositions = new List<Vector2>();
        const float MIN_DISTANCE = 150f; // Minimum distance between dinosaurs

        // Create dinosaurs in their preferred zones
        foreach (var (species, count) in distribution)
        {
            var suitableZones = zones.Where(z => z.allowedSpecies.Contains(species)).ToList();

            for (int i = 0; i < count; i++)
            {
                Vector2 position;
                int attempts = 0;
                const int MAX_ATTEMPTS = 50;

                // Try to find a position that doesn't overlap with existing dinosaurs
                do
                {
                    // Pick a random suitable zone
                    var zone = suitableZones[_random.Next(suitableZones.Count)];

                    // Generate a random position within the zone
                    position = new Vector2(
                        zone.area.X + _random.Next(zone.area.Width),
                        zone.area.Y + _random.Next(zone.area.Height)
                    );

                    attempts++;
                } while (attempts < MAX_ATTEMPTS &&
                         usedPositions.Any(used => Vector2.Distance(used, position) < MIN_DISTANCE));

                // Add the position to used positions and create the dinosaur
                usedPositions.Add(position);
                CreateDinosaur(species, position);
            }
        }
    }

    /// <summary>
    /// Updates all dinosaurs.
    /// </summary>
    public void Update(float deltaTime)
    {
        foreach (var dinosaur in _dinosaurs)
        {
            dinosaur.AI.Update(deltaTime);
            dinosaur.Renderer.Update();
        }
    }

    /// <summary>
    /// Draws all dinosaurs.
    /// </summary>
    public void Draw(Camera2D camera)
    {
        foreach (var dinosaur in _dinosaurs)
        {
            dinosaur.Renderer.Draw(camera);
        }
    }

    /// <summary>
    /// Gets all dinosaur points for camera targeting.
    /// </summary>
    public IEnumerable<VerletPoint> GetAllDinosaurPoints()
    {
        return _dinosaurs.SelectMany(d => d.Entity.Points);
    }

    /// <summary>
    /// Resets all dinosaurs to their initial positions.
    /// </summary>
    public void ResetPositions()
    {
        // This could be implemented to reset dinosaur positions during game restart
        // For now, we'll leave it as a placeholder
    }
}

/// <summary>
/// Represents a single dinosaur instance with its entity, renderer, and AI.
/// </summary>
public class DinosaurInstance
{
    public NormalDinosaur Entity { get; }
    public DinosaurRenderer Renderer { get; }
    public IDinosaurAI AI { get; }
    public DinosaurSpecies Species { get; }
    public DinosaurBehavior Behavior { get; }

    public DinosaurInstance(NormalDinosaur entity, DinosaurRenderer renderer, IDinosaurAI ai,
                           DinosaurSpecies species, DinosaurBehavior behavior)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        AI = ai ?? throw new ArgumentNullException(nameof(ai));
        Species = species;
        Behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
    }
}

/// <summary>
/// Interface for dinosaur AI controllers.
/// </summary>
public interface IDinosaurAI
{
    void Update(float deltaTime);
}
