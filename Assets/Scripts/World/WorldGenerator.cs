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

            PlaceExtractionPoints(chunk, cx, cz);
        }

        /// <summary>
        /// Places extraction point structures at deterministic positions.
        /// Uses a grid-based approach: one potential extraction point per
        /// ExtractionSpacing-block cell. A noise threshold controls density
        /// so not every cell gets one.
        /// </summary>
        private void PlaceExtractionPoints(Chunk chunk, int cx, int cz)
        {
            // Spacing between potential extraction point locations (in blocks).
            const int spacing = 80;
            // Noise threshold — only place if hash is below this.
            const float threshold = 0.35f;

            // Determine which grid cells this chunk overlaps.
            int minCellX = FloorDiv(cx, spacing);
            int maxCellX = FloorDiv(cx + Chunk.Width - 1, spacing);
            int minCellZ = FloorDiv(cz, spacing);
            int maxCellZ = FloorDiv(cz + Chunk.Depth - 1, spacing);

            for (int cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                for (int cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
                {
                    // Deterministic hash per cell to decide placement.
                    float hash = PseudoHash(cellX, cellZ, _s.Seed + 999);
                    if (hash > threshold) continue;

                    // World position: center of the cell, offset by 5 so the local
                    // coordinate (45 mod 16 = 13) lands safely inside [1,14].
                    int wx = cellX * spacing + spacing / 2 + 5;
                    int wz = cellZ * spacing + spacing / 2 + 5;

                    // Convert to local chunk coords.
                    int lx = wx - cx;
                    int lz = wz - cz;

                    // Only place if the center falls within this chunk.
                    if (lx < 1 || lx >= Chunk.Width - 1 || lz < 1 || lz >= Chunk.Depth - 1)
                        continue;

                    // Find surface height at center.
                    int surface = SurfaceHeight(wx, wz);
                    int platformY = surface + 1;

                    if (platformY >= Chunk.Height - 1) continue;

                    // Place a 3×1×3 platform of ExtractionCore blocks.
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int px = lx + dx;
                            int pz = lz + dz;
                            if (chunk.IsInBounds(px, platformY, pz))
                                chunk.SetBlock(px, platformY, pz, BlockType.ExtractionCore);

                            // Clear air above so the player can stand on it.
                            for (int ay = 1; ay <= 3; ay++)
                            {
                                int clearY = platformY + ay;
                                if (chunk.IsInBounds(px, clearY, pz))
                                    chunk.SetBlock(px, clearY, pz, BlockType.Air);
                            }
                        }
                }
            }
        }

        private static int FloorDiv(int a, int b)
        {
            return a >= 0 ? a / b : (a - b + 1) / b;
        }

        /// <summary>Simple deterministic hash in [0,1] for placement decisions.</summary>
        private static float PseudoHash(int x, int z, int seed)
        {
            int h = x * 374761393 + z * 668265263 + seed;
            h = (h ^ (h >> 13)) * 1274126177;
            h = h ^ (h >> 16);
            return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF;
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
