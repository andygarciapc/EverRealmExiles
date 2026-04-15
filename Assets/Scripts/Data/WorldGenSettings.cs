using UnityEngine;

namespace EverRealm.Exiles.Data
{
    /// <summary>
    /// Tunable parameters for world generation. Assign in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldGenSettings", menuName = "EverRealm/World Gen Settings")]
    public class WorldGenSettings : ScriptableObject
    {
        [Header("Seed")]
        public int Seed = 42;

        [Header("Map Size")]
        [Tooltip("Half-width/depth in chunks — full map is (2*radius+1)^2 chunks.")]
        public int ChunkRadius = 8; // 17×17 chunk map = 272×272 blocks

        [Header("Height")]
        [Range(8, 32)]  public int HeightMin = 28;
        [Range(32, 56)] public int HeightMax = 48;

        [Header("Surface Noise")]
        public float NoiseScale      = 0.04f;
        [Range(1, 8)] public int    NoiseOctaves     = 4;
        [Range(0f, 1f)] public float NoisePersistence = 0.5f;
        [Range(1f, 4f)] public float NoiseLacunarity  = 2.0f;

        [Header("Streaming")]
        [Tooltip("Chunks within this radius of the player are loaded.")]
        public int LoadRadius   = 5;
        [Tooltip("Chunks beyond this radius are unloaded.")]
        public int UnloadRadius = 7;

        [Tooltip("Max chunks meshed per frame to avoid spikes.")]
        public int MeshPerFrame = 3;

        /// <summary>
        /// Create a runtime copy of these settings with biome overrides applied.
        /// The original asset is not modified.
        /// </summary>
        public WorldGenSettings WithBiome(BiomeDefinition biome)
        {
            var copy = Instantiate(this);
            copy.name = $"{name}_{biome.BiomeId}";

            copy.HeightMin        = biome.HeightMin;
            copy.HeightMax        = biome.HeightMax;
            copy.NoiseScale       = biome.NoiseScale;
            copy.NoiseOctaves     = biome.NoiseOctaves;
            copy.NoisePersistence = biome.NoisePersistence;
            copy.NoiseLacunarity  = biome.NoiseLacunarity;

            return copy;
        }
    }
}
