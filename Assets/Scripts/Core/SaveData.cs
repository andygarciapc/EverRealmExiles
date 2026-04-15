using System.Collections.Generic;

namespace EverRealm.Exiles.Core
{
    /// <summary>
    /// JSON-serializable save data persisted between sessions.
    /// Contains the player's stash, selected loadout, and lifetime stats.
    /// </summary>
    [System.Serializable]
    public sealed class SaveData
    {
        /// <summary>Items stored in the persistent stash.</summary>
        public List<SavedItemStack> StashItems = new();

        /// <summary>WeaponId of the weapon chosen for the next run.</summary>
        public string SelectedWeaponId = "";

        /// <summary>BiomeId of the biome chosen for the next run.</summary>
        public string SelectedBiomeId = "";

        // ----- Player profile -----
        public string PlayerName = "Exile";
        public int PlayerLevel = 1;
        public int Currency;

        // ----- Lifetime statistics -----
        public int TotalRuns;
        public int TotalExtractions;
        public int TotalKills;
        public float TotalPlayTime;
    }

    /// <summary>
    /// Serialization-friendly mirror of ItemStack.
    /// References items by their stable ItemId string instead of an asset reference.
    /// </summary>
    [System.Serializable]
    public struct SavedItemStack
    {
        public string ItemId;
        public int Count;

        public SavedItemStack(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }
}
