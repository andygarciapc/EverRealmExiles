using UnityEngine;

namespace EverRealm.Exiles.Data
{
    /// <summary>
    /// Tunable parameters for POI generation. Read-only at runtime.
    /// Values are captured by <see cref="World.WorldGenerator"/> at construction
    /// so background-thread generation never touches the SO directly.
    /// </summary>
    [CreateAssetMenu(fileName = "POISettings", menuName = "EverRealm/POI Settings")]
    public sealed class POISettings : ScriptableObject
    {
        [Header("Enemy Camps")]
        [Tooltip("Grid spacing in blocks between potential enemy camp locations.")]
        public int EnemyCampSpacing = 50;
        [Range(0f, 1f)]
        [Tooltip("Hash threshold — lower means fewer camps.")]
        public float EnemyCampThreshold = 0.40f;
        public int EnemyCampSeedOffset = 2000;

        [Header("Treasure Caches")]
        [Tooltip("Grid spacing in blocks between potential cache locations.")]
        public int TreasureCacheSpacing = 60;
        [Range(0f, 1f)]
        [Tooltip("Hash threshold — lower means fewer caches.")]
        public float TreasureCacheThreshold = 0.30f;
        public int TreasureCacheSeedOffset = 3000;

        [Header("Enemy Counts by Zone")]
        public int SafeZoneEnemies   = 0;
        public int MediumZoneEnemies = 2;
        public int HighZoneEnemies   = 4;

        [Header("Loot Rolls by Zone")]
        public int SafeZoneLootRolls  = 2;
        public int MediumZoneLootRolls = 3;
        public int HighZoneLootRolls   = 5;
    }
}
