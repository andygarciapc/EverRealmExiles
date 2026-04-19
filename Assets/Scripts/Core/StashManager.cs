using System;
using System.Collections.Generic;
using UnityEngine;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.Extraction;
using EverRealm.Exiles.Items;

namespace EverRealm.Exiles.Core
{
    /// <summary>
    /// Owns the persistent stash inventory, equipment loadout, and save data.
    /// Lives on the GameBootstrap GameObject (DontDestroyOnLoad).
    /// Loads from disk once on first Awake; auto-saves on mutations.
    /// Fires <see cref="OnStashChanged"/> and <see cref="OnLoadoutChanged"/>
    /// whenever their respective contents change.
    /// </summary>
    public sealed class StashManager : MonoBehaviour
    {
        [SerializeField] private ItemRegistry _itemRegistry;
        [SerializeField] private WeaponRegistry _weaponRegistry;
        [SerializeField] private BiomeRegistry _biomeRegistry;

        [Tooltip("Items seeded into the stash for brand-new saves (first launch only).")]
        [SerializeField] private ItemDefinition[] _starterItems;

        private Inventory _stash;
        private Loadout _loadout;
        private SaveData _data;
        private bool _initialized;

        public Inventory Stash => _stash;
        public Loadout Loadout => _loadout;
        public SaveData Stats => _data;
        public ItemRegistry ItemRegistry => _itemRegistry;
        public WeaponRegistry WeaponRegistry => _weaponRegistry;
        public BiomeRegistry BiomeRegistry => _biomeRegistry;

        /// <summary>Fired when stash contents change (transfer, add, remove).</summary>
        public event Action OnStashChanged;

        /// <summary>Fired when loadout changes (equip, unequip, backpack mutation).</summary>
        public event Action OnLoadoutChanged;

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
            _loadout = new Loadout(12);

            // Seed starter items on first-ever launch.
            bool isFreshSave = _data.TotalRuns == 0
                            && _data.StashItems.Count == 0
                            && _data.EquippedItems.Count == 0;

            if (isFreshSave && _starterItems != null && _starterItems.Length > 0)
            {
                foreach (var item in _starterItems)
                {
                    if (item == null) continue;
                    _data.StashItems.Add(new SavedItemStack(item.ItemId, 1));
                }
                SaveManager.Save(_data);
                Debug.Log($"[StashManager] Seeded {_starterItems.Length} starter items for new player.");
            }

            // Rebuild stash inventory from saved item stacks.
            foreach (var saved in _data.StashItems)
            {
                var def = _itemRegistry.GetById(saved.ItemId);
                if (def == null)
                {
                    Debug.LogWarning($"[StashManager] Skipping unknown stash item '{saved.ItemId}' during load.");
                    continue;
                }
                _stash.Add(def, saved.Count);
            }

            // Rebuild equipment from saved loadout.
            foreach (var saved in _data.EquippedItems)
            {
                var def = _itemRegistry.GetById(saved.ItemId);
                if (def == null)
                {
                    Debug.LogWarning($"[StashManager] Skipping unknown equipped item '{saved.ItemId}' during load.");
                    continue;
                }

                if (!System.Enum.TryParse<EquipSlot>(saved.SlotName, out var slot))
                {
                    Debug.LogWarning($"[StashManager] Skipping unknown equip slot '{saved.SlotName}' during load.");
                    continue;
                }

                _loadout.TryEquip(new ItemStack(def, saved.Count));
            }

            // Rebuild backpack from saved data.
            foreach (var saved in _data.BackpackItems)
            {
                var def = _itemRegistry.GetById(saved.ItemId);
                if (def == null)
                {
                    Debug.LogWarning($"[StashManager] Skipping unknown backpack item '{saved.ItemId}' during load.");
                    continue;
                }
                _loadout.Backpack.Add(def, saved.Count);
            }

            // Subscribe to loadout changes for auto-save.
            _loadout.OnChanged += OnLoadoutMutated;

            Debug.Log($"[StashManager] Initialized — {_stash.SlotCount} stash slots, " +
                      $"{_data.EquippedItems.Count} equipped, {_data.BackpackItems.Count} backpack, " +
                      $"{_data.TotalRuns} total runs.");
        }

        private void OnLoadoutMutated()
        {
            Save();
            OnLoadoutChanged?.Invoke();
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
            int transferred = 0;

            foreach (var stack in items)
            {
                if (stack.IsEmpty) continue;

                if (stack.Definition == null)
                {
                    Debug.LogWarning("[StashManager] Skipping null-definition item during transfer.");
                    continue;
                }

                _stash.Add(stack.Definition, stack.Count);
                transferred++;
            }

            Save();
            OnStashChanged?.Invoke();
            Debug.Log($"[StashManager] Transferred {transferred} item stacks to stash.");
        }

        // ---------------------------------------------------------------------
        // Loadout — equip / unequip

        /// <summary>
        /// Move an item from stash to loadout. Equippable items go to their
        /// equipment slot (swapping any existing item back to stash).
        /// Non-equippable items go to the backpack.
        /// Returns true if the item was successfully moved.
        /// </summary>
        public bool EquipFromStash(string itemId)
        {
            var def = _itemRegistry.GetById(itemId);
            if (def == null) return false;

            int removed = _stash.Remove(def, 1);
            if (removed == 0) return false;

            var stack = new ItemStack(def, 1);

            if (def.EquipSlot != EquipSlot.None)
            {
                var displaced = _loadout.TryEquip(stack);
                if (!displaced.IsEmpty && displaced.Definition != null)
                    _stash.Add(displaced.Definition, displaced.Count);
            }
            else
            {
                int overflow = _loadout.Backpack.Add(def, 1);
                if (overflow > 0)
                {
                    // Backpack full — return item to stash.
                    _stash.Add(def, 1);
                    return false;
                }
            }

            Save();
            OnStashChanged?.Invoke();
            OnLoadoutChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Move an equipped item from a loadout slot back to stash.
        /// Returns true if an item was moved.
        /// </summary>
        public bool UnequipToStash(EquipSlot slot)
        {
            var item = _loadout.Unequip(slot);
            if (item.IsEmpty || item.Definition == null) return false;

            _stash.Add(item.Definition, item.Count);
            Save();
            OnStashChanged?.Invoke();
            OnLoadoutChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Equip an item that currently lives in the player's run inventory
        /// (used mid-run when the persistent stash isn't accessible).
        /// Non-equippable items are rejected. If the destination slot is
        /// occupied, the displaced item swaps back into the run inventory.
        /// </summary>
        public bool EquipFromRunInventory(string itemId, Inventory runInventory)
        {
            if (runInventory == null) return false;

            var def = _itemRegistry.GetById(itemId);
            if (def == null || def.EquipSlot == EquipSlot.None) return false;

            int removed = runInventory.Remove(def, 1);
            if (removed == 0) return false;

            var displaced = _loadout.TryEquip(new ItemStack(def, 1));
            if (!displaced.IsEmpty && displaced.Definition != null)
                runInventory.Add(displaced.Definition, displaced.Count);

            Save();
            OnLoadoutChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Unequip a loadout slot into the player's run inventory (used mid-run
        /// when the persistent stash isn't accessible). On success the item
        /// leaves the loadout and lands in the run bag where it behaves like
        /// any other picked-up loot — extracted or lost on death.
        /// Fails and re-equips if the run inventory has no room.
        /// </summary>
        public bool UnequipToRunInventory(EquipSlot slot, Inventory runInventory)
        {
            if (runInventory == null) return false;

            var item = _loadout.Unequip(slot);
            if (item.IsEmpty || item.Definition == null) return false;

            int overflow = runInventory.Add(item.Definition, item.Count);
            if (overflow >= item.Count)
            {
                // Run inventory had no room at all — put it back.
                _loadout.TryEquip(new ItemStack(item.Definition, item.Count));
                return false;
            }

            Save();
            OnLoadoutChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Move an item from the backpack back to stash.
        /// Returns true if an item was moved.
        /// </summary>
        public bool RemoveFromBackpack(string itemId)
        {
            var def = _itemRegistry.GetById(itemId);
            if (def == null) return false;

            int removed = _loadout.Backpack.Remove(def, 1);
            if (removed == 0) return false;

            _stash.Add(def, 1);
            Save();
            OnStashChanged?.Invoke();
            OnLoadoutChanged?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------------
        // Weapon selection (reads from loadout, falls back to legacy)

        /// <summary>
        /// Get the weapon selected for the next run.
        /// Reads from the equipped primary weapon first, then falls
        /// back to the legacy SelectedWeaponId for backward compatibility.
        /// Returns null if none selected (caller should use default).
        /// </summary>
        public WeaponDefinition GetSelectedWeapon()
        {
            // New path: read from loadout equipment.
            var primary = _loadout.GetEquipped(EquipSlot.PrimaryWeapon);
            if (!primary.IsEmpty && primary.Definition != null && primary.Definition.LinkedWeapon != null)
                return primary.Definition.LinkedWeapon;

            // Legacy fallback.
            if (!string.IsNullOrEmpty(_data.SelectedWeaponId))
                return _weaponRegistry.GetById(_data.SelectedWeaponId);

            return null;
        }

        /// <summary>
        /// Get the secondary weapon for the next run (if equipped).
        /// Returns null if no secondary weapon is equipped.
        /// </summary>
        public WeaponDefinition GetSecondaryWeapon()
        {
            var secondary = _loadout.GetEquipped(EquipSlot.SecondaryWeapon);
            if (!secondary.IsEmpty && secondary.Definition != null && secondary.Definition.LinkedWeapon != null)
                return secondary.Definition.LinkedWeapon;

            return null;
        }

        /// <summary>
        /// Set the weapon for the next run by WeaponId. Persists immediately.
        /// Legacy path — prefer equipping via EquipFromStash.
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
        /// Convert runtime stash and loadout to saved format and write to disk.
        /// </summary>
        public void Save()
        {
            // Stash.
            _data.StashItems.Clear();
            foreach (var slot in _stash.Slots)
            {
                if (slot.IsEmpty || slot.Definition == null) continue;
                _data.StashItems.Add(new SavedItemStack(slot.Definition.ItemId, slot.Count));
            }

            // Equipment.
            _data.EquippedItems.Clear();
            foreach (var kvp in _loadout.Equipment)
            {
                if (kvp.Value.IsEmpty || kvp.Value.Definition == null) continue;
                _data.EquippedItems.Add(new SavedEquipSlot(
                    kvp.Key.ToString(),
                    kvp.Value.Definition.ItemId,
                    kvp.Value.Count
                ));
            }

            // Backpack.
            _data.BackpackItems.Clear();
            foreach (var slot in _loadout.Backpack.Slots)
            {
                if (slot.IsEmpty || slot.Definition == null) continue;
                _data.BackpackItems.Add(new SavedItemStack(slot.Definition.ItemId, slot.Count));
            }

            SaveManager.Save(_data);
        }
    }
}
