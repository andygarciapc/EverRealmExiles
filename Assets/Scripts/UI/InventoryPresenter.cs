using System;
using System.Collections.Generic;
using EverRealm.Exiles.Items;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Converts runtime <see cref="Inventory"/> state into presentation-ready
    /// <see cref="ItemViewData"/>. Subscribes to inventory events and fires
    /// <see cref="OnRefreshed"/> when display data changes.
    /// </summary>
    public sealed class InventoryPresenter : IDisposable
    {
        private readonly Inventory _inventory;
        private readonly List<ItemViewData> _viewData = new();
        private readonly int _displaySlotCount;

        /// <summary>Fired whenever the display data is rebuilt.</summary>
        public event Action OnRefreshed;

        /// <summary>Current display data. Safe to read at any time.</summary>
        public IReadOnlyList<ItemViewData> ViewData => _viewData;

        /// <summary>Total gold value of all items currently held.</summary>
        public int TotalValue { get; private set; }

        /// <summary>Number of non-empty slots.</summary>
        public int OccupiedSlots { get; private set; }

        public InventoryPresenter(Inventory inventory, int displaySlotCount = 20)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _displaySlotCount = displaySlotCount;

            _inventory.OnChanged += Refresh;
            Refresh();
        }

        public void Dispose()
        {
            if (_inventory != null)
                _inventory.OnChanged -= Refresh;
        }

        /// <summary>Rebuild view data from current inventory state.</summary>
        public void Refresh()
        {
            _viewData.Clear();
            TotalValue = 0;
            OccupiedSlots = 0;

            var slots = _inventory.Slots;
            int count = Math.Max(slots.Count, _displaySlotCount);

            for (int i = 0; i < count; i++)
            {
                if (i < slots.Count && !slots[i].IsEmpty)
                {
                    var view = ItemViewData.FromStack(slots[i]);
                    _viewData.Add(view);
                    TotalValue += view.Value * view.Count;
                    OccupiedSlots++;
                }
                else
                {
                    _viewData.Add(ItemViewData.Empty);
                }
            }

            OnRefreshed?.Invoke();
        }
    }
}
