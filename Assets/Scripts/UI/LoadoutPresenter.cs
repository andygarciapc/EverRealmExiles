using System;
using System.Collections.Generic;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Converts the player's <see cref="Loadout"/> state into presentation-ready
    /// <see cref="ItemViewData"/> for the UI. Subscribes to
    /// <see cref="StashManager.OnLoadoutChanged"/> and fires
    /// <see cref="OnRefreshed"/> when data changes.
    /// </summary>
    public sealed class LoadoutPresenter : IDisposable
    {
        private readonly StashManager _stash;
        private readonly Dictionary<EquipSlot, ItemViewData> _equipmentData = new();
        private readonly List<ItemViewData> _backpackData = new();

        /// <summary>Fired whenever the display data is rebuilt.</summary>
        public event Action OnRefreshed;

        /// <summary>Equipment slots mapped to their display data.</summary>
        public IReadOnlyDictionary<EquipSlot, ItemViewData> EquipmentData => _equipmentData;

        /// <summary>Backpack slot display data.</summary>
        public IReadOnlyList<ItemViewData> BackpackData => _backpackData;

        /// <summary>Number of occupied backpack slots.</summary>
        public int BackpackUsed { get; private set; }

        /// <summary>Maximum backpack slots.</summary>
        public int BackpackCapacity { get; private set; }

        /// <summary>Total defense from all equipped armor.</summary>
        public float TotalDefense { get; private set; }

        public LoadoutPresenter(StashManager stash)
        {
            _stash = stash ?? throw new ArgumentNullException(nameof(stash));
            _stash.OnLoadoutChanged += Refresh;
            Refresh();
        }

        public void Dispose()
        {
            if (_stash != null)
                _stash.OnLoadoutChanged -= Refresh;
        }

        /// <summary>Rebuild view data from current loadout state.</summary>
        public void Refresh()
        {
            _equipmentData.Clear();
            _backpackData.Clear();
            TotalDefense = 0f;
            BackpackUsed = 0;

            var loadout = _stash.Loadout;
            BackpackCapacity = loadout.Backpack.MaxSlots > 0
                ? loadout.Backpack.MaxSlots
                : 12;

            // Equipment slots.
            foreach (var kvp in loadout.Equipment)
            {
                if (kvp.Value.IsEmpty) continue;
                var view = ItemViewData.FromStack(kvp.Value);
                _equipmentData[kvp.Key] = view;
                TotalDefense += view.DefenseValue;
            }

            // Backpack.
            foreach (var slot in loadout.Backpack.Slots)
            {
                if (slot.IsEmpty) continue;
                _backpackData.Add(ItemViewData.FromStack(slot));
                BackpackUsed++;
            }

            OnRefreshed?.Invoke();
        }
    }
}
