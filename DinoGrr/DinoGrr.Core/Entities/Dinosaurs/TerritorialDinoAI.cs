using System;
using Microsoft.Xna.Framework;
using DinoGrr.Core.Entities.Player;

namespace DinoGrr.Core.Entities.Dinosaurs;

/// <summary>
/// AI that makes a dinosaur territorial - guards a specific area and attacks intruders.
/// </summary>
public class TerritorialDinoAI : IDinosaurAI
{
    private readonly NormalDinosaur _dinosaur;
    private readonly DinoGirl _target;
    private readonly Vector2 _territory;
    private readonly DinosaurBehavior _behavior;
    private float _timeSinceLastAction;
    private float _nextActionTime;
    private readonly Random _random;
    private const float TERRITORY_RADIUS = 150f;

    public TerritorialDinoAI(NormalDinosaur dinosaur, DinoGirl target, Vector2 territory, DinosaurBehavior behavior)
    {
        _dinosaur = dinosaur ?? throw new ArgumentNullException(nameof(dinosaur));
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _territory = territory;
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
            float distanceToTerritory = GetDistanceToTerritory();

            // Priority 1: Attack intruders in territory
            if (distanceToTarget <= _behavior.MaxTargetDistance && IsTargetInTerritory())
            {
                Vector2 direction = GetDirectionToTarget();

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
            // Priority 2: Return to territory if too far away
            else if (distanceToTerritory > TERRITORY_RADIUS)
            {
                Vector2 directionToTerritory = GetDirectionToTerritory();

                if (Math.Abs(directionToTerritory.X) > 0.1f)
                {
                    if (directionToTerritory.X < 0)
                        _dinosaur.JumpLeft();
                    else
                        _dinosaur.JumpRight();
                }
                else
                {
                    _dinosaur.Jump();
                }
            }
            // Priority 3: Random patrol within territory
            else
            {
                if (_random.NextDouble() < 0.6) // 60% chance to patrol
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

    private float GetDistanceToTerritory()
    {
        if (_dinosaur.Points.Count == 0)
            return float.MaxValue;

        Vector2 dinoPosition = _dinosaur.Points[0].Position;
        return Vector2.Distance(dinoPosition, _territory);
    }

    private bool IsTargetInTerritory()
    {
        if (_target.Points.Count == 0)
            return false;

        Vector2 targetPosition = _target.Points[0].Position;
        return Vector2.Distance(targetPosition, _territory) <= TERRITORY_RADIUS;
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

    private Vector2 GetDirectionToTerritory()
    {
        if (_dinosaur.Points.Count == 0)
            return Vector2.Zero;

        Vector2 dinoPosition = _dinosaur.Points[0].Position;
        Vector2 direction = _territory - dinoPosition;
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
