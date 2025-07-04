using System;

namespace DinoGrr.Core.Entities.Dinosaurs;

/// <summary>
/// Adds random movement behavior to a dinosaur.
/// </summary>
public class RandomDinoMover : IDinosaurAI
{
    private readonly NormalDinosaur _dinosaur;
    private float _timeSinceLastJump;
    private float _nextJumpTime;
    private readonly Random _random;

    /// <summary>
    /// Creates a new random movement controller for a dinosaur.
    /// </summary>
    /// <param name="dinosaur">The dinosaur to control.</param>
    public RandomDinoMover(NormalDinosaur dinosaur)
    {
        _dinosaur = dinosaur ?? throw new ArgumentNullException(nameof(dinosaur));
        _random = new Random();
        ResetJumpTimer();
    }

    /// <summary>
    /// Updates the random movement behavior.
    /// </summary>
    /// <param name="deltaTime">Time since last update in seconds.</param>
    public void Update(float deltaTime)
    {
        _timeSinceLastJump += deltaTime;

        // Check if it's time for a random jump and the dinosaur can jump
        if (_timeSinceLastJump >= _nextJumpTime && _dinosaur.CanJump)
        {
            // Pick a random jump direction: -1 (left), 0 (up), 1 (right)
            int direction = _random.Next(-1, 2);

            // Perform the jump
            if (direction == -1)
            {
                _dinosaur.JumpLeft();
            }
            else if (direction == 1)
            {
                _dinosaur.JumpRight();
            }
            else
            {
                _dinosaur.Jump(0); // Jump straight up
            }

            // Reset the jump timer
            ResetJumpTimer();
        }
    }

    /// <summary>
    /// Resets the jump timer with a random delay.
    /// </summary>
    private void ResetJumpTimer()
    {
        _timeSinceLastJump = 0;

        // Random time between 1-4 seconds until next jump
        _nextJumpTime = 1.0f + (float)_random.NextDouble() * 3.0f;
    }
}