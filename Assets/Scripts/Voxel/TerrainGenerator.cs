using UnityEngine;

namespace EverRealmExiles.Voxel
{
    public class TerrainGenerator
    {
        private int seed;
        private float baseScale = 0.02f;
        private float heightScale = 0.01f;
        private float caveScale = 0.05f;

        private int baseHeight = 64;
        private int heightAmplitude = 32;
        private int waterLevel = 58;

        public TerrainGenerator(int seed)
        {
            this.seed = seed;
        }

        public void GenerateChunk(VoxelChunk chunk)
        {
            Vector3Int chunkPos = chunk.chunkPosition;
            int worldOffsetX = chunkPos.x * VoxelChunk.CHUNK_SIZE;
            int worldOffsetZ = chunkPos.z * VoxelChunk.CHUNK_SIZE;

            for (int x = 0; x < VoxelChunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < VoxelChunk.CHUNK_SIZE; z++)
                {
                    int worldX = worldOffsetX + x;
                    int worldZ = worldOffsetZ + z;

                    int terrainHeight = GetTerrainHeight(worldX, worldZ);
                    BiomeType biome = GetBiome(worldX, worldZ);

                    GenerateColumn(chunk, x, z, worldX, worldZ, terrainHeight, biome);
                }
            }

            GenerateTrees(chunk, worldOffsetX, worldOffsetZ);
            GenerateOres(chunk, worldOffsetX, worldOffsetZ);

            chunk.SetGenerated(true);
        }

        private int GetTerrainHeight(int worldX, int worldZ)
        {
            float nx = worldX * baseScale + seed;
            float nz = worldZ * baseScale + seed;

            float continentalness = Mathf.PerlinNoise(nx * 0.3f, nz * 0.3f);
            float erosion = Mathf.PerlinNoise(nx * 0.7f + 500, nz * 0.7f + 500);
            float peaks = Mathf.PerlinNoise(nx * 2f + 1000, nz * 2f + 1000);

            float heightNoise = continentalness * 0.5f + erosion * 0.3f + peaks * 0.2f;

            float mountainNoise = Mathf.PerlinNoise(nx * heightScale + 2000, nz * heightScale + 2000);
            if (mountainNoise > 0.6f)
            {
                heightNoise += (mountainNoise - 0.6f) * 2f;
            }

            return baseHeight + Mathf.RoundToInt(heightNoise * heightAmplitude);
        }

        private enum BiomeType { Plains, Forest, Desert, Mountains, Snow }

        private BiomeType GetBiome(int worldX, int worldZ)
        {
            float temperature = Mathf.PerlinNoise(worldX * 0.005f + seed + 3000, worldZ * 0.005f + seed + 3000);
            float humidity = Mathf.PerlinNoise(worldX * 0.005f + seed + 4000, worldZ * 0.005f + seed + 4000);

            if (temperature > 0.7f && humidity < 0.3f)
                return BiomeType.Desert;
            if (temperature < 0.3f)
                return BiomeType.Snow;
            if (humidity > 0.6f)
                return BiomeType.Forest;

            float mountainHeight = Mathf.PerlinNoise(worldX * 0.01f + seed + 2000, worldZ * 0.01f + seed + 2000);
            if (mountainHeight > 0.7f)
                return BiomeType.Mountains;

            return BiomeType.Plains;
        }

        private void GenerateColumn(VoxelChunk chunk, int x, int z, int worldX, int worldZ, int terrainHeight, BiomeType biome)
        {
            for (int y = 0; y < VoxelChunk.CHUNK_HEIGHT; y++)
            {
                BlockType block = DetermineBlockType(y, terrainHeight, biome, worldX, worldZ);

                if (y > terrainHeight && y <= waterLevel)
                {
                    block = BlockType.Water;
                }

                if (block != BlockType.Air && y > 5 && y < terrainHeight - 5)
                {
                    if (IsCave(worldX, y, worldZ))
                    {
                        block = BlockType.Air;
                    }
                }

                chunk.SetBlock(x, y, z, block);
            }
        }

        private BlockType DetermineBlockType(int y, int terrainHeight, BiomeType biome, int worldX, int worldZ)
        {
            if (y == 0)
                return BlockType.Bedrock;

            if (y > terrainHeight)
                return BlockType.Air;

            if (y == terrainHeight)
            {
                switch (biome)
                {
                    case BiomeType.Desert:
                        return BlockType.Sand;
                    case BiomeType.Snow:
                        return BlockType.Snow;
                    default:
                        return terrainHeight <= waterLevel ? BlockType.Sand : BlockType.Grass;
                }
            }

            if (y > terrainHeight - 4)
            {
                switch (biome)
                {
                    case BiomeType.Desert:
                        return BlockType.Sand;
                    case BiomeType.Snow:
                        return BlockType.Dirt;
                    default:
                        return BlockType.Dirt;
                }
            }

            return BlockType.Stone;
        }

        private bool IsCave(int worldX, int y, int worldZ)
        {
            float cave1 = Perlin3D(worldX * caveScale, y * caveScale, worldZ * caveScale, seed);
            float cave2 = Perlin3D(worldX * caveScale * 0.5f, y * caveScale * 0.5f, worldZ * caveScale * 0.5f, seed + 1000);

            return cave1 > 0.6f && cave2 > 0.5f;
        }

        private void GenerateTrees(VoxelChunk chunk, int worldOffsetX, int worldOffsetZ)
        {
            System.Random random = new System.Random(seed + worldOffsetX * 10000 + worldOffsetZ);

            for (int x = 2; x < VoxelChunk.CHUNK_SIZE - 2; x++)
            {
                for (int z = 2; z < VoxelChunk.CHUNK_SIZE - 2; z++)
                {
                    int worldX = worldOffsetX + x;
                    int worldZ = worldOffsetZ + z;

                    BiomeType biome = GetBiome(worldX, worldZ);
                    if (biome == BiomeType.Desert || biome == BiomeType.Snow)
                        continue;

                    float treeChance = biome == BiomeType.Forest ? 0.02f : 0.005f;

                    if (random.NextDouble() < treeChance)
                    {
                        int terrainHeight = GetTerrainHeight(worldX, worldZ);
                        if (terrainHeight > waterLevel && terrainHeight < VoxelChunk.CHUNK_HEIGHT - 10)
                        {
                            if (chunk.GetBlock(x, terrainHeight, z) == BlockType.Grass)
                            {
                                GenerateTree(chunk, x, terrainHeight + 1, z, random);
                            }
                        }
                    }
                }
            }
        }

        private void GenerateTree(VoxelChunk chunk, int x, int y, int z, System.Random random)
        {
            int trunkHeight = 4 + random.Next(3);

            for (int i = 0; i < trunkHeight; i++)
            {
                if (y + i < VoxelChunk.CHUNK_HEIGHT)
                    chunk.SetBlock(x, y + i, z, BlockType.Wood);
            }

            int leafStart = y + trunkHeight - 2;
            int leafEnd = y + trunkHeight + 2;

            for (int ly = leafStart; ly <= leafEnd; ly++)
            {
                int radius = ly < y + trunkHeight ? 2 : 1;
                if (ly == leafEnd) radius = 0;

                for (int lx = -radius; lx <= radius; lx++)
                {
                    for (int lz = -radius; lz <= radius; lz++)
                    {
                        if (Mathf.Abs(lx) == radius && Mathf.Abs(lz) == radius && random.NextDouble() < 0.5)
                            continue;

                        int bx = x + lx;
                        int bz = z + lz;

                        if (bx >= 0 && bx < VoxelChunk.CHUNK_SIZE && bz >= 0 && bz < VoxelChunk.CHUNK_SIZE && ly < VoxelChunk.CHUNK_HEIGHT)
                        {
                            if (chunk.GetBlock(bx, ly, bz) == BlockType.Air)
                            {
                                chunk.SetBlock(bx, ly, bz, BlockType.Leaves);
                            }
                        }
                    }
                }
            }

            if (y + trunkHeight < VoxelChunk.CHUNK_HEIGHT)
            {
                chunk.SetBlock(x, y + trunkHeight, z, BlockType.Leaves);
                chunk.SetBlock(x, y + trunkHeight + 1, z, BlockType.Leaves);
            }
        }

        private void GenerateOres(VoxelChunk chunk, int worldOffsetX, int worldOffsetZ)
        {
            GenerateOre(chunk, worldOffsetX, worldOffsetZ, BlockType.Coal, 0.03f, 5, 80, 5000);
            GenerateOre(chunk, worldOffsetX, worldOffsetZ, BlockType.Iron, 0.02f, 5, 64, 6000);
            GenerateOre(chunk, worldOffsetX, worldOffsetZ, BlockType.Gold, 0.01f, 5, 32, 7000);
            GenerateOre(chunk, worldOffsetX, worldOffsetZ, BlockType.Diamond, 0.005f, 5, 16, 8000);
        }

        private void GenerateOre(VoxelChunk chunk, int worldOffsetX, int worldOffsetZ, BlockType oreType, float frequency, int minY, int maxY, int seedOffset)
        {
            for (int x = 0; x < VoxelChunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < VoxelChunk.CHUNK_SIZE; z++)
                {
                    for (int y = minY; y < maxY && y < VoxelChunk.CHUNK_HEIGHT; y++)
                    {
                        if (chunk.GetBlock(x, y, z) == BlockType.Stone)
                        {
                            int worldX = worldOffsetX + x;
                            int worldZ = worldOffsetZ + z;

                            float noise = Perlin3D(worldX * 0.1f, y * 0.1f, worldZ * 0.1f, seed + seedOffset);
                            if (noise > 1f - frequency)
                            {
                                chunk.SetBlock(x, y, z, oreType);
                            }
                        }
                    }
                }
            }
        }

        private float Perlin3D(float x, float y, float z, int seed)
        {
            float xy = Mathf.PerlinNoise(x + seed, y + seed);
            float xz = Mathf.PerlinNoise(x + seed, z + seed);
            float yz = Mathf.PerlinNoise(y + seed, z + seed);
            float yx = Mathf.PerlinNoise(y + seed, x + seed);
            float zx = Mathf.PerlinNoise(z + seed, x + seed);
            float zy = Mathf.PerlinNoise(z + seed, y + seed);

            return (xy + xz + yz + yx + zx + zy) / 6f;
        }
    }
}
