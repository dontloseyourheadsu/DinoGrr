using System;
using System.Collections.Generic;
using System.Linq;
using DinoGrr.Core.Database.Models;

namespace DinoGrr.Core.Database.Repositories
{
    /// <summary>
    /// Repository for managing game levels data using binary serialization.
    /// Implements the repository pattern with dependency injection design.
    /// </summary>
    public class GameLevelsRepository : IGameLevelsRepository
    {
        private const string DATABASE_FILE_NAME = "game-levels";

        private readonly DatabaseFactory _databaseFactory;
        private GameLevelsData _levelsData;
        private readonly string _filePath;

        /// <summary>
        /// Creates a new GameLevelsRepository instance.
        /// </summary>
        /// <param name="databaseFactory">The database factory for file operations.</param>
        public GameLevelsRepository(DatabaseFactory databaseFactory)
        {
            _databaseFactory = databaseFactory ?? throw new ArgumentNullException(nameof(databaseFactory));
            _filePath = _databaseFactory.GetDatabasePath(DATABASE_FILE_NAME);

            LoadData();
        }

        /// <summary>
        /// Loads data from the binary file or creates default data.
        /// </summary>
        private void LoadData()
        {
            _levelsData = BinarySerializer.LoadOrDefault(_filePath, CreateDefaultLevelsData);
        }

        /// <summary>
        /// Creates default levels data when no save file exists.
        /// </summary>
        private GameLevelsData CreateDefaultLevelsData()
        {
            var data = new GameLevelsData();

            // Create some default levels
            data.Levels.AddRange(new[]
            {
                new GameLevel(1, "Welcome to DinoGrr", "Learn the basic controls and meet DinoGirl!", 1, true),
                new GameLevel(2, "First Steps", "Practice walking and jumping with DinoGirl.", 1, false),
                new GameLevel(3, "Dinosaur Encounter", "Meet your first friendly dinosaur!", 2, false),
                new GameLevel(4, "The Trampoline", "Learn to use trampolines for high jumps.", 2, false),
                new GameLevel(5, "Multiple Dinosaurs", "Navigate around several dinosaurs.", 3, false),
                new GameLevel(6, "Speed Run", "Complete the level as fast as possible!", 3, false),
                new GameLevel(7, "Survival Mode", "Avoid aggressive dinosaurs while collecting items.", 4, false),
                new GameLevel(8, "The Great Migration", "Help DinoGirl navigate through a dinosaur migration.", 4, false),
                new GameLevel(9, "Boss Battle", "Face off against the mighty T-Rex!", 5, false),
                new GameLevel(10, "Master of DinoGrr", "The ultimate challenge for experienced players.", 5, false)
            });

            return data;
        }

        public IReadOnlyList<GameLevel> GetAllLevels()
        {
            return _levelsData.Levels.AsReadOnly();
        }

        public GameLevel GetLevel(int levelId)
        {
            return _levelsData.Levels.FirstOrDefault(l => l.Id == levelId);
        }

        public IReadOnlyList<GameLevel> GetUnlockedLevels()
        {
            return _levelsData.Levels.Where(l => l.IsUnlocked).ToList().AsReadOnly();
        }

        public IReadOnlyList<GameLevel> GetCompletedLevels()
        {
            return _levelsData.Levels.Where(l => l.IsCompleted).ToList().AsReadOnly();
        }

        public bool UnlockLevel(int levelId)
        {
            var level = GetLevel(levelId);
            if (level == null || level.IsUnlocked)
                return false;

            level.IsUnlocked = true;
            level.UnlockedDate = DateTime.Now;
            return true;
        }

        public bool CompleteLevel(int levelId, int score, float timeInSeconds, int starsEarned)
        {
            var level = GetLevel(levelId);
            if (level == null)
                return false;

            // Update completion status
            bool wasFirstCompletion = !level.IsCompleted;
            level.IsCompleted = true;

            if (wasFirstCompletion)
                level.CompletedDate = DateTime.Now;

            // Update best scores if this is better
            if (score > level.BestScore)
                level.BestScore = score;

            if (timeInSeconds < level.BestTime || level.BestTime == 0f)
                level.BestTime = timeInSeconds;

            if (starsEarned > level.StarsEarned)
                level.StarsEarned = Math.Max(1, Math.Min(3, starsEarned));

            // Auto-unlock next level if this was first completion
            if (wasFirstCompletion)
            {
                var nextLevel = GetLevel(levelId + 1);
                if (nextLevel != null && !nextLevel.IsUnlocked)
                {
                    UnlockLevel(nextLevel.Id);
                }
            }

            return true;
        }

        public void Save()
        {
            _levelsData.LastSaved = DateTime.Now;
            BinarySerializer.Save(_levelsData, _filePath);
        }

        public void Reload()
        {
            LoadData();
        }

        public void ResetAllProgress()
        {
            foreach (var level in _levelsData.Levels)
            {
                level.IsUnlocked = level.Id == 1; // Only first level unlocked
                level.IsCompleted = false;
                level.BestScore = 0;
                level.BestTime = 0f;
                level.StarsEarned = 0;
                level.UnlockedDate = level.Id == 1 ? DateTime.Now : null;
                level.CompletedDate = null;
            }
        }

        public int TotalLevels => _levelsData.Levels.Count;

        public int UnlockedLevelsCount => _levelsData.Levels.Count(l => l.IsUnlocked);

        public int CompletedLevelsCount => _levelsData.Levels.Count(l => l.IsCompleted);
    }
}
