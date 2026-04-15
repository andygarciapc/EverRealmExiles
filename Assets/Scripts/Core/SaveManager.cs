using System;
using System.IO;
using UnityEngine;

namespace EverRealm.Exiles.Core
{
    /// <summary>
    /// Static helper that reads and writes <see cref="SaveData"/> to disk
    /// as JSON via <see cref="JsonUtility"/>.
    /// </summary>
    public static class SaveManager
    {
        private const string FileName = "save.json";

        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>
        /// Serialize and write save data to disk.
        /// </summary>
        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(FilePath, json);
                Debug.Log($"[SaveManager] Saved to {FilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
            }
        }

        /// <summary>
        /// Load save data from disk. Returns a fresh <see cref="SaveData"/>
        /// if the file does not exist or cannot be read.
        /// </summary>
        public static SaveData Load()
        {
            if (!File.Exists(FilePath))
            {
                Debug.Log("[SaveManager] No save file found — starting fresh.");
                return new SaveData();
            }

            try
            {
                string json = File.ReadAllText(FilePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("[SaveManager] Save loaded successfully.");
                return data ?? new SaveData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load: {e.Message}");
                return new SaveData();
            }
        }

        /// <summary>True if a save file exists on disk.</summary>
        public static bool Exists() => File.Exists(FilePath);

        /// <summary>Delete the save file. For debug/testing.</summary>
        public static void DeleteSave()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
                Debug.Log("[SaveManager] Save file deleted.");
            }
        }
    }
}
