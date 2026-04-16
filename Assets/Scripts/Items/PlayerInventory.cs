using System;
using UnityEngine;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.Items
{
    /// <summary>
    /// Thin MonoBehaviour that owns the player's <see cref="Inventory"/> instance.
    /// Attach to the Player GameObject.
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        private Inventory _inventory;

        /// <summary>The underlying inventory. Use for read access and event subscription.</summary>
        public Inventory Inventory => _inventory;

        /// <summary>Fired after a pickup is successfully added (for HUD notifications).</summary>
        public event Action<ItemStack> OnPickedUp;

        /// <summary>Fired when inventory contents change for any reason.</summary>
        public event Action OnInventoryChanged;

        private void Awake()
        {
            _inventory = new Inventory();
            _inventory.OnItemAdded += OnItemAdded;
            _inventory.OnChanged += HandleChanged;
        }

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.OnItemAdded -= OnItemAdded;
                _inventory.OnChanged -= HandleChanged;
            }
        }

        private void OnItemAdded(ItemStack stack)
        {
            OnPickedUp?.Invoke(stack);

            string name = stack.Definition != null ? stack.Definition.DisplayName : "???";
            Debug.Log($"[Inventory] +{stack.Count} {name}");
        }

        private void HandleChanged()
        {
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Try to add an item. Returns true if the full amount was added.
        /// </summary>
        public bool TryAdd(ItemDefinition def, int count = 1)
        {
            if (def == null)
            {
                Debug.LogWarning("[PlayerInventory] Attempted to add null item definition.");
                return false;
            }

            int overflow = _inventory.Add(def, count);
            return overflow == 0;
        }
    }
}
