using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DinoGrr.Core.Physics;

/// <summary>
/// Diagnostic system for monitoring physics performance and detecting issues.
/// </summary>
public class PhysicsDiagnostics
{
    private readonly List<float> _frameTimeHistory = new List<float>();
    private readonly List<int> _collisionCountHistory = new List<int>();
    private readonly List<float> _maxVelocityHistory = new List<float>();
    private const int HistorySize = 60; // Keep 1 second of history at 60 FPS

    /// <summary>
    /// Gets the current physics frame time in milliseconds.
    /// </summary>
    public float CurrentFrameTime { get; private set; }

    /// <summary>
    /// Gets the current number of collisions detected.
    /// </summary>
    public int CurrentCollisionCount { get; private set; }

    /// <summary>
    /// Gets the current maximum velocity in the system.
    /// </summary>
    public float CurrentMaxVelocity { get; private set; }

    /// <summary>
    /// Gets the average frame time over the history period.
    /// </summary>
    public float AverageFrameTime => _frameTimeHistory.Count > 0 ? _frameTimeHistory.Average() : 0f;

    /// <summary>
    /// Gets the average collision count over the history period.
    /// </summary>
    public float AverageCollisionCount => _collisionCountHistory.Count > 0 ? (float)_collisionCountHistory.Average() : 0f;

    /// <summary>
    /// Gets the average maximum velocity over the history period.
    /// </summary>
    public float AverageMaxVelocity => _maxVelocityHistory.Count > 0 ? _maxVelocityHistory.Average() : 0f;

    /// <summary>
    /// Updates the diagnostics with current frame data.
    /// </summary>
    /// <param name="frameTime">Time taken for physics update in milliseconds.</param>
    /// <param name="collisionCount">Number of collisions detected this frame.</param>
    /// <param name="maxVelocity">Maximum velocity in the system this frame.</param>
    public void UpdateFrame(float frameTime, int collisionCount, float maxVelocity)
    {
        CurrentFrameTime = frameTime;
        CurrentCollisionCount = collisionCount;
        CurrentMaxVelocity = maxVelocity;

        // Add to history
        _frameTimeHistory.Add(frameTime);
        _collisionCountHistory.Add(collisionCount);
        _maxVelocityHistory.Add(maxVelocity);

        // Maintain history size
        if (_frameTimeHistory.Count > HistorySize)
        {
            _frameTimeHistory.RemoveAt(0);
            _collisionCountHistory.RemoveAt(0);
            _maxVelocityHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// Checks for potential physics issues and returns warnings.
    /// </summary>
    /// <returns>List of warning messages.</returns>
    public List<string> GetWarnings()
    {
        var warnings = new List<string>();

        if (AverageFrameTime > 16.67f) // More than 16.67ms means below 60 FPS
        {
            warnings.Add($"Physics performance issue: Average frame time {AverageFrameTime:F2}ms (target: <16.67ms)");
        }

        if (CurrentMaxVelocity > PhysicsConfig.MaxVelocity * 0.9f)
        {
            warnings.Add($"High velocity detected: {CurrentMaxVelocity:F2} (max: {PhysicsConfig.MaxVelocity})");
        }

        if (AverageCollisionCount > 100)
        {
            warnings.Add($"High collision count: {AverageCollisionCount:F0} (consider spatial partitioning)");
        }

        if (_maxVelocityHistory.Count >= 10 && _maxVelocityHistory.TakeLast(10).All(v => v > PhysicsConfig.MaxVelocity * 0.8f))
        {
            warnings.Add("Sustained high velocities detected - potential tunneling risk");
        }

        return warnings;
    }

    /// <summary>
    /// Resets all diagnostic data.
    /// </summary>
    public void Reset()
    {
        _frameTimeHistory.Clear();
        _collisionCountHistory.Clear();
        _maxVelocityHistory.Clear();
        CurrentFrameTime = 0f;
        CurrentCollisionCount = 0;
        CurrentMaxVelocity = 0f;
    }

    /// <summary>
    /// Gets a summary of current physics state.
    /// </summary>
    /// <returns>Formatted summary string.</returns>
    public string GetSummary()
    {
        return $"Physics Summary:\n" +
               $"  Frame Time: {CurrentFrameTime:F2}ms (avg: {AverageFrameTime:F2}ms)\n" +
               $"  Collisions: {CurrentCollisionCount} (avg: {AverageCollisionCount:F0})\n" +
               $"  Max Velocity: {CurrentMaxVelocity:F2} (avg: {AverageMaxVelocity:F2})\n" +
               $"  Gravity: {PhysicsConfig.Gravity}\n" +
               $"  Sub-steps: {PhysicsConfig.DefaultSubSteps}";
    }
}
