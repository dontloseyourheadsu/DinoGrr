using Microsoft.Xna.Framework;

namespace DinoGrr.Core.Entities.Dinosaurs;

/// <summary>
/// Defines the different species of dinosaurs available in the game.
/// </summary>
public enum DinosaurSpecies
{
    Allosaurus,
    Anchylosaurus,
    Brontosaurus,
    Dimetrodon,
    Othnielia,
    Parasaurolophus,
    Polacanthus,
    TRex,
    Triceratops,
    Velociraptor
}

/// <summary>
/// Contains data about different dinosaur species including their realistic sizes and behaviors.
/// </summary>
public static class DinosaurSpeciesData
{
    /// <summary>
    /// Gets the size data for a specific dinosaur species.
    /// Sizes are based on realistic dinosaur proportions, scaled for the game.
    /// </summary>
    public static (float width, float height) GetSize(DinosaurSpecies species)
    {
        return species switch
        {
            // Large carnivores
            DinosaurSpecies.TRex => (180f, 120f),          // Massive predator
            DinosaurSpecies.Allosaurus => (160f, 100f),    // Large predator

            // Large herbivores
            DinosaurSpecies.Brontosaurus => (200f, 140f),  // Massive long-necked herbivore
            DinosaurSpecies.Triceratops => (170f, 90f),    // Heavy herbivore with frill
            DinosaurSpecies.Parasaurolophus => (150f, 110f), // Large duck-billed herbivore

            // Medium-sized dinosaurs
            DinosaurSpecies.Dimetrodon => (140f, 100f),    // Sail-backed synapsid
            DinosaurSpecies.Anchylosaurus => (120f, 70f),  // Armored herbivore
            DinosaurSpecies.Polacanthus => (130f, 80f),    // Spiky herbivore

            // Small dinosaurs
            DinosaurSpecies.Velociraptor => (80f, 60f),    // Fast predator
            DinosaurSpecies.Othnielia => (70f, 50f),       // Small herbivore

            _ => (120f, 80f) // Default size
        };
    }

    /// <summary>
    /// Gets the texture path for a specific dinosaur species.
    /// </summary>
    public static string GetTexturePath(DinosaurSpecies species)
    {
        return species switch
        {
            DinosaurSpecies.Allosaurus => "Assets/Dinosaurs/alosaur_orange",
            DinosaurSpecies.Anchylosaurus => "Assets/Dinosaurs/anchylosaur_yellow",
            DinosaurSpecies.Brontosaurus => "Assets/Dinosaurs/bronchosaur_purple",
            DinosaurSpecies.Dimetrodon => "Assets/Dinosaurs/dimetrodon_red",
            DinosaurSpecies.Othnielia => "Assets/Dinosaurs/othniela_green",
            DinosaurSpecies.Parasaurolophus => "Assets/Dinosaurs/parasaur_blue",
            DinosaurSpecies.Polacanthus => "Assets/Dinosaurs/polacantus_purple",
            DinosaurSpecies.TRex => "Assets/Dinosaurs/trex_red",
            DinosaurSpecies.Triceratops => "Assets/Dinosaurs/triceratops_cyan",
            DinosaurSpecies.Velociraptor => "Assets/Dinosaurs/velociraptor_cyan",
            _ => "Assets/Dinosaurs/triceratops_cyan"
        };
    }

    /// <summary>
    /// Gets the behavioral characteristics for a specific dinosaur species.
    /// </summary>
    public static DinosaurBehavior GetBehavior(DinosaurSpecies species)
    {
        return species switch
        {
            // Aggressive carnivores
            DinosaurSpecies.TRex => new DinosaurBehavior
            {
                Type = BehaviorType.Aggressive,
                JumpForce = 3.5f,
                Speed = 0.8f,
                ActionInterval = (1.0f, 2.5f),
                MaxTargetDistance = 400f
            },
            DinosaurSpecies.Allosaurus => new DinosaurBehavior
            {
                Type = BehaviorType.Aggressive,
                JumpForce = 3.0f,
                Speed = 1.0f,
                ActionInterval = (0.8f, 2.0f),
                MaxTargetDistance = 350f
            },
            DinosaurSpecies.Velociraptor => new DinosaurBehavior
            {
                Type = BehaviorType.Aggressive,
                JumpForce = 4.0f,
                Speed = 1.5f,
                ActionInterval = (0.5f, 1.5f),
                MaxTargetDistance = 300f
            },

            // Defensive herbivores
            DinosaurSpecies.Triceratops => new DinosaurBehavior
            {
                Type = BehaviorType.Defensive,
                JumpForce = 2.5f,
                Speed = 0.6f,
                ActionInterval = (2.0f, 4.0f),
                MaxTargetDistance = 200f
            },
            DinosaurSpecies.Anchylosaurus => new DinosaurBehavior
            {
                Type = BehaviorType.Defensive,
                JumpForce = 2.0f,
                Speed = 0.4f,
                ActionInterval = (3.0f, 5.0f),
                MaxTargetDistance = 150f
            },
            DinosaurSpecies.Polacanthus => new DinosaurBehavior
            {
                Type = BehaviorType.Defensive,
                JumpForce = 2.2f,
                Speed = 0.5f,
                ActionInterval = (2.5f, 4.0f),
                MaxTargetDistance = 180f
            },

            // Passive herbivores
            DinosaurSpecies.Brontosaurus => new DinosaurBehavior
            {
                Type = BehaviorType.Passive,
                JumpForce = 2.0f,
                Speed = 0.3f,
                ActionInterval = (4.0f, 8.0f),
                MaxTargetDistance = 100f
            },
            DinosaurSpecies.Parasaurolophus => new DinosaurBehavior
            {
                Type = BehaviorType.Passive,
                JumpForce = 2.5f,
                Speed = 0.7f,
                ActionInterval = (3.0f, 6.0f),
                MaxTargetDistance = 120f
            },
            DinosaurSpecies.Othnielia => new DinosaurBehavior
            {
                Type = BehaviorType.Passive,
                JumpForce = 3.5f,
                Speed = 1.2f,
                ActionInterval = (2.0f, 4.0f),
                MaxTargetDistance = 100f
            },

            // Special cases
            DinosaurSpecies.Dimetrodon => new DinosaurBehavior
            {
                Type = BehaviorType.Territorial,
                JumpForce = 2.8f,
                Speed = 0.7f,
                ActionInterval = (1.5f, 3.0f),
                MaxTargetDistance = 250f
            },

            _ => new DinosaurBehavior
            {
                Type = BehaviorType.Random,
                JumpForce = 2.5f,
                Speed = 0.8f,
                ActionInterval = (1.0f, 3.0f),
                MaxTargetDistance = 200f
            }
        };
    }
}

/// <summary>
/// Defines the behavior types for dinosaurs.
/// </summary>
public enum BehaviorType
{
    Random,      // Moves randomly
    Aggressive,  // Actively hunts DinoGirl
    Defensive,   // Attacks when DinoGirl gets too close
    Passive,     // Rarely interacts, mostly wanders
    Territorial  // Guards a specific area
}

/// <summary>
/// Contains behavioral data for a dinosaur species.
/// </summary>
public class DinosaurBehavior
{
    public BehaviorType Type { get; set; }
    public float JumpForce { get; set; }
    public float Speed { get; set; }
    public (float min, float max) ActionInterval { get; set; }
    public float MaxTargetDistance { get; set; }
}
