using UnityEngine;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.World
{
    /// <summary>
    /// Fills a <see cref="Chunk"/> with block data from a seeded noise function.
    /// Pure C# — no MonoBehaviour. Safe to call from a background thread.
    ///
    /// NOTE: Mathf.PerlinNoise is assumed thread-safe (stateless math). Replace
    /// with a pure C# noise library if threading issues arise.
    /// </summary>
    public sealed class WorldGenerator
    {
        private readonly WorldGenSettings _s;

        // Precomputed seed offsets so each noise layer is independent.
        private readonly float _seedX;
        private readonly float _seedZ;

        public WorldGenerator(WorldGenSettings settings)
        {
            _s = settings;
            // Scatter seed offsets so nearby seeds don't produce similar worlds.
            _seedX = settings.Seed * 0.3721f;
            _seedZ = settings.Seed * 0.6547f;
        }

        /// <summary>Fills <paramref name="chunk"/> with terrain data. Thread-safe.</summary>
        public void Generate(Chunk chunk)
        {
            int cx = chunk.ChunkPosition.x * Chunk.Width;
            int cz = chunk.ChunkPosition.y * Chunk.Depth;

            for (int x = 0; x < Chunk.Width; x++)
            {
                for (int z = 0; z < Chunk.Depth; z++)
                {
                    int worldX = cx + x;
                    int worldZ = cz + z;
                    int surface = SurfaceHeight(worldX, worldZ);

                    for (int y = 0; y < Chunk.Height; y++)
                        chunk.SetBlock(x, y, z, BlockAt(worldX, y, worldZ, surface));
                }
            }
        }

        private int SurfaceHeight(int wx, int wz)
        {
            float n = FractalNoise(wx, wz);
            return Mathf.RoundToInt(Mathf.Lerp(_s.HeightMin, _s.HeightMax, n));
        }

        private BlockType BlockAt(int wx, int y, int wz, int surface)
        {
            if (y > surface)          return BlockType.Air;
            if (y == surface)         return BlockType.Grass;
            if (y >= surface - 3)     return BlockType.Dirt;

            // Ore seams — 3-D approximation with two 2-D noise layers
            if (y < 48 && OreNoise(wx, y, wz, 0.09f, _s.Seed + 10) > 0.84f) return BlockType.CoalOre;
            if (y < 32 && OreNoise(wx, y, wz, 0.11f, _s.Seed + 20) > 0.90f) return BlockType.IronOre;
            if (y < 16 && OreNoise(wx, y, wz, 0.14f, _s.Seed + 30) > 0.93f) return BlockType.GoldOre;

            return BlockType.Stone;
        }

        // Fractal Perlin noise in [0,1]
        private float FractalNoise(float wx, float wz)
        {
            float value     = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue  = 0f;

            for (int i = 0; i < _s.NoiseOctaves; i++)
            {
                float nx = (wx * _s.NoiseScale * frequency) + _seedX;
                float nz = (wz * _s.NoiseScale * frequency) + _seedZ;
                value    += Mathf.PerlinNoise(nx, nz) * amplitude;
                maxValue += amplitude;
                amplitude *= _s.NoisePersistence;
                frequency *= _s.NoiseLacunarity;
            }

            return value / maxValue;
        }

        // Rough 3-D noise using two XZ/XY slices
        private float OreNoise(float wx, float wy, float wz, float scale, int seed)
        {
            float sx = seed * 0.4111f;
            float n1 = Mathf.PerlinNoise(wx * scale + sx, wz * scale + sx);
            float n2 = Mathf.PerlinNoise(wx * scale + sx, wy * scale + sx * 0.5f);
            return (n1 + n2) * 0.5f;
        }
    }
}
