using System.Collections.Generic;
using UnityEngine;

namespace EverRealm.Exiles.Data
{
    /// <summary>
    /// Central registry of all item definitions. Provides O(1) lookup by ItemId.
    /// Create via Assets > Create > EverRealm > Item Registry.
    /// </summary>
    [CreateAssetMenu(menuName = "EverRealm/Item Registry", fileName = "ItemRegistry")]
    public sealed class ItemRegistry : ScriptableObject
    {
        [SerializeField] private ItemDefinition[] _items;

        private Dictionary<string, ItemDefinition> _lookup;

        public IReadOnlyList<ItemDefinition> All => _items;

        /// <summary>Build the lookup dictionary. Called automatically on first query.</summary>
        public void Initialize()
        {
            _lookup = new Dictionary<string, ItemDefinition>();
            if (_items == null) return;

            foreach (var item in _items)
            {
                if (item == null || string.IsNullOrEmpty(item.ItemId)) continue;

                if (!_lookup.TryAdd(item.ItemId, item))
                    Debug.LogWarning($"[ItemRegistry] Duplicate ItemId '{item.ItemId}' — skipping.");
            }
        }

        /// <summary>
        /// Look up an item by its stable ItemId. Returns null with a warning if not found.
        /// </summary>
        public ItemDefinition GetById(string itemId)
        {
            if (_lookup == null) Initialize();

            if (string.IsNullOrEmpty(itemId)) return null;

            if (_lookup.TryGetValue(itemId, out var def))
                return def;

            Debug.LogWarning($"[ItemRegistry] No item found with id '{itemId}'.");
            return null;
        }
    }
}
