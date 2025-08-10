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
        /// Loads data from the binary file or creates empty data if file doesn't exist.
        /// </summary>
        private void LoadData()
        {
            _levelsData = BinarySerializer.LoadOrDefault(_filePath, () => new GameLevelsData());
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
