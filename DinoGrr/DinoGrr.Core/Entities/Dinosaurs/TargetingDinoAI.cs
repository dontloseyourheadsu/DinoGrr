using System;
using Microsoft.Xna.Framework;
using DinoGrr.Core.Entities.Player;

namespace DinoGrr.Core.Entities.Dinosaurs;

/// <summary>
/// AI that makes a dinosaur target and move towards DinoGirl.
/// </summary>
public class TargetingDinoAI : IDinosaurAI
{
    private readonly NormalDinosaur _dinosaur;
    private readonly DinoGirl _target;
    private readonly float _maxTargetDistance;
    private float _timeSinceLastAction;
    private float _nextActionTime;
    private readonly Random _random;

    // AI parameters
    private const float MIN_ACTION_INTERVAL = 0.5f; // Minimum time between actions
    private const float MAX_ACTION_INTERVAL = 2.0f; // Maximum time between actions
    private const float JUMP_THRESHOLD = 100f; // Distance at which dinosaur will jump towards target

    /// <summary>
    /// Creates a new targeting AI for a dinosaur.
    /// </summary>
    /// <param name="dinosaur">The dinosaur to control.</param>
    /// <param name="target">The DinoGirl to target.</param>
    /// <param name="maxTargetDistance">Maximum distance to pursue the target.</param>
    public TargetingDinoAI(NormalDinosaur dinosaur, DinoGirl target, float maxTargetDistance)
    {
        _dinosaur = dinosaur ?? throw new ArgumentNullException(nameof(dinosaur));
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _maxTargetDistance = maxTargetDistance;
        _random = new Random();
        ResetActionTimer();
    }

    /// <summary>
    /// Updates the targeting AI behavior.
    /// </summary>
    /// <param name="deltaTime">Time since last update in seconds.</param>
    public void Update(float deltaTime)
    {
        _timeSinceLastAction += deltaTime;

        // Check if it's time for the next action and the dinosaur can move
        if (_timeSinceLastAction >= _nextActionTime && _dinosaur.CanJump)
        {
            // Get the distance to the target
            float distanceToTarget = GetDistanceToTarget();

            // Only pursue if within range
            if (distanceToTarget <= _maxTargetDistance)
            {
                PerformTargetingAction(distanceToTarget);
            }

            // Reset the action timer
            ResetActionTimer();
        }
    }

    /// <summary>
    /// Calculates the distance between the dinosaur and DinoGirl.
    /// </summary>
    /// <returns>Distance to the target.</returns>
    private float GetDistanceToTarget()
    {
        if (_dinosaur.Points.Count == 0 || _target.Points.Count == 0)
            return float.MaxValue;

        Vector2 dinoPosition = _dinosaur.Points[0].Position;
        Vector2 targetPosition = _target.Points[0].Position;

        return Vector2.Distance(dinoPosition, targetPosition);
    }

    /// <summary>
    /// Performs an action based on the target's position.
    /// </summary>
    /// <param name="distanceToTarget">Current distance to the target.</param>
    private void PerformTargetingAction(float distanceToTarget)
    {
        if (_dinosaur.Points.Count == 0 || _target.Points.Count == 0)
            return;

        Vector2 dinoPosition = _dinosaur.Points[0].Position;
        Vector2 targetPosition = _target.Points[0].Position;

        // Calculate direction to target
        Vector2 directionToTarget = targetPosition - dinoPosition;
        directionToTarget.Normalize();

        // Determine the action based on distance and direction
        if (distanceToTarget > JUMP_THRESHOLD)
        {
            // Far away - jump towards target
            if (Math.Abs(directionToTarget.X) > 0.3f) // Only jump if there's significant horizontal distance
            {
                if (directionToTarget.X > 0)
                {
                    _dinosaur.JumpRight();
                }
                else
                {
                    _dinosaur.JumpLeft();
                }
            }
            else
            {
                // Target is mostly above/below - jump straight up
                _dinosaur.Jump(0);
            }
        }
        else
        {
            // Close to target - be more aggressive
            // 70% chance to jump towards target, 30% chance for unpredictable movement
            if (_random.NextDouble() < 0.7)
            {
                // Jump towards target
                if (directionToTarget.X > 0.2f)
                {
                    _dinosaur.JumpRight();
                }
                else if (directionToTarget.X < -0.2f)
                {
                    _dinosaur.JumpLeft();
                }
                else
                {
                    _dinosaur.Jump(0);
                }
            }
            else
            {
                // Random movement for unpredictability
                int randomDirection = _random.Next(-1, 2);
                if (randomDirection == -1)
                {
                    _dinosaur.JumpLeft();
                }
                else if (randomDirection == 1)
                {
                    _dinosaur.JumpRight();
                }
                else
                {
                    _dinosaur.Jump(0);
                }
            }
        }
    }

    /// <summary>
    /// Resets the action timer with a random delay.
    /// </summary>
    private void ResetActionTimer()
    {
        _timeSinceLastAction = 0;

        // Random time between actions
        _nextActionTime = MIN_ACTION_INTERVAL + (float)_random.NextDouble() * (MAX_ACTION_INTERVAL - MIN_ACTION_INTERVAL);
    }

    /// <summary>
    /// Gets whether the target is within pursuit range.
    /// </summary>
    /// <returns>True if the target is within range, false otherwise.</returns>
    public bool IsTargetInRange()
    {
        return GetDistanceToTarget() <= _maxTargetDistance;
    }

    /// <summary>
    /// Gets the current distance to the target.
    /// </summary>
    /// <returns>Distance to the target in pixels.</returns>
    public float GetCurrentDistanceToTarget()
    {
        return GetDistanceToTarget();
    }
}
