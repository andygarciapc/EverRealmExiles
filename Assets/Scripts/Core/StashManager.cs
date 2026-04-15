using System.Collections.Generic;
using UnityEngine;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.Extraction;
using EverRealm.Exiles.Items;

namespace EverRealm.Exiles.Core
{
    /// <summary>
    /// Owns the persistent stash inventory and save data.
    /// Lives on the GameBootstrap GameObject (DontDestroyOnLoad).
    /// Loads from disk once on first Awake; auto-saves on mutations.
    /// </summary>
    public sealed class StashManager : MonoBehaviour
    {
        [SerializeField] private ItemRegistry _itemRegistry;
        [SerializeField] private WeaponRegistry _weaponRegistry;
        [SerializeField] private BiomeRegistry _biomeRegistry;

        private Inventory _stash;
        private SaveData _data;
        private bool _initialized;

        public Inventory Stash => _stash;
        public SaveData Stats => _data;
        public ItemRegistry ItemRegistry => _itemRegistry;
        public WeaponRegistry WeaponRegistry => _weaponRegistry;
        public BiomeRegistry BiomeRegistry => _biomeRegistry;

        // ---------------------------------------------------------------------

        private void Awake()
        {
            if (_initialized) return;
            _initialized = true;

            _itemRegistry.Initialize();
            _weaponRegistry.Initialize();
            if (_biomeRegistry != null) _biomeRegistry.Initialize();

            _data = SaveManager.Load();
            _stash = new Inventory(100);

            // Rebuild stash inventory from saved item stacks.
            foreach (var saved in _data.StashItems)
            {
                var def = _itemRegistry.GetById(saved.ItemId);
                if (def == null) continue; // item was removed from the game
                _stash.Add(def, saved.Count);
            }

            Debug.Log($"[StashManager] Initialized — {_stash.SlotCount} stash slots, {_data.TotalRuns} total runs.");
        }

        // ---------------------------------------------------------------------
        // Run lifecycle hooks

        /// <summary>
        /// Record stats for a completed run (success or failure).
        /// Called by RunManager after EndRun.
        /// </summary>
        public void RecordRunEnd(RunResult result)
        {
            _data.TotalRuns++;
            _data.TotalKills += result.KillCount;
            _data.TotalPlayTime += result.ElapsedTime;

            if (result.Success)
                _data.TotalExtractions++;

            Save();
        }

        /// <summary>
        /// Transfer extracted items from the run inventory to the persistent stash.
        /// Called by RunManager on successful extraction.
        /// </summary>
        public void TransferRunItems(IReadOnlyList<ItemStack> items)
        {
            foreach (var stack in items)
            {
                if (stack.IsEmpty) continue;
                _stash.Add(stack.Definition, stack.Count);
            }

            Save();
            Debug.Log($"[StashManager] Transferred {items.Count} item stacks to stash.");
        }

        // ---------------------------------------------------------------------
        // Loadout

        /// <summary>
        /// Get the weapon selected for the next run.
        /// Returns null if none selected (caller should use default).
        /// </summary>
        public WeaponDefinition GetSelectedWeapon()
        {
            if (string.IsNullOrEmpty(_data.SelectedWeaponId))
                return null;

            return _weaponRegistry.GetById(_data.SelectedWeaponId);
        }

        /// <summary>
        /// Set the weapon for the next run by WeaponId. Persists immediately.
        /// </summary>
        public void SetSelectedWeapon(string weaponId)
        {
            _data.SelectedWeaponId = weaponId ?? "";
            Save();
        }

        // ---------------------------------------------------------------------
        // Biome selection

        /// <summary>
        /// Get the biome selected for the next run.
        /// Returns null if none selected (caller should use default settings).
        /// </summary>
        public BiomeDefinition GetSelectedBiome()
        {
            if (_biomeRegistry == null || string.IsNullOrEmpty(_data.SelectedBiomeId))
                return null;

            return _biomeRegistry.GetById(_data.SelectedBiomeId);
        }

        /// <summary>
        /// Set the biome for the next run by BiomeId. Persists immediately.
        /// </summary>
        public void SetSelectedBiome(string biomeId)
        {
            _data.SelectedBiomeId = biomeId ?? "";
            Save();
        }

        // ---------------------------------------------------------------------
        // Player profile

        public string PlayerName => _data.PlayerName;
        public int PlayerLevel => _data.PlayerLevel;
        public int Currency => _data.Currency;

        // ---------------------------------------------------------------------
        // Persistence

        /// <summary>
        /// Convert runtime stash to saved format and write to disk.
        /// </summary>
        public void Save()
        {
            _data.StashItems.Clear();

            foreach (var slot in _stash.Slots)
            {
                if (slot.IsEmpty || slot.Definition == null) continue;
                _data.StashItems.Add(new SavedItemStack(slot.Definition.ItemId, slot.Count));
            }

            SaveManager.Save(_data);
        }
    }
}
