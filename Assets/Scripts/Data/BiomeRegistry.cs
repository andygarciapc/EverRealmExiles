using System.Collections.Generic;
using UnityEngine;

namespace EverRealm.Exiles.Data
{
    /// <summary>
    /// Central registry of all biome definitions. Provides O(1) lookup by BiomeId.
    /// Create via Assets > Create > EverRealm > Biome Registry.
    /// </summary>
    [CreateAssetMenu(menuName = "EverRealm/Biome Registry", fileName = "BiomeRegistry")]
    public sealed class BiomeRegistry : ScriptableObject
    {
        [SerializeField] private BiomeDefinition[] _biomes;

        private Dictionary<string, BiomeDefinition> _lookup;

        public IReadOnlyList<BiomeDefinition> All => _biomes;

        /// <summary>Build the lookup dictionary. Called automatically on first query.</summary>
        public void Initialize()
        {
            _lookup = new Dictionary<string, BiomeDefinition>();
            if (_biomes == null) return;

            foreach (var biome in _biomes)
            {
                if (biome == null || string.IsNullOrEmpty(biome.BiomeId)) continue;

                if (!_lookup.TryAdd(biome.BiomeId, biome))
                    Debug.LogWarning($"[BiomeRegistry] Duplicate BiomeId '{biome.BiomeId}' — skipping.");
            }
        }

        /// <summary>
        /// Look up a biome by its stable BiomeId. Returns null with a warning if not found.
        /// </summary>
        public BiomeDefinition GetById(string biomeId)
        {
            if (_lookup == null) Initialize();

            if (string.IsNullOrEmpty(biomeId)) return null;

            if (_lookup.TryGetValue(biomeId, out var def))
                return def;

            Debug.LogWarning($"[BiomeRegistry] No biome found with id '{biomeId}'.");
            return null;
        }
    }
}
