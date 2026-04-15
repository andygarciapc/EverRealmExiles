using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.Extraction;

namespace EverRealm.Exiles.World
{
    /// <summary>
    /// Manages chunk streaming around the player.
    ///
    /// Flow:
    ///   1. Each Update, determine which chunk positions need loading/unloading.
    ///   2. For each new chunk: fire a background Task to fill block data
    ///      (<see cref="WorldGenerator"/>).
    ///   3. Completed chunks are enqueued; ProcessMeshQueue() meshes up to
    ///      <see cref="WorldGenSettings.MeshPerFrame"/> per frame (main thread).
    ///   4. Chunks beyond the unload radius are destroyed.
    ///
    /// Setup: add to a GameObject in the Game scene. Assign _settings, _chunkMaterial,
    /// and _playerTransform in the Inspector. Create a WorldGenSettings asset via
    /// Assets → Create → EverRealm → World Gen Settings.
    ///
    /// Atlas texture: create a Texture2D of width=1, height=<see cref="ChunkMesher.BlockTypeCount"/>
    /// with one colour per row (row 0 = Air, row 1 = Grass, …). Call GenerateDebugAtlas()
    /// from Start to get a placeholder atlas at runtime.
    /// </summary>
    public sealed class WorldManager : MonoBehaviour
    {
        [SerializeField] private WorldGenSettings _settings;
        [SerializeField] private Material         _chunkMaterial;
        [SerializeField] private Transform        _playerTransform;

        private WorldGenerator  _generator;
        private BlockEntityManager _blockEntities;
        private CancellationTokenSource _cts;

        /// <summary>Singleton accessor so systems like AI pathfinding can query blocks.</summary>
        public static WorldManager Instance { get; private set; }

        /// <summary>Registry for special blocks (chests, etc.) that carry extra data.</summary>
        public BlockEntityManager BlockEntities => _blockEntities;

        // Chunk positions currently loaded (active in scene)
        private readonly Dictionary<Vector2Int, ChunkRenderer> _activeChunks = new();

        // Keep chunk data alive for runtime queries (pathfinding, destruction, etc.)
        private readonly Dictionary<Vector2Int, Chunk> _chunkData = new();

        // Chunk positions with a generation task in flight
        private readonly HashSet<Vector2Int> _pendingChunks = new();

        // Background-thread → main-thread handoff
        private readonly ConcurrentQueue<Chunk> _meshQueue = new();

        // -------------------------------------------------------------------------

        private void Awake()
        {
            Instance       = this;
            _cts           = new CancellationTokenSource();
            _blockEntities = new BlockEntityManager();

            // Apply biome overrides if a biome was selected from the Play tab.
            var biome = Core.GameBootstrap.Instance?.SelectedBiome;
            if (biome != null)
            {
                var biomeSettings = _settings.WithBiome(biome);
                _generator = new WorldGenerator(biomeSettings, biome.SurfaceBlock, biome.SubSurfaceBlock);
                Debug.Log($"[WorldManager] Using biome '{biome.BiomeName}' for world generation.");
            }
            else
            {
                _generator = new WorldGenerator(_settings);
            }
        }

        private void Start()
        {
            // Populate the chunk material with a generated debug atlas if no texture is assigned.
            if (_chunkMaterial != null && _chunkMaterial.mainTexture == null)
                _chunkMaterial.mainTexture = GenerateDebugAtlas();
        }

        private void Update()
        {
            ProcessMeshQueue();
            UpdateStreaming();
        }

        private void OnDestroy()
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        // -------------------------------------------------------------------------
        // Streaming

        private void UpdateStreaming()
        {
            // Fall back to world origin if no player exists yet (pre-Phase 3).
            Vector3 observerPos = _playerTransform != null ? _playerTransform.position : Vector3.zero;
            Vector2Int playerChunk = WorldToChunk(observerPos);
            int load   = _settings.LoadRadius;
            int unload = _settings.UnloadRadius;

            // Load
            for (int x = -load; x <= load; x++)
                for (int z = -load; z <= load; z++)
                {
                    var cp = playerChunk + new Vector2Int(x, z);
                    if (!_activeChunks.ContainsKey(cp) && !_pendingChunks.Contains(cp))
                        BeginChunkLoad(cp);
                }

            // Unload
            var toRemove = new List<Vector2Int>();
            foreach (var cp in _activeChunks.Keys)
                if (ChebyshevDist(cp, playerChunk) > unload)
                    toRemove.Add(cp);

            foreach (var cp in toRemove)
                UnloadChunk(cp);
        }

        private async void BeginChunkLoad(Vector2Int chunkPos)
        {
            _pendingChunks.Add(chunkPos);
            var chunk = new Chunk(chunkPos);
            var token = _cts.Token;

            try
            {
                await Task.Run(() => _generator.Generate(chunk), token);

                // Back on main thread (Unity SynchronizationContext).
                if (!token.IsCancellationRequested)
                    _meshQueue.Enqueue(chunk);
            }
            catch (TaskCanceledException) { }
        }

        private void UnloadChunk(Vector2Int chunkPos)
        {
            if (!_activeChunks.TryGetValue(chunkPos, out var cr)) return;
            _blockEntities.RemoveChunkEntities(chunkPos);
            Destroy(cr.gameObject);
            _activeChunks.Remove(chunkPos);
            _chunkData.Remove(chunkPos);
        }

        // -------------------------------------------------------------------------
        // Mesh building (main thread)

        private void ProcessMeshQueue()
        {
            int count = 0;
            while (count < _settings.MeshPerFrame && _meshQueue.TryDequeue(out var chunk))
            {
                _pendingChunks.Remove(chunk.ChunkPosition);

                Mesh mesh        = ChunkMesher.BuildMesh(chunk);
                ChunkRenderer cr = SpawnChunkRenderer(chunk, mesh);
                _activeChunks[chunk.ChunkPosition] = cr;
                _chunkData[chunk.ChunkPosition] = chunk;

                RegisterBlockEntities(chunk);
                count++;
            }
        }

        private ChunkRenderer SpawnChunkRenderer(Chunk chunk, Mesh mesh)
        {
            var go = new GameObject($"Chunk_{chunk.ChunkPosition.x}_{chunk.ChunkPosition.y}");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(
                chunk.ChunkPosition.x * Chunk.Width,
                0f,
                chunk.ChunkPosition.y * Chunk.Depth
            );

            var cr = go.AddComponent<ChunkRenderer>();
            cr.Initialize(mesh, _chunkMaterial);
            return cr;
        }

        // -------------------------------------------------------------------------
        // Block entity registration

        /// <summary>
        /// Scans a newly loaded chunk for special block types and registers
        /// their block entities. Called on the main thread after meshing.
        /// </summary>
        private void RegisterBlockEntities(Chunk chunk)
        {
            int cx = chunk.ChunkPosition.x * Chunk.Width;
            int cz = chunk.ChunkPosition.y * Chunk.Depth;

            for (int x = 0; x < Chunk.Width; x++)
                for (int z = 0; z < Chunk.Depth; z++)
                    for (int y = 0; y < Chunk.Height; y++)
                    {
                        var bt = chunk.GetBlock(x, y, z);
                        if (bt == BlockType.Air) continue;

                        var worldPos = new Vector3Int(cx + x, y, cz + z);

                        switch (bt)
                        {
                            case BlockType.ExtractionCore:
                                _blockEntities.Register(new ExtractionBlockEntity(worldPos));
                                break;
                            // Future: case BlockType.Chest → register ChestBlockEntity
                        }
                    }
        }

        // -------------------------------------------------------------------------
        // Helpers

        /// <summary>
        /// Returns the block type at a world-space integer position.
        /// Returns Air if the chunk is not loaded.
        /// </summary>
        public BlockType GetBlock(int wx, int wy, int wz)
        {
            // Integer division that floors for negatives.
            int cx = wx >= 0 ? wx / Chunk.Width  : (wx - Chunk.Width  + 1) / Chunk.Width;
            int cz = wz >= 0 ? wz / Chunk.Depth  : (wz - Chunk.Depth  + 1) / Chunk.Depth;
            var cp = new Vector2Int(cx, cz);

            if (!_chunkData.TryGetValue(cp, out Chunk chunk)) return BlockType.Air;

            int lx = wx - cx * Chunk.Width;
            int lz = wz - cz * Chunk.Depth;

            if (!chunk.IsInBounds(lx, wy, lz)) return BlockType.Air;
            return chunk.GetBlock(lx, wy, lz);
        }

        /// <summary>
        /// Returns true if (wx, wy, wz) is a walkable surface:
        /// the block is Air and the block below is solid.
        /// </summary>
        public bool IsWalkable(int wx, int wy, int wz)
        {
            return wy > 0
                && GetBlock(wx, wy, wz) == BlockType.Air
                && GetBlock(wx, wy - 1, wz) != BlockType.Air;
        }

        /// <summary>
        /// Finds the Y of the walkable surface at (wx, wz), searching downward from startY.
        /// Returns -1 if none found.
        /// </summary>
        public int GetSurfaceY(int wx, int wz, int startY = Chunk.Height - 1)
        {
            for (int y = startY; y > 0; y--)
            {
                if (IsWalkable(wx, y, wz))
                    return y;
            }
            return -1;
        }

        /// <summary>
        /// Convert a raycast hit on terrain to the world-space integer position
        /// of the solid block that was hit. Nudges inward along the hit normal
        /// so we land inside the block, not on its surface.
        /// </summary>
        public static Vector3Int HitToBlockPos(RaycastHit hit)
        {
            // Nudge slightly into the block (opposite of normal) to avoid landing on the boundary.
            Vector3 inside = hit.point - hit.normal * 0.1f;
            return new Vector3Int(
                Mathf.FloorToInt(inside.x),
                Mathf.FloorToInt(inside.y),
                Mathf.FloorToInt(inside.z)
            );
        }

        private static Vector2Int WorldToChunk(Vector3 worldPos) => new(
            Mathf.FloorToInt(worldPos.x / Chunk.Width),
            Mathf.FloorToInt(worldPos.z / Chunk.Depth)
        );

        private static int ChebyshevDist(Vector2Int a, Vector2Int b)
            => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        /// <summary>
        /// Creates a 1×N texture with one distinct colour per BlockType.
        /// Lets you see terrain without any external texture assets.
        /// Replace with a proper atlas in Phase 11.
        /// </summary>
        public static Texture2D GenerateDebugAtlas()
        {
            int count  = ChunkMesher.BlockTypeCount;
            var colors = new Color[]
            {
                Color.clear,                                // Air
                new Color(0.30f, 0.65f, 0.20f),            // Grass
                new Color(0.55f, 0.37f, 0.18f),            // Dirt
                new Color(0.50f, 0.50f, 0.50f),            // Stone
                new Color(0.90f, 0.85f, 0.55f),            // Sand
                new Color(0.20f, 0.20f, 0.20f),            // CoalOre
                new Color(0.70f, 0.55f, 0.45f),            // IronOre
                new Color(0.95f, 0.80f, 0.20f),            // GoldOre
                new Color(0.55f, 0.35f, 0.10f),            // Chest
                new Color(0.10f, 0.85f, 0.95f),            // ExtractionCore
            };

            var tex = new Texture2D(1, count, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Clamp,
                name       = "BlockAtlas_Debug"
            };

            for (int i = 0; i < count; i++)
                tex.SetPixel(0, i, i < colors.Length ? colors[i] : Color.magenta);

            tex.Apply();
            return tex;
        }
    }
}
