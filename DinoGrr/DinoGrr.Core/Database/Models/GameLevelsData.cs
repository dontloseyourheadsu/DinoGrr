using System;
using System.Collections.Generic;

namespace DinoGrr.Core.Database.Models
{
    /// <summary>
    /// Container for all game levels data for serialization.
    /// </summary>
    public class GameLevelsData
    {
        /// <summary>
        /// List of all game levels.
        /// </summary>
        public List<GameLevel> Levels { get; set; }

        /// <summary>
        /// Version of the data format for migration purposes.
        /// </summary>
        public int DataVersion { get; set; }

        /// <summary>
        /// When this data was last saved.
        /// </summary>
        public DateTime LastSaved { get; set; }

        /// <summary>
        /// Creates a new GameLevelsData instance.
        /// </summary>
        public GameLevelsData()
        {
            Levels = new List<GameLevel>();
            DataVersion = 1;
            LastSaved = DateTime.Now;
        }
    }
}
