using UnityEngine;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.Items;

namespace EverRealm.Exiles.World
{
    /// <summary>
    /// Block entity for chest blocks. Holds a loot table reference,
    /// rolls loot on first interaction, and spawns pickups.
    /// </summary>
    public sealed class ChestBlockEntity : IBlockEntity
    {
        public Vector3Int Position { get; }

        private readonly LootTable _lootTable;
        private readonly int _rollCount;
        private readonly GameObject _lootPickupPrefab;
        private bool _opened;

        public string InteractPrompt => _opened ? "Empty" : "Open Chest";

        public ChestBlockEntity(Vector3Int position, LootTable lootTable, int rollCount, GameObject lootPickupPrefab)
        {
            Position = position;
            _lootTable = lootTable;
            _rollCount = rollCount;
            _lootPickupPrefab = lootPickupPrefab;
        }

        public void OnInteract(Player.PlayerController player)
        {
            if (_opened) return;
            _opened = true;

            var inv = player.GetComponent<PlayerInventory>();

            for (int i = 0; i < _rollCount; i++)
            {
                var item = _lootTable.Roll(out int count);
                if (item == null) continue;

                if (inv != null && inv.TryAdd(item, count))
                    continue;

                // Inventory full or missing — spill as world pickup.
                if (_lootPickupPrefab != null)
                {
                    Vector3 spawnPos = new Vector3(Position.x + 0.5f, Position.y + 1.2f, Position.z + 0.5f);
                    Vector3 offset = Random.insideUnitSphere * 0.8f;
                    offset.y = Mathf.Abs(offset.y);
                    LootPickup.Spawn(item, count, spawnPos + offset, _lootPickupPrefab);
                }
            }

            Debug.Log($"[ChestBlockEntity] Opened chest at {Position}");
        }

        public void OnRemoved() { }
    }
}
