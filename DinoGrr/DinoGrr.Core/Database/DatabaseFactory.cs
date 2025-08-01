using System;
using System.IO;

namespace DinoGrr.Core.Database
{
    /// <summary>
    /// Factory for creating database connections to binary files.
    /// Acts as a central configuration point for all database operations.
    /// </summary>
    public class DatabaseFactory
    {
        private readonly string _basePath;

        /// <summary>
        /// Creates a new DatabaseFactory instance.
        /// </summary>
        public DatabaseFactory()
        {
            // Use platform-appropriate application data folder
            _basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DinoGrr",
                "Data"
            );

            // Ensure the directory exists
            Directory.CreateDirectory(_basePath);
        }

        /// <summary>
        /// Gets the full path for a JSON database file.
        /// </summary>
        /// <param name="fileName">The name of the database file (without extension).</param>
        /// <returns>Full path to the database file.</returns>
        public string GetDatabasePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            return Path.Combine(_basePath, $"{fileName}.json");
        }

        /// <summary>
        /// Checks if a database file exists.
        /// </summary>
        /// <param name="fileName">The name of the database file (without extension).</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        public bool DatabaseExists(string fileName)
        {
            return File.Exists(GetDatabasePath(fileName));
        }

        /// <summary>
        /// Deletes a database file if it exists.
        /// </summary>
        /// <param name="fileName">The name of the database file (without extension).</param>
        /// <returns>True if the file was deleted, false if it didn't exist.</returns>
        public bool DeleteDatabase(string fileName)
        {
            string path = GetDatabasePath(fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the base data directory path.
        /// </summary>
        public string BasePath => _basePath;
    }
}
