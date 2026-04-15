using UnityEngine;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.World
{
    /// <summary>
    /// Fills a <see cref="Chunk"/> with block data from a seeded noise function
    /// and places Points of Interest (extraction points, enemy camps, treasure caches).
    /// Pure C# — no MonoBehaviour. Safe to call from a background thread.
    ///
    /// NOTE: Mathf.PerlinNoise is assumed thread-safe (stateless math). Replace
    /// with a pure C# noise library if threading issues arise.
    /// </summary>
    public sealed class WorldGenerator
    {
        private readonly WorldGenSettings _s;
        private readonly BlockType _surfaceBlock;
        private readonly BlockType _subSurfaceBlock;

        // Precomputed seed offsets so each noise layer is independent.
        private readonly float _seedX;
        private readonly float _seedZ;

        // POI placement parameters — captured from POISettings at construction
        // so the background thread never touches the ScriptableObject.
        private readonly bool _hasTrees;
        private readonly int _chunkRadius;
        private readonly int _campSpacing;
        private readonly float _campThreshold;
        private readonly int _campSeedOffset;
        private readonly int _cacheSpacing;
        private readonly float _cacheThreshold;
        private readonly int _cacheSeedOffset;

        public WorldGenerator(WorldGenSettings settings,
            BlockType surfaceBlock = BlockType.Grass,
            BlockType subSurfaceBlock = BlockType.Dirt,
            POISettings poiSettings = null,
            int chunkRadius = 8,
            bool hasTrees = false)
        {
            _s = settings;
            _surfaceBlock = surfaceBlock;
            _subSurfaceBlock = subSurfaceBlock;
            _hasTrees = hasTrees;
            _chunkRadius = chunkRadius;

            // Scatter seed offsets so nearby seeds don't produce similar worlds.
            _seedX = settings.Seed * 0.3721f;
            _seedZ = settings.Seed * 0.6547f;

            // Capture POI settings (or defaults) for thread-safe access.
            _campSpacing     = poiSettings != null ? poiSettings.EnemyCampSpacing        : 50;
            _campThreshold   = poiSettings != null ? poiSettings.EnemyCampThreshold       : 0.40f;
            _campSeedOffset  = poiSettings != null ? poiSettings.EnemyCampSeedOffset      : 2000;
            _cacheSpacing    = poiSettings != null ? poiSettings.TreasureCacheSpacing     : 60;
            _cacheThreshold  = poiSettings != null ? poiSettings.TreasureCacheThreshold   : 0.30f;
            _cacheSeedOffset = poiSettings != null ? poiSettings.TreasureCacheSeedOffset  : 3000;
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

            if (_hasTrees)
                PlaceTrees(chunk, cx, cz);

            PlacePOIs(chunk, cx, cz);
        }

        // -----------------------------------------------------------------
        // Tree placement
        // -----------------------------------------------------------------

        /// <summary>
        /// Places voxel trees at deterministic positions. Trees consist of a Wood trunk
        /// (3–5 blocks) topped with a Leaves canopy (roughly 3×3×2 ellipsoid).
        /// Only placed on surface blocks matching <see cref="_surfaceBlock"/>.
        /// </summary>
        private void PlaceTrees(Chunk chunk, int cx, int cz)
        {
            const int spacing = 8;
            const float threshold = 0.25f;
            const int seedOffset = 5000;

            int minCellX = FloorDiv(cx, spacing);
            int maxCellX = FloorDiv(cx + Chunk.Width - 1, spacing);
            int minCellZ = FloorDiv(cz, spacing);
            int maxCellZ = FloorDiv(cz + Chunk.Depth - 1, spacing);

            for (int cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                for (int cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
                {
                    float hash = PseudoHash(cellX, cellZ, _s.Seed + seedOffset);
                    if (hash > threshold) continue;

                    // Position within cell — offset so it doesn't always land on the grid.
                    int wx = cellX * spacing + (int)(PseudoHash(cellX, cellZ, _s.Seed + seedOffset + 1) * spacing);
                    int wz = cellZ * spacing + (int)(PseudoHash(cellX, cellZ, _s.Seed + seedOffset + 2) * spacing);

                    int lx = wx - cx;
                    int lz = wz - cz;

                    // Need 2-block border for canopy.
                    if (lx < 2 || lx >= Chunk.Width - 2 || lz < 2 || lz >= Chunk.Depth - 2)
                        continue;

                    int surface = SurfaceHeight(wx, wz);
                    if (surface <= 0 || surface >= Chunk.Height - 8) continue;

                    // Only place on the expected surface block type.
                    if (chunk.GetBlock(lx, surface, lz) != _surfaceBlock) continue;

                    // Trunk height varies by hash (3–5 blocks).
                    int trunkHeight = 3 + (int)(PseudoHash(cellX + 7, cellZ + 3, _s.Seed + seedOffset + 3) * 3);

                    // Place trunk.
                    for (int ty = 1; ty <= trunkHeight; ty++)
                    {
                        int y = surface + ty;
                        if (chunk.IsInBounds(lx, y, lz))
                            chunk.SetBlock(lx, y, lz, BlockType.Wood);
                    }

                    // Place canopy: 3×3×2 ellipsoid with corners removed.
                    int canopyBase = surface + trunkHeight;
                    for (int cy = 0; cy < 2; cy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                // Remove corners on top layer for rounder shape.
                                if (cy == 1 && Mathf.Abs(dx) + Mathf.Abs(dz) > 1) continue;

                                int px = lx + dx;
                                int py = canopyBase + cy;
                                int pz = lz + dz;

                                if (!chunk.IsInBounds(px, py, pz)) continue;

                                // Don't overwrite trunk.
                                if (dx == 0 && dz == 0 && cy == 0) continue;

                                chunk.SetBlock(px, py, pz, BlockType.Leaves);
                            }
                        }
                    }

                    // Trunk continues through canopy base.
                    if (chunk.IsInBounds(lx, canopyBase, lz))
                        chunk.SetBlock(lx, canopyBase, lz, BlockType.Wood);
                }
            }
        }

        // -----------------------------------------------------------------
        // POI placement
        // -----------------------------------------------------------------

        /// <summary>Orchestrates all POI placement passes for a chunk.</summary>
        private void PlacePOIs(Chunk chunk, int cx, int cz)
        {
            PlaceExtractionPoints(chunk, cx, cz);
            PlaceEnemyCamps(chunk, cx, cz);
            PlaceTreasureCaches(chunk, cx, cz);
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

        /// <summary>
        /// Places enemy camp platforms at deterministic positions.
        /// Camps only appear in Medium and High risk zones.
        /// </summary>
        private void PlaceEnemyCamps(Chunk chunk, int cx, int cz)
        {
            int spacing = _campSpacing;
            if (spacing <= 0) return;

            int minCellX = FloorDiv(cx, spacing);
            int maxCellX = FloorDiv(cx + Chunk.Width - 1, spacing);
            int minCellZ = FloorDiv(cz, spacing);
            int maxCellZ = FloorDiv(cz + Chunk.Depth - 1, spacing);

            for (int cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                for (int cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
                {
                    float hash = PseudoHash(cellX, cellZ, _s.Seed + _campSeedOffset);
                    if (hash > _campThreshold) continue;

                    // Offset by 3 (different from extraction offset of 5)
                    // to reduce overlap with extraction point grid.
                    int wx = cellX * spacing + spacing / 2 + 3;
                    int wz = cellZ * spacing + spacing / 2 + 3;

                    // Skip Safe zone — camps only in Medium and High.
                    var zone = MapLayout.GetZone(wx, wz, _chunkRadius);
                    if (zone == RiskZone.Safe) continue;

                    // Skip if outside playable bounds.
                    if (!MapLayout.IsInBounds(wx, wz, _chunkRadius)) continue;

                    int lx = wx - cx;
                    int lz = wz - cz;

                    if (lx < 1 || lx >= Chunk.Width - 1 || lz < 1 || lz >= Chunk.Depth - 1)
                        continue;

                    int surface = SurfaceHeight(wx, wz);
                    int platformY = surface + 1;
                    if (platformY >= Chunk.Height - 2) continue;

                    // Place a 3×1×3 platform of EnemyCampCore blocks.
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int px = lx + dx;
                            int pz = lz + dz;
                            if (chunk.IsInBounds(px, platformY, pz))
                                chunk.SetBlock(px, platformY, pz, BlockType.EnemyCampCore);

                            // Clear 4 blocks of air above for headroom.
                            for (int ay = 1; ay <= 4; ay++)
                            {
                                int clearY = platformY + ay;
                                if (chunk.IsInBounds(px, clearY, pz))
                                    chunk.SetBlock(px, clearY, pz, BlockType.Air);
                            }
                        }

                    chunk.AddPOIMarker(new POIMarker(
                        POIType.EnemyCamp, wx, wz, surface, zone));
                }
            }
        }

        /// <summary>
        /// Places treasure cache blocks at deterministic positions.
        /// Caches appear in all zones; loot quality scales with risk.
        /// </summary>
        private void PlaceTreasureCaches(Chunk chunk, int cx, int cz)
        {
            int spacing = _cacheSpacing;
            if (spacing <= 0) return;

            int minCellX = FloorDiv(cx, spacing);
            int maxCellX = FloorDiv(cx + Chunk.Width - 1, spacing);
            int minCellZ = FloorDiv(cz, spacing);
            int maxCellZ = FloorDiv(cz + Chunk.Depth - 1, spacing);

            for (int cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                for (int cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
                {
                    float hash = PseudoHash(cellX, cellZ, _s.Seed + _cacheSeedOffset);
                    if (hash > _cacheThreshold) continue;

                    // Offset by 7 (different from camp=3 and extraction=5).
                    int wx = cellX * spacing + spacing / 2 + 7;
                    int wz = cellZ * spacing + spacing / 2 + 7;

                    if (!MapLayout.IsInBounds(wx, wz, _chunkRadius)) continue;

                    int lx = wx - cx;
                    int lz = wz - cz;

                    if (lx < 0 || lx >= Chunk.Width || lz < 0 || lz >= Chunk.Depth)
                        continue;

                    int surface = SurfaceHeight(wx, wz);
                    int blockY = surface + 1;
                    if (blockY >= Chunk.Height - 1) continue;

                    // Place a single TreasureCacheCore block.
                    if (chunk.IsInBounds(lx, blockY, lz))
                        chunk.SetBlock(lx, blockY, lz, BlockType.TreasureCacheCore);

                    // Clear air above for interaction.
                    for (int ay = 1; ay <= 3; ay++)
                    {
                        int clearY = blockY + ay;
                        if (chunk.IsInBounds(lx, clearY, lz))
                            chunk.SetBlock(lx, clearY, lz, BlockType.Air);
                    }

                    var zone = MapLayout.GetZone(wx, wz, _chunkRadius);
                    chunk.AddPOIMarker(new POIMarker(
                        POIType.TreasureCache, wx, wz, surface, zone));
                }
            }
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

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
            if (y == surface)         return _surfaceBlock;
            if (y >= surface - 3)     return _subSurfaceBlock;

            // Cave carving — carve air pockets in stone below the subsurface layer.
            if (y < surface - _s.CaveMinDepth && y > _s.CaveMinY)
            {
                float cave = CaveNoise(wx, y, wz, _s.CaveNoiseScale, _s.Seed + 50);
                if (cave > _s.CaveThreshold)
                    return BlockType.Air;
            }

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

        // 3-D cave noise using three 2-D Perlin slices blended together.
        // Produces more connected, tunnel-like cavities than the simpler OreNoise.
        private float CaveNoise(float wx, float wy, float wz, float scale, int seed)
        {
            float sx = seed * 0.3173f;
            float n1 = Mathf.PerlinNoise(wx * scale + sx, wz * scale + sx);           // XZ
            float n2 = Mathf.PerlinNoise(wx * scale + sx, wy * scale + sx * 0.7f);    // XY
            float n3 = Mathf.PerlinNoise(wy * scale + sx * 0.4f, wz * scale + sx);    // YZ
            return (n1 + n2 + n3) / 3f;
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
