using System;

namespace DinoGrr.Core.Database.Models
{
    /// <summary>
    /// Represents a game level with its metadata and completion status.
    /// </summary>
    public class GameLevel
    {
        /// <summary>
        /// Unique identifier for the level.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Display name of the level.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Brief description of the level.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Difficulty rating (1-5).
        /// </summary>
        public int Difficulty { get; set; }

        /// <summary>
        /// Whether the level has been unlocked by the player.
        /// </summary>
        public bool IsUnlocked { get; set; }

        /// <summary>
        /// Whether the player has completed this level.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Best score achieved on this level.
        /// </summary>
        public int BestScore { get; set; }

        /// <summary>
        /// Best completion time in seconds.
        /// </summary>
        public float BestTime { get; set; }

        /// <summary>
        /// Number of stars earned (1-3).
        /// </summary>
        public int StarsEarned { get; set; }

        /// <summary>
        /// When this level was first unlocked.
        /// </summary>
        public DateTime? UnlockedDate { get; set; }

        /// <summary>
        /// When this level was first completed.
        /// </summary>
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Creates a new GameLevel instance.
        /// </summary>
        public GameLevel()
        {
            Name = string.Empty;
            Description = string.Empty;
            IsUnlocked = false;
            IsCompleted = false;
            BestScore = 0;
            BestTime = 0f;
            StarsEarned = 0;
        }

        /// <summary>
        /// Creates a new GameLevel with specified parameters.
        /// </summary>
        public GameLevel(int id, string name, string description, int difficulty, bool isUnlocked = false)
        {
            Id = id;
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            Difficulty = difficulty;
            IsUnlocked = isUnlocked;
            IsCompleted = false;
            BestScore = 0;
            BestTime = 0f;
            StarsEarned = 0;
        }
    }
}
