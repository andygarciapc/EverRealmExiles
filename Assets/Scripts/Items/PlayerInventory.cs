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

        /// <summary>Fired after a pickup is successfully added.</summary>
        public event System.Action<ItemStack> OnPickedUp;

        private void Awake()
        {
            _inventory = new Inventory();
            _inventory.OnItemAdded += stack =>
            {
                OnPickedUp?.Invoke(stack);
                Debug.Log($"[Inventory] +{stack.Count} {stack.Definition.DisplayName}");
            };
        }

        /// <summary>
        /// Try to add an item. Returns true if the full amount was added.
        /// </summary>
        public bool TryAdd(ItemDefinition def, int count = 1)
        {
            int overflow = _inventory.Add(def, count);
            return overflow == 0;
        }
    }
}
