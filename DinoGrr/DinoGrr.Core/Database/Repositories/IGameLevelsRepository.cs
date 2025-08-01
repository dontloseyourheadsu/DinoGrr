using System;
using System.Collections.Generic;
using DinoGrr.Core.Database.Models;

namespace DinoGrr.Core.Database.Repositories
{
    /// <summary>
    /// Interface for game levels repository operations.
    /// Follows repository pattern for data access abstraction.
    /// </summary>
    public interface IGameLevelsRepository
    {
        /// <summary>
        /// Gets all game levels.
        /// </summary>
        /// <returns>List of all game levels.</returns>
        IReadOnlyList<GameLevel> GetAllLevels();

        /// <summary>
        /// Gets a specific level by ID.
        /// </summary>
        /// <param name="levelId">The ID of the level to retrieve.</param>
        /// <returns>The game level, or null if not found.</returns>
        GameLevel GetLevel(int levelId);

        /// <summary>
        /// Gets all unlocked levels.
        /// </summary>
        /// <returns>List of unlocked levels.</returns>
        IReadOnlyList<GameLevel> GetUnlockedLevels();

        /// <summary>
        /// Gets all completed levels.
        /// </summary>
        /// <returns>List of completed levels.</returns>
        IReadOnlyList<GameLevel> GetCompletedLevels();

        /// <summary>
        /// Unlocks a specific level.
        /// </summary>
        /// <param name="levelId">The ID of the level to unlock.</param>
        /// <returns>True if the level was unlocked, false if it was already unlocked or doesn't exist.</returns>
        bool UnlockLevel(int levelId);

        /// <summary>
        /// Marks a level as completed with score and time.
        /// </summary>
        /// <param name="levelId">The ID of the level.</param>
        /// <param name="score">The score achieved.</param>
        /// <param name="timeInSeconds">The completion time in seconds.</param>
        /// <param name="starsEarned">Number of stars earned (1-3).</param>
        /// <returns>True if the level was updated successfully.</returns>
        bool CompleteLevel(int levelId, int score, float timeInSeconds, int starsEarned);

        /// <summary>
        /// Saves all changes to the binary file.
        /// </summary>
        void Save();

        /// <summary>
        /// Reloads data from the binary file.
        /// </summary>
        void Reload();

        /// <summary>
        /// Resets all level progress (for debugging/testing).
        /// </summary>
        void ResetAllProgress();

        /// <summary>
        /// Gets the total number of levels.
        /// </summary>
        int TotalLevels { get; }

        /// <summary>
        /// Gets the number of unlocked levels.
        /// </summary>
        int UnlockedLevelsCount { get; }

        /// <summary>
        /// Gets the number of completed levels.
        /// </summary>
        int CompletedLevelsCount { get; }
    }
}
