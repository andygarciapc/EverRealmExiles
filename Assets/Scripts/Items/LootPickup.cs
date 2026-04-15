using UnityEngine;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.Items
{
    /// <summary>
    /// World-space loot drop. Displays a billboard sprite and is picked up
    /// when the player aims at it and presses Interact.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class LootPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private SpriteRenderer _billboard;

        private ItemDefinition _item;
        private int _count;

        public string InteractPrompt =>
            _item != null
                ? $"Pick up {_item.DisplayName}" + (_count > 1 ? $" x{_count}" : "")
                : "";

        /// <summary>Initialize after instantiation.</summary>
        public void Init(ItemDefinition item, int count = 1)
        {
            _item = item;
            _count = count;

            if (_billboard != null && item.Icon != null)
                _billboard.sprite = item.Icon;

            gameObject.name = $"LootPickup_{item.DisplayName}";
        }

        public void Interact(Player.PlayerController player)
        {
            var inv = player.GetComponent<PlayerInventory>();
            if (inv == null) return;

            if (inv.TryAdd(_item, _count))
            {
                Core.AudioManager.Instance?.PlayLootPickup();
                Destroy(gameObject);
            }
        }

        private void LateUpdate()
        {
            // Billboard: face the camera so the sprite is always readable.
            if (_billboard != null && Camera.main != null)
                _billboard.transform.rotation = Camera.main.transform.rotation;
        }

        /// <summary>
        /// Factory: spawn a loot pickup at a position using the given prefab.
        /// </summary>
        public static LootPickup Spawn(ItemDefinition item, int count, Vector3 position, GameObject prefab)
        {
            var go = Instantiate(prefab, position + Vector3.up * 0.5f, Quaternion.identity);
            var pickup = go.GetComponent<LootPickup>();
            pickup.Init(item, count);
            return pickup;
        }
    }
}
