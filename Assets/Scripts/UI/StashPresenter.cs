using System;
using System.Collections.Generic;
using EverRealm.Exiles.Core;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Converts persistent stash state into presentation-ready <see cref="ItemViewData"/>.
    /// Subscribes to <see cref="StashManager.OnStashChanged"/> and fires
    /// <see cref="OnRefreshed"/> when data changes.
    /// </summary>
    public sealed class StashPresenter : IDisposable
    {
        private readonly StashManager _stash;
        private readonly List<ItemViewData> _viewData = new();

        /// <summary>Fired whenever the display data is rebuilt.</summary>
        public event Action OnRefreshed;

        /// <summary>Current display data. Safe to read at any time.</summary>
        public IReadOnlyList<ItemViewData> ViewData => _viewData;

        /// <summary>Total gold value of all stashed items.</summary>
        public int TotalValue { get; private set; }

        /// <summary>Number of non-empty slots.</summary>
        public int OccupiedSlots { get; private set; }

        /// <summary>Sum of all item counts across slots.</summary>
        public int TotalItemCount { get; private set; }

        public StashPresenter(StashManager stash)
        {
            _stash = stash ?? throw new ArgumentNullException(nameof(stash));

            _stash.OnStashChanged += Refresh;
            Refresh();
        }

        public void Dispose()
        {
            if (_stash != null)
                _stash.OnStashChanged -= Refresh;
        }

        /// <summary>Rebuild view data from current stash state.</summary>
        public void Refresh()
        {
            _viewData.Clear();
            TotalValue = 0;
            OccupiedSlots = 0;
            TotalItemCount = 0;

            foreach (var slot in _stash.Stash.Slots)
            {
                if (slot.IsEmpty) continue;

                var view = ItemViewData.FromStack(slot);
                _viewData.Add(view);
                TotalValue += view.Value * view.Count;
                TotalItemCount += view.Count;
                OccupiedSlots++;
            }

            OnRefreshed?.Invoke();
        }
    }
}
