using System;
using System.IO;
using System.Text.Json;

namespace DinoGrr.Core.Database
{
    /// <summary>
    /// Helper class for JSON serialization operations.
    /// Provides safe save/load operations with error handling.
    /// Note: Renamed from BinarySerializer to JsonSerializer for modern .NET compatibility.
    /// </summary>
    public static class BinarySerializer
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Serializes an object to a JSON file.
        /// </summary>
        /// <typeparam name="T">Type of object to serialize.</typeparam>
        /// <param name="data">The object to serialize.</param>
        /// <param name="filePath">Full path to the output file.</param>
        /// <exception cref="InvalidOperationException">Thrown when serialization fails.</exception>
        public static void Save<T>(T data, string filePath) where T : class
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    JsonSerializer.Serialize(fileStream, data, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save data to {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deserializes an object from a JSON file.
        /// </summary>
        /// <typeparam name="T">Type of object to deserialize.</typeparam>
        /// <param name="filePath">Full path to the input file.</param>
        /// <returns>The deserialized object, or null if the file doesn't exist.</returns>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
        public static T Load<T>(string filePath) where T : class
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                return null;

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    return JsonSerializer.Deserialize<T>(fileStream, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load data from {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Safely loads an object with a fallback value if loading fails.
        /// </summary>
        /// <typeparam name="T">Type of object to deserialize.</typeparam>
        /// <param name="filePath">Full path to the input file.</param>
        /// <param name="fallbackFactory">Function to create a fallback object if loading fails.</param>
        /// <returns>The deserialized object or the fallback.</returns>
        public static T LoadOrDefault<T>(string filePath, Func<T> fallbackFactory) where T : class
        {
            try
            {
                var result = Load<T>(filePath);
                return result ?? fallbackFactory();
            }
            catch
            {
                return fallbackFactory();
            }
        }
    }
}
