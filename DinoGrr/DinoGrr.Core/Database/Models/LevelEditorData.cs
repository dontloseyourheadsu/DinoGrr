using System;

namespace DinoGrr.Core.Database.Models
{
    /// <summary>
    /// Data model for passing level information to the level editor.
    /// Contains both metadata and level content for editing.
    /// </summary>
    public class LevelEditorData
    {
        /// <summary>
        /// The unique identifier of the level being edited.
        /// Set to -1 or 0 for new levels.
        /// </summary>
        public int LevelId { get; set; }

        /// <summary>
        /// The level's display name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The level's description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// JSON string containing the level data for the editor to load.
        /// This includes entities, terrain, spawn points, objectives, etc.
        /// </summary>
        public string LevelDataJson { get; set; }

        /// <summary>
        /// Whether this is a new level (true) or editing existing level (false).
        /// </summary>
        public bool IsNewLevel { get; set; }

        /// <summary>
        /// Creates a new LevelEditorData instance for a new level.
        /// </summary>
        public LevelEditorData()
        {
            LevelId = -1;
            Name = "New Level";
            Description = "A new level to be created";
            LevelDataJson = "{}"; // Empty JSON object for new levels
            IsNewLevel = true;
        }

        /// <summary>
        /// Creates a new LevelEditorData instance for editing an existing level.
        /// </summary>
        /// <param name="levelId">The ID of the level to edit.</param>
        /// <param name="name">The level's name.</param>
        /// <param name="description">The level's description.</param>
        /// <param name="levelDataJson">The JSON representation of the level data.</param>
        public LevelEditorData(int levelId, string name, string description, string levelDataJson)
        {
            LevelId = levelId;
            Name = name ?? "Unnamed Level";
            Description = description ?? "No description";
            LevelDataJson = levelDataJson ?? "{}";
            IsNewLevel = false;
        }
    }
}
