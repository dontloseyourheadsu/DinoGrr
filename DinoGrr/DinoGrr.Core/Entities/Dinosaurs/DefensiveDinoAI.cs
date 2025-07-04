using System;
using Microsoft.Xna.Framework;
using DinoGrr.Core.Entities.Player;

namespace DinoGrr.Core.Entities.Dinosaurs;

/// <summary>
/// AI that makes a dinosaur defensive - attacks when DinoGirl gets too close.
/// </summary>
public class DefensiveDinoAI : IDinosaurAI
{
    private readonly NormalDinosaur _dinosaur;
    private readonly DinoGirl _target;
    private readonly DinosaurBehavior _behavior;
    private float _timeSinceLastAction;
    private float _nextActionTime;
    private readonly Random _random;

    public DefensiveDinoAI(NormalDinosaur dinosaur, DinoGirl target, DinosaurBehavior behavior)
    {
        _dinosaur = dinosaur ?? throw new ArgumentNullException(nameof(dinosaur));
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
        _random = new Random();
        ResetActionTimer();
    }

    public void Update(float deltaTime)
    {
        _timeSinceLastAction += deltaTime;

        if (_timeSinceLastAction >= _nextActionTime && _dinosaur.CanJump)
        {
            float distanceToTarget = GetDistanceToTarget();

            // Only react if target is within defensive range
            if (distanceToTarget <= _behavior.MaxTargetDistance)
            {
                Vector2 direction = GetDirectionToTarget();

                // Attack towards target when threatened
                if (Math.Abs(direction.X) > 0.1f)
                {
                    if (direction.X < 0)
                        _dinosaur.JumpLeft();
                    else
                        _dinosaur.JumpRight();
                }
                else
                {
                    _dinosaur.Jump();
                }
            }
            else
            {
                // Random passive movement when not threatened
                if (_random.NextDouble() < 0.3) // 30% chance to move
                {
                    int randomDirection = _random.Next(-1, 2);
                    if (randomDirection == -1)
                        _dinosaur.JumpLeft();
                    else if (randomDirection == 1)
                        _dinosaur.JumpRight();
                    else
                        _dinosaur.Jump();
                }
            }

            ResetActionTimer();
        }
    }

    private float GetDistanceToTarget()
    {
        if (_dinosaur.Points.Count == 0 || _target.Points.Count == 0)
            return float.MaxValue;

        Vector2 dinoPosition = _dinosaur.Points[0].Position;
        Vector2 targetPosition = _target.Points[0].Position;
        return Vector2.Distance(dinoPosition, targetPosition);
    }

    private Vector2 GetDirectionToTarget()
    {
        if (_dinosaur.Points.Count == 0 || _target.Points.Count == 0)
            return Vector2.Zero;

        Vector2 dinoPosition = _dinosaur.Points[0].Position;
        Vector2 targetPosition = _target.Points[0].Position;
        Vector2 direction = targetPosition - dinoPosition;
        return direction.Length() > 0 ? Vector2.Normalize(direction) : Vector2.Zero;
    }

    private void ResetActionTimer()
    {
        _timeSinceLastAction = 0;
        _nextActionTime = _behavior.ActionInterval.min +
                         (float)_random.NextDouble() *
                         (_behavior.ActionInterval.max - _behavior.ActionInterval.min);
    }
}
