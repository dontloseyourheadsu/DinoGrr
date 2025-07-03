using Microsoft.Xna.Framework;

namespace DinoGrr.Core.Physics;

/// <summary>
/// Centralized configuration for physics parameters.
/// Allows easy tweaking of physics behavior across the entire system.
/// </summary>
public static class PhysicsConfig
{
    /// <summary>
    /// Global gravity vector.
    /// </summary>
    public static Vector2 Gravity { get; set; } = new Vector2(0, 15.0f * 30);

    /// <summary>
    /// Global damping factor for collisions (0.0 to 1.0).
    /// </summary>
    public static float GlobalDamping { get; set; } = 0.2f;

    /// <summary>
    /// Maximum velocity allowed for any physics object.
    /// </summary>
    public static float MaxVelocity { get; set; } = 800f;

    /// <summary>
    /// Global velocity damping factor applied each frame.
    /// </summary>
    public static float VelocityDamping { get; set; } = 0.999f;

    /// <summary>
    /// Friction coefficient for ground collisions.
    /// </summary>
    public static float GroundFriction { get; set; } = 0.90f;

    /// <summary>
    /// Friction coefficient for wall collisions.
    /// </summary>
    public static float WallFriction { get; set; } = 0.92f;

    /// <summary>
    /// Friction coefficient for object-to-object collisions.
    /// </summary>
    public static float ObjectFriction { get; set; } = 0.8f; // Increased from 0.7f for better traction on objects

    /// <summary>
    /// Friction coefficient for trampoline surfaces (higher for better grip).
    /// </summary>
    public static float TrampolineFriction { get; set; } = 0.8f;

    /// <summary>
    /// Friction coefficient for ice surfaces (very low friction).
    /// </summary>
    public static float IceFriction { get; set; } = 0.98f;

    /// <summary>
    /// Default surface friction for unmarked surfaces.
    /// </summary>
    public static float DefaultSurfaceFriction { get; set; } = 0.9f;

    /// <summary>
    /// Number of default sub-steps for physics simulation.
    /// </summary>
    public static int DefaultSubSteps { get; set; } = 12;

    /// <summary>
    /// Maximum number of sub-steps allowed (performance cap).
    /// </summary>
    public static int MaxSubSteps { get; set; } = 20;

    /// <summary>
    /// Number of constraint satisfaction iterations per sub-step.
    /// </summary>
    public static int ConstraintIterations { get; set; } = 2;

    /// <summary>
    /// Number of spring satisfaction iterations per constraint iteration.
    /// </summary>
    public static int SpringIterations { get; set; } = 2;

    /// <summary>
    /// Percentage of position overlap to correct in collisions (0.0 to 1.0).
    /// </summary>
    public static float PositionCorrectionPercent { get; set; } = 0.8f;

    /// <summary>
    /// Small overlap allowance to improve stability.
    /// </summary>
    public static float PositionSlop { get; set; } = 0.01f;

    /// <summary>
    /// Minimum distance threshold for collision detection.
    /// </summary>
    public static float MinDistanceThreshold { get; set; } = 0.0001f;

    /// <summary>
    /// Resets all physics parameters to their default values.
    /// </summary>
    public static void ResetToDefaults()
    {
        Gravity = new Vector2(0, 15.0f * 30);
        GlobalDamping = 0.2f;
        MaxVelocity = 800f;
        VelocityDamping = 0.999f;
        GroundFriction = 0.95f;
        WallFriction = 0.92f;
        ObjectFriction = 0.9f;
        TrampolineFriction = 0.8f;
        IceFriction = 0.98f;
        DefaultSurfaceFriction = 0.9f;
        DefaultSubSteps = 12;
        MaxSubSteps = 20;
        ConstraintIterations = 2;
        SpringIterations = 2;
        PositionCorrectionPercent = 0.8f;
        PositionSlop = 0.01f;
        MinDistanceThreshold = 0.0001f;
    }

    /// <summary>
    /// Sets up configuration for high-performance scenarios (lower quality but faster).
    /// </summary>
    public static void SetPerformanceMode()
    {
        DefaultSubSteps = 6;
        MaxSubSteps = 10;
        ConstraintIterations = 1;
        SpringIterations = 1;
        PositionCorrectionPercent = 0.6f;
    }

    /// <summary>
    /// Sets up configuration for high-quality scenarios (higher quality but slower).
    /// </summary>
    public static void SetQualityMode()
    {
        DefaultSubSteps = 16;
        MaxSubSteps = 30;
        ConstraintIterations = 3;
        SpringIterations = 3;
        PositionCorrectionPercent = 0.9f;
    }

    /// <summary>
    /// Gets the appropriate friction coefficient based on surface type.
    /// </summary>
    /// <param name="surfaceTag">The tag identifying the surface type.</param>
    /// <returns>The friction coefficient for the surface.</returns>
    public static float GetSurfaceFriction(string surfaceTag)
    {
        if (string.IsNullOrEmpty(surfaceTag))
            return DefaultSurfaceFriction;

        return surfaceTag.ToLower() switch
        {
            var tag when tag.Contains("trampoline") => TrampolineFriction,
            var tag when tag.Contains("ice") => IceFriction,
            var tag when tag.Contains("ground") => GroundFriction,
            var tag when tag.Contains("wall") => WallFriction,
            _ => DefaultSurfaceFriction
        };
    }
}
