using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace EverRealm.Exiles.World
{
    /// <summary>
    /// Pure C# data container for a 16×64×16 block column.
    /// No MonoBehaviour — safe to construct and fill on any thread.
    /// </summary>
    public sealed class Chunk
    {
        public const int Width  = 16;
        public const int Height = 64;
        public const int Depth  = 16;

        /// <summary>Position of this chunk in chunk-space (x = chunkX, y = chunkZ).</summary>
        public Vector2Int ChunkPosition { get; }

        private readonly BlockType[,,] _blocks = new BlockType[Width, Height, Depth];

        // POI metadata sidecar — written during Generate() on a background thread,
        // then read by RegisterBlockEntities() on the main thread. The mesh queue
        // handoff serializes access so no concurrent read/write occurs.
        private List<POIMarker> _poiMarkers;

        public Chunk(Vector2Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
        }

        public BlockType GetBlock(int x, int y, int z) => _blocks[x, y, z];

        public void SetBlock(int x, int y, int z, BlockType type) => _blocks[x, y, z] = type;

        public bool IsInBounds(int x, int y, int z)
            => (uint)x < Width && (uint)y < Height && (uint)z < Depth;

        /// <summary>Add a POI marker during world generation.</summary>
        public void AddPOIMarker(POIMarker marker)
        {
            _poiMarkers ??= new List<POIMarker>(4);
            _poiMarkers.Add(marker);
        }

        /// <summary>Returns POI markers placed in this chunk (empty if none).</summary>
        public IReadOnlyList<POIMarker> GetPOIMarkers()
        {
            return (IReadOnlyList<POIMarker>)_poiMarkers ?? System.Array.Empty<POIMarker>();
        }

        /// <summary>
        /// Copies block data into a flat NativeArray for the Job System.
        /// Index layout: x + z * Width + y * Width * Depth.
        /// </summary>
        public void CopyBlocksFlat(NativeArray<byte> dest)
        {
            for (int y = 0; y < Height; y++)
                for (int z = 0; z < Depth; z++)
                    for (int x = 0; x < Width; x++)
                        dest[x + z * Width + y * Width * Depth] = (byte)_blocks[x, y, z];
        }
    }
}
