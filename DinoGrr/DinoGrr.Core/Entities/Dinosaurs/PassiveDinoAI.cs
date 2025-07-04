using System;

namespace DinoGrr.Core.Entities.Dinosaurs;

/// <summary>
/// AI that makes a dinosaur passive - rarely moves, mostly peaceful.
/// </summary>
public class PassiveDinoAI : IDinosaurAI
{
    private readonly NormalDinosaur _dinosaur;
    private readonly DinosaurBehavior _behavior;
    private float _timeSinceLastAction;
    private float _nextActionTime;
    private readonly Random _random;

    public PassiveDinoAI(NormalDinosaur dinosaur, DinosaurBehavior behavior)
    {
        _dinosaur = dinosaur ?? throw new ArgumentNullException(nameof(dinosaur));
        _behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
        _random = new Random();
        ResetActionTimer();
    }

    public void Update(float deltaTime)
    {
        _timeSinceLastAction += deltaTime;

        if (_timeSinceLastAction >= _nextActionTime && _dinosaur.CanJump)
        {
            // Only move occasionally and randomly
            if (_random.NextDouble() < 0.4) // 40% chance to actually move
            {
                int randomDirection = _random.Next(-1, 2);
                if (randomDirection == -1)
                    _dinosaur.JumpLeft();
                else if (randomDirection == 1)
                    _dinosaur.JumpRight();
                else
                    _dinosaur.Jump();
            }

            ResetActionTimer();
        }
    }

    private void ResetActionTimer()
    {
        _timeSinceLastAction = 0;
        _nextActionTime = _behavior.ActionInterval.min +
                         (float)_random.NextDouble() *
                         (_behavior.ActionInterval.max - _behavior.ActionInterval.min);
    }
}
