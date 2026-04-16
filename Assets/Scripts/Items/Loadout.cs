using System;
using System.Collections.Generic;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.Items
{
    /// <summary>
    /// Represents the player's raid loadout: named equipment slots (armor, weapons)
    /// and a capacity-limited backpack for consumables/materials.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public sealed class Loadout
    {
        /// <summary>Fired whenever any equipment slot or backpack changes.</summary>
        public event Action OnChanged;

        private readonly Dictionary<EquipSlot, ItemStack> _equipment = new();
        private readonly Inventory _backpack;

        public IReadOnlyDictionary<EquipSlot, ItemStack> Equipment => _equipment;
        public Inventory Backpack => _backpack;

        public Loadout(int backpackCapacity = 12)
        {
            _backpack = new Inventory(backpackCapacity, maxSlots: backpackCapacity);
            _backpack.OnChanged += () => OnChanged?.Invoke();
        }

        /// <summary>
        /// Equip an item into its matching slot.
        /// Returns the previously equipped item (empty if slot was vacant).
        /// Returns the input stack unchanged if the item is not equippable.
        /// </summary>
        public ItemStack TryEquip(ItemStack stack)
        {
            if (stack.IsEmpty || stack.Definition == null)
                return default;

            var slot = stack.Definition.EquipSlot;
            if (slot == EquipSlot.None)
                return stack; // not equippable

            var displaced = GetEquipped(slot);
            _equipment[slot] = stack;
            OnChanged?.Invoke();
            return displaced;
        }

        /// <summary>
        /// Remove and return the item in the given slot.
        /// Returns empty if nothing was equipped.
        /// </summary>
        public ItemStack Unequip(EquipSlot slot)
        {
            if (slot == EquipSlot.None) return default;

            if (_equipment.TryGetValue(slot, out var stack))
            {
                _equipment.Remove(slot);
                OnChanged?.Invoke();
                return stack;
            }

            return default;
        }

        /// <summary>Get the item currently in a slot. Returns empty if vacant.</summary>
        public ItemStack GetEquipped(EquipSlot slot)
        {
            if (slot != EquipSlot.None && _equipment.TryGetValue(slot, out var stack))
                return stack;

            return default;
        }

        /// <summary>Whether the given slot has an item equipped.</summary>
        public bool IsSlotOccupied(EquipSlot slot)
        {
            return slot != EquipSlot.None
                && _equipment.TryGetValue(slot, out var stack)
                && !stack.IsEmpty;
        }

        /// <summary>Sum of DefenseValue across all equipped armor pieces.</summary>
        public float TotalDefense
        {
            get
            {
                float total = 0f;
                foreach (var kvp in _equipment)
                {
                    if (!kvp.Value.IsEmpty && kvp.Value.Definition != null)
                        total += kvp.Value.Definition.DefenseValue;
                }
                return total;
            }
        }

        /// <summary>Clear all equipment and backpack contents.</summary>
        public void Clear()
        {
            bool hadItems = _equipment.Count > 0 || _backpack.SlotCount > 0;
            _equipment.Clear();
            _backpack.Clear();
            if (hadItems) OnChanged?.Invoke();
        }

        /// <summary>Clear only backpack, keep equipment.</summary>
        public void ClearBackpack()
        {
            _backpack.Clear();
        }
    }
}
