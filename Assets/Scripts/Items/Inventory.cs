using System;
using System.Collections.Generic;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.Items
{
    /// <summary>
    /// Pure C# inventory with stacking, events, and no MonoBehaviour dependency.
    /// </summary>
    public sealed class Inventory
    {
        public event Action<ItemStack> OnItemAdded;
        public event Action<ItemStack> OnItemRemoved;
        public event Action OnChanged;

        private readonly List<ItemStack> _slots;
        public IReadOnlyList<ItemStack> Slots => _slots;
        public int SlotCount => _slots.Count;

        /// <summary>Maximum slot count. 0 = unlimited.</summary>
        public int MaxSlots { get; }

        public Inventory(int initialCapacity = 20, int maxSlots = 0)
        {
            _slots = new List<ItemStack>(initialCapacity);
            MaxSlots = maxSlots;
        }

        /// <summary>
        /// Add items. Stacks onto existing matching slots first, then creates new ones.
        /// Returns the number of items that could NOT be added (0 = full success).
        /// </summary>
        public int Add(ItemDefinition def, int count = 1)
        {
            if (def == null || count <= 0) return count;

            int remaining = count;

            if (def.Stackable)
            {
                int max = def.MaxStack > 0 ? def.MaxStack : 1;

                // Fill existing stacks first.
                for (int i = 0; i < _slots.Count && remaining > 0; i++)
                {
                    if (_slots[i].Definition != def) continue;
                    int space = max - _slots[i].Count;
                    if (space <= 0) continue;

                    int toAdd = remaining < space ? remaining : space;
                    var slot = _slots[i];
                    slot.Count += toAdd;
                    _slots[i] = slot;
                    remaining -= toAdd;
                }

                // Create new stacks for the remainder.
                while (remaining > 0)
                {
                    if (MaxSlots > 0 && _slots.Count >= MaxSlots) break;
                    int toAdd = remaining < max ? remaining : max;
                    _slots.Add(new ItemStack(def, toAdd));
                    remaining -= toAdd;
                }
            }
            else
            {
                // Non-stackable: one item per slot.
                for (int i = 0; i < count; i++)
                {
                    if (MaxSlots > 0 && _slots.Count >= MaxSlots) break;
                    _slots.Add(new ItemStack(def, 1));
                    remaining--;
                }
            }

            int added = count - remaining;
            if (added > 0)
            {
                OnItemAdded?.Invoke(new ItemStack(def, added));
                OnChanged?.Invoke();
            }

            return remaining;
        }

        /// <summary>
        /// Remove up to <paramref name="count"/> of the given item.
        /// Returns the actual number removed.
        /// </summary>
        public int Remove(ItemDefinition def, int count = 1)
        {
            if (def == null || count <= 0) return 0;

            int remaining = count;

            for (int i = _slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (_slots[i].Definition != def) continue;

                int available = _slots[i].Count;
                if (available <= remaining)
                {
                    remaining -= available;
                    _slots.RemoveAt(i);
                }
                else
                {
                    var slot = _slots[i];
                    slot.Count -= remaining;
                    _slots[i] = slot;
                    remaining = 0;
                }
            }

            int removed = count - remaining;
            if (removed > 0)
            {
                OnItemRemoved?.Invoke(new ItemStack(def, removed));
                OnChanged?.Invoke();
            }

            return removed;
        }

        /// <summary>Total count of a given item across all slots.</summary>
        public int CountOf(ItemDefinition def)
        {
            int total = 0;
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i].Definition == def)
                    total += _slots[i].Count;
            return total;
        }

        public void Clear()
        {
            if (_slots.Count == 0) return;
            _slots.Clear();
            OnChanged?.Invoke();
        }
    }
}
