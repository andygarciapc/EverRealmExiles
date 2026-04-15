using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
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

        [Header("POI Settings")]
        [SerializeField] private POISettings      _poiSettings;

        [Header("POI Prefabs")]
        [SerializeField] private GameObject        _enemyPrefab;
        [SerializeField] private GameObject        _heavyBrutePrefab;
        [SerializeField] private GameObject        _rangedArcherPrefab;
        [SerializeField] private GameObject        _lootPickupPrefab;

        [Header("Zone Loot Tables")]
        [SerializeField] private LootTable         _safeLootTable;
        [SerializeField] private LootTable         _mediumLootTable;
        [SerializeField] private LootTable         _highLootTable;

        private WorldGenerator  _generator;
        private BlockEntityManager _blockEntities;
        private CancellationTokenSource _cts;
        private int _biomeDifficultyTier = 1;

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

        // In-flight Burst mesh jobs (Job System path)
        private readonly List<ChunkMesher.MeshJobData> _pendingJobs = new();

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
                _biomeDifficultyTier = biome.DifficultyTier;
                var biomeSettings = _settings.WithBiome(biome);
                _generator = new WorldGenerator(
                    biomeSettings, biome.SurfaceBlock, biome.SubSurfaceBlock,
                    _poiSettings, _settings.ChunkRadius, biome.HasTrees);
                Debug.Log($"[WorldManager] Using biome '{biome.BiomeName}' (difficulty {biome.DifficultyTier}) for world generation.");
            }
            else
            {
                _generator = new WorldGenerator(
                    _settings, chunkRadius: _settings.ChunkRadius, poiSettings: _poiSettings);
            }
        }

        private void Start()
        {
            // Populate the chunk material with a generated atlas if no texture is assigned.
            if (_chunkMaterial != null && _chunkMaterial.mainTexture == null)
            {
                _chunkMaterial.mainTexture = GenerateDebugAtlas();
                _chunkMaterial.mainTextureScale = Vector2.one;
            }
        }

        private void Update()
        {
            ProcessMeshQueue();
            UpdateStreaming();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            _cts.Cancel();
            _cts.Dispose();

            // Dispose any in-flight mesh jobs to prevent native memory leaks.
            for (int i = 0; i < _pendingJobs.Count; i++)
                ChunkMesher.DisposeJobData(_pendingJobs[i]);
            _pendingJobs.Clear();
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
            // 1. Complete any finished Burst mesh jobs.
            for (int i = _pendingJobs.Count - 1; i >= 0; i--)
            {
                var jobData = _pendingJobs[i];
                if (!jobData.Handle.IsCompleted) continue;

                _pendingJobs.RemoveAt(i);

                Mesh mesh        = ChunkMesher.CompleteMesh(jobData);
                Chunk chunk      = jobData.Chunk;
                ChunkRenderer cr = SpawnChunkRenderer(chunk, mesh);
                _activeChunks[chunk.ChunkPosition] = cr;
                _chunkData[chunk.ChunkPosition]    = chunk;

                RegisterBlockEntities(chunk);
                ProcessPOIMarkers(chunk);
            }

            // 2. Schedule new jobs (or build synchronously as fallback).
            int scheduled = 0;
            while (scheduled < _settings.MeshPerFrame && _meshQueue.TryDequeue(out var chunk))
            {
                _pendingChunks.Remove(chunk.ChunkPosition);

                if (ChunkMesher.UseJobs)
                {
                    _pendingJobs.Add(ChunkMesher.ScheduleJob(chunk));
                }
                else
                {
                    Mesh mesh        = ChunkMesher.BuildMesh(chunk);
                    ChunkRenderer cr = SpawnChunkRenderer(chunk, mesh);
                    _activeChunks[chunk.ChunkPosition] = cr;
                    _chunkData[chunk.ChunkPosition]    = chunk;

                    RegisterBlockEntities(chunk);
                    ProcessPOIMarkers(chunk);
                }
                scheduled++;
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

                            case BlockType.TreasureCacheCore:
                                var zone = MapLayout.GetZone(worldPos.x, worldPos.z, _settings.ChunkRadius);
                                _blockEntities.Register(new ChestBlockEntity(
                                    worldPos,
                                    GetLootTableForZone(zone),
                                    GetRollCountForZone(zone),
                                    _lootPickupPrefab));
                                break;

                            case BlockType.EnemyCampCore:
                                // Visual marker only — enemies spawned via POI markers below.
                                break;
                        }
                    }
        }

        /// <summary>
        /// Processes POI markers from a newly loaded chunk to spawn runtime
        /// GameObjects (enemies, etc.) on the main thread.
        /// </summary>
        private void ProcessPOIMarkers(Chunk chunk)
        {
            var markers = chunk.GetPOIMarkers();
            for (int i = 0; i < markers.Count; i++)
            {
                var poi = markers[i];
                switch (poi.Type)
                {
                    case POIType.EnemyCamp:
                        SpawnEnemyCamp(poi);
                        break;

                    case POIType.TreasureCache:
                        // Handled by block entity registration above.
                        break;

                    case POIType.DungeonEntrance:
                        Debug.Log($"[WorldManager] Dungeon entrance stub at ({poi.WorldX}, {poi.WorldZ})");
                        break;
                }
            }
        }

        // -------------------------------------------------------------------------
        // POI spawning

        private void SpawnEnemyCamp(POIMarker poi)
        {
            if (_enemyPrefab == null) return;

            int baseCount = _poiSettings != null
                ? poi.Zone switch
                {
                    RiskZone.Medium => _poiSettings.MediumZoneEnemies,
                    RiskZone.High   => _poiSettings.HighZoneEnemies,
                    _               => _poiSettings.SafeZoneEnemies
                }
                : poi.Zone switch
                {
                    RiskZone.Medium => 2,
                    RiskZone.High   => 4,
                    _               => 1
                };

            // Scale by biome difficulty: tier 1 = 1x, tier 5 = 3x.
            float difficultyScale = 1f + (_biomeDifficultyTier - 1) * 0.5f;
            int enemyCount = Mathf.Max(1, Mathf.RoundToInt(baseCount * difficultyScale));

            for (int i = 0; i < enemyCount; i++)
            {
                // Distribute enemies in a ring around the camp center.
                float angle = (i / (float)enemyCount) * Mathf.PI * 2f;
                float dist = 3f + i * 1.5f;
                float spawnX = poi.WorldX + 0.5f + Mathf.Cos(angle) * dist;
                float spawnZ = poi.WorldZ + 0.5f + Mathf.Sin(angle) * dist;

                int sx = Mathf.FloorToInt(spawnX);
                int sz = Mathf.FloorToInt(spawnZ);
                int spawnY = GetSurfaceY(sx, sz);
                if (spawnY < 0) continue;

                var pos = new Vector3(spawnX, spawnY + 0.5f, spawnZ);
                var prefab = PickEnemyPrefab(poi.Zone, i, poi.WorldX, poi.WorldZ);
                if (prefab != null)
                    Instantiate(prefab, pos, Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f));
            }
        }

        /// <summary>
        /// Picks an enemy prefab based on zone and a deterministic hash.
        /// Medium zones mix in archers and occasional brutes.
        /// High zones have roughly equal variety.
        /// </summary>
        private GameObject PickEnemyPrefab(RiskZone zone, int enemyIndex, int campX, int campZ)
        {
            // Deterministic variety per enemy per camp.
            int hash = campX * 374761393 + campZ * 668265263 + enemyIndex * 1274126177;
            float roll = ((hash & 0x7FFFFFFF) / (float)0x7FFFFFFF);

            switch (zone)
            {
                case RiskZone.Medium:
                    // 60% Grunt, 25% Archer, 15% Brute
                    if (roll < 0.60f) return _enemyPrefab;
                    if (roll < 0.85f) return _rangedArcherPrefab != null ? _rangedArcherPrefab : _enemyPrefab;
                    return _heavyBrutePrefab != null ? _heavyBrutePrefab : _enemyPrefab;

                case RiskZone.High:
                    // 35% Grunt, 35% Archer, 30% Brute
                    if (roll < 0.35f) return _enemyPrefab;
                    if (roll < 0.70f) return _rangedArcherPrefab != null ? _rangedArcherPrefab : _enemyPrefab;
                    return _heavyBrutePrefab != null ? _heavyBrutePrefab : _enemyPrefab;

                default:
                    return _enemyPrefab;
            }
        }

        private LootTable GetLootTableForZone(RiskZone zone)
        {
            return zone switch
            {
                RiskZone.High   => _highLootTable,
                RiskZone.Medium => _mediumLootTable,
                _               => _safeLootTable
            };
        }

        private int GetRollCountForZone(RiskZone zone)
        {
            if (_poiSettings == null)
                return zone switch { RiskZone.High => 5, RiskZone.Medium => 3, _ => 2 };

            return zone switch
            {
                RiskZone.High   => _poiSettings.HighZoneLootRolls,
                RiskZone.Medium => _poiSettings.MediumZoneLootRolls,
                _               => _poiSettings.SafeZoneLootRolls
            };
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
        /// Creates a 16×N texture with one colour per BlockType plus subtle
        /// per-pixel noise for visual depth. Material wrap mode is set to
        /// Repeat so the pattern tiles across greedy-merged faces.
        /// </summary>
        public static Texture2D GenerateDebugAtlas()
        {
            int count  = ChunkMesher.BlockTypeCount;
            const int tileWidth = 16; // pixels per row — gives tiling detail

            var baseColors = new Color[]
            {
                Color.clear,                                // 0  Air
                new Color(0.30f, 0.65f, 0.20f),            // 1  Grass
                new Color(0.55f, 0.37f, 0.18f),            // 2  Dirt
                new Color(0.50f, 0.50f, 0.50f),            // 3  Stone
                new Color(0.90f, 0.85f, 0.55f),            // 4  Sand
                new Color(0.20f, 0.20f, 0.20f),            // 5  CoalOre
                new Color(0.70f, 0.55f, 0.45f),            // 6  IronOre
                new Color(0.95f, 0.80f, 0.20f),            // 7  GoldOre
                new Color(0.55f, 0.35f, 0.10f),            // 8  Chest
                new Color(0.10f, 0.85f, 0.95f),            // 9  ExtractionCore
                new Color(0.70f, 0.15f, 0.15f),            // 10 EnemyCampCore
                new Color(0.85f, 0.70f, 0.10f),            // 11 TreasureCacheCore
                new Color(0.45f, 0.28f, 0.13f),            // 12 Wood
                new Color(0.18f, 0.50f, 0.12f),            // 13 Leaves
                new Color(0.92f, 0.95f, 0.98f),            // 14 Snow
            };

            var tex = new Texture2D(tileWidth, count, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Repeat,
                name       = "BlockAtlas"
            };

            for (int row = 0; row < count; row++)
            {
                Color baseCol = row < baseColors.Length ? baseColors[row] : Color.magenta;
                for (int x = 0; x < tileWidth; x++)
                {
                    // Subtle per-pixel noise dithering for visual depth.
                    float noise = Mathf.PerlinNoise(x * 0.8f + row * 13.7f, row * 7.3f + x * 0.5f);
                    float variation = (noise - 0.5f) * 0.08f; // ±4% brightness
                    Color c = new Color(
                        Mathf.Clamp01(baseCol.r + variation),
                        Mathf.Clamp01(baseCol.g + variation),
                        Mathf.Clamp01(baseCol.b + variation),
                        baseCol.a
                    );
                    tex.SetPixel(x, row, c);
                }
            }

            tex.Apply();
            return tex;
        }
    }
}
