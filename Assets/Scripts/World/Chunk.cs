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

        public Chunk(Vector2Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
        }

        public BlockType GetBlock(int x, int y, int z) => _blocks[x, y, z];

        public void SetBlock(int x, int y, int z, BlockType type) => _blocks[x, y, z] = type;

        public bool IsInBounds(int x, int y, int z)
            => (uint)x < Width && (uint)y < Height && (uint)z < Depth;
    }
}
