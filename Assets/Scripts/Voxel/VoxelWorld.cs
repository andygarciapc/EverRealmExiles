using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EverRealmExiles.Voxel
{
    public class VoxelWorld : MonoBehaviour
    {
        [Header("World Settings")]
        public int seed = 12345;
        public int renderDistance = 8;
        public Material voxelMaterial;

        [Header("Generation Settings")]
        public bool generateOnStart = true;
        public int chunksPerFrame = 2;

        [Header("Player Reference")]
        public Transform playerTransform;

        private Dictionary<Vector2Int, VoxelChunk> chunks = new Dictionary<Vector2Int, VoxelChunk>();
        private TerrainGenerator terrainGenerator;
        private Queue<Vector2Int> chunkGenerationQueue = new Queue<Vector2Int>();
        private HashSet<Vector2Int> queuedChunks = new HashSet<Vector2Int>();

        private Vector2Int lastPlayerChunk;
        private bool isGenerating = false;

        private void Awake()
        {
            if (seed == 0)
                seed = Random.Range(1, 999999);

            terrainGenerator = new TerrainGenerator(seed);
        }

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateInitialWorld();
            }
        }

        private void Update()
        {
            if (playerTransform != null)
            {
                UpdateChunksAroundPlayer();
            }

            ProcessChunkQueue();
        }

        public void GenerateInitialWorld()
        {
            Vector2Int centerChunk = Vector2Int.zero;

            if (playerTransform != null)
            {
                centerChunk = GetChunkPosition(playerTransform.position);
            }

            lastPlayerChunk = centerChunk;

            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector2Int chunkPos = new Vector2Int(centerChunk.x + x, centerChunk.y + z);
                    if (!queuedChunks.Contains(chunkPos) && !chunks.ContainsKey(chunkPos))
                    {
                        chunkGenerationQueue.Enqueue(chunkPos);
                        queuedChunks.Add(chunkPos);
                    }
                }
            }

            Debug.Log($"VoxelWorld: Queued {chunkGenerationQueue.Count} chunks for generation (Seed: {seed})");
        }

        private void UpdateChunksAroundPlayer()
        {
            Vector2Int currentPlayerChunk = GetChunkPosition(playerTransform.position);

            if (currentPlayerChunk != lastPlayerChunk)
            {
                lastPlayerChunk = currentPlayerChunk;

                for (int x = -renderDistance; x <= renderDistance; x++)
                {
                    for (int z = -renderDistance; z <= renderDistance; z++)
                    {
                        Vector2Int chunkPos = new Vector2Int(currentPlayerChunk.x + x, currentPlayerChunk.y + z);
                        if (!chunks.ContainsKey(chunkPos) && !queuedChunks.Contains(chunkPos))
                        {
                            chunkGenerationQueue.Enqueue(chunkPos);
                            queuedChunks.Add(chunkPos);
                        }
                    }
                }

                UnloadDistantChunks(currentPlayerChunk);
            }
        }

        private void ProcessChunkQueue()
        {
            int processed = 0;
            while (chunkGenerationQueue.Count > 0 && processed < chunksPerFrame)
            {
                Vector2Int chunkPos = chunkGenerationQueue.Dequeue();
                queuedChunks.Remove(chunkPos);

                if (!chunks.ContainsKey(chunkPos))
                {
                    CreateChunk(chunkPos);
                    processed++;
                }
            }
        }

        private void CreateChunk(Vector2Int position)
        {
            GameObject chunkObject = new GameObject($"Chunk_{position.x}_{position.y}");
            chunkObject.transform.parent = transform;

            VoxelChunk chunk = chunkObject.AddComponent<VoxelChunk>();
            chunk.Initialize(new Vector3Int(position.x, 0, position.y), this);

            if (voxelMaterial != null)
            {
                chunk.SetMaterial(voxelMaterial);
            }
            else
            {
                chunk.SetMaterial(CreateDefaultMaterial());
            }

            terrainGenerator.GenerateChunk(chunk);
            chunk.GenerateMesh();

            chunks[position] = chunk;
        }

        private void UnloadDistantChunks(Vector2Int playerChunk)
        {
            List<Vector2Int> chunksToRemove = new List<Vector2Int>();
            int unloadDistance = renderDistance + 2;

            foreach (var kvp in chunks)
            {
                int distX = Mathf.Abs(kvp.Key.x - playerChunk.x);
                int distZ = Mathf.Abs(kvp.Key.y - playerChunk.y);

                if (distX > unloadDistance || distZ > unloadDistance)
                {
                    chunksToRemove.Add(kvp.Key);
                }
            }

            foreach (var pos in chunksToRemove)
            {
                if (chunks.TryGetValue(pos, out VoxelChunk chunk))
                {
                    Destroy(chunk.gameObject);
                    chunks.Remove(pos);
                }
            }
        }

        public VoxelChunk GetChunk(Vector2Int position)
        {
            chunks.TryGetValue(position, out VoxelChunk chunk);
            return chunk;
        }

        public VoxelChunk GetChunkAtWorldPosition(Vector3 worldPos)
        {
            Vector2Int chunkPos = GetChunkPosition(worldPos);
            return GetChunk(chunkPos);
        }

        public BlockType GetBlock(int worldX, int worldY, int worldZ)
        {
            if (worldY < 0 || worldY >= VoxelChunk.CHUNK_HEIGHT)
                return BlockType.Air;

            Vector2Int chunkPos = new Vector2Int(
                Mathf.FloorToInt((float)worldX / VoxelChunk.CHUNK_SIZE),
                Mathf.FloorToInt((float)worldZ / VoxelChunk.CHUNK_SIZE)
            );

            VoxelChunk chunk = GetChunk(chunkPos);
            if (chunk == null)
                return BlockType.Air;

            int localX = worldX - chunkPos.x * VoxelChunk.CHUNK_SIZE;
            int localZ = worldZ - chunkPos.y * VoxelChunk.CHUNK_SIZE;

            if (localX < 0) localX += VoxelChunk.CHUNK_SIZE;
            if (localZ < 0) localZ += VoxelChunk.CHUNK_SIZE;

            return chunk.GetBlock(localX, worldY, localZ);
        }

        public void SetBlock(int worldX, int worldY, int worldZ, BlockType type)
        {
            if (worldY < 0 || worldY >= VoxelChunk.CHUNK_HEIGHT)
                return;

            Vector2Int chunkPos = new Vector2Int(
                Mathf.FloorToInt((float)worldX / VoxelChunk.CHUNK_SIZE),
                Mathf.FloorToInt((float)worldZ / VoxelChunk.CHUNK_SIZE)
            );

            VoxelChunk chunk = GetChunk(chunkPos);
            if (chunk == null)
                return;

            int localX = worldX - chunkPos.x * VoxelChunk.CHUNK_SIZE;
            int localZ = worldZ - chunkPos.y * VoxelChunk.CHUNK_SIZE;

            if (localX < 0) localX += VoxelChunk.CHUNK_SIZE;
            if (localZ < 0) localZ += VoxelChunk.CHUNK_SIZE;

            chunk.SetBlock(localX, worldY, localZ, type);
            chunk.GenerateMesh();

            UpdateNeighborChunks(localX, localZ, chunkPos);
        }

        private void UpdateNeighborChunks(int localX, int localZ, Vector2Int chunkPos)
        {
            if (localX == 0)
                UpdateChunkMesh(new Vector2Int(chunkPos.x - 1, chunkPos.y));
            if (localX == VoxelChunk.CHUNK_SIZE - 1)
                UpdateChunkMesh(new Vector2Int(chunkPos.x + 1, chunkPos.y));
            if (localZ == 0)
                UpdateChunkMesh(new Vector2Int(chunkPos.x, chunkPos.y - 1));
            if (localZ == VoxelChunk.CHUNK_SIZE - 1)
                UpdateChunkMesh(new Vector2Int(chunkPos.x, chunkPos.y + 1));
        }

        private void UpdateChunkMesh(Vector2Int pos)
        {
            VoxelChunk chunk = GetChunk(pos);
            if (chunk != null)
            {
                chunk.MarkDirty();
                chunk.GenerateMesh();
            }
        }

        private Vector2Int GetChunkPosition(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / VoxelChunk.CHUNK_SIZE),
                Mathf.FloorToInt(worldPos.z / VoxelChunk.CHUNK_SIZE)
            );
        }

        public Vector3 GetSpawnPosition()
        {
            int x = 0;
            int z = 0;

            for (int y = VoxelChunk.CHUNK_HEIGHT - 1; y >= 0; y--)
            {
                if (BlockDatabase.IsSolid(GetBlock(x, y, z)))
                {
                    return new Vector3(x + 0.5f, y + 2f, z + 0.5f);
                }
            }

            return new Vector3(0, 70, 0);
        }

        private Material CreateDefaultMaterial()
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = Color.white;
            return mat;
        }

        public void RegenerateWorld(int newSeed)
        {
            foreach (var chunk in chunks.Values)
            {
                if (chunk != null)
                    Destroy(chunk.gameObject);
            }
            chunks.Clear();
            chunkGenerationQueue.Clear();
            queuedChunks.Clear();

            seed = newSeed;
            terrainGenerator = new TerrainGenerator(seed);

            GenerateInitialWorld();
        }
    }
}
