using System.Collections.Generic;
using UnityEngine;

namespace EverRealm.Exiles.World
{
    /// <summary>
    /// Global registry mapping world-space block positions to their
    /// <see cref="IBlockEntity"/> data. Only special blocks (chests, etc.)
    /// have entries — the vast majority of blocks have zero overhead.
    /// </summary>
    public sealed class BlockEntityManager
    {
        public static BlockEntityManager Instance { get; private set; }

        private readonly Dictionary<Vector3Int, IBlockEntity> _entities = new();

        public BlockEntityManager()
        {
            Instance = this;
        }

        public void Register(IBlockEntity entity)
        {
            _entities[entity.Position] = entity;
        }

        public void Remove(Vector3Int position)
        {
            if (_entities.TryGetValue(position, out var entity))
            {
                entity.OnRemoved();
                _entities.Remove(position);
            }
        }

        public bool TryGet(Vector3Int position, out IBlockEntity entity)
        {
            return _entities.TryGetValue(position, out entity);
        }

        /// <summary>Remove all entities belonging to a given chunk.</summary>
        public void RemoveChunkEntities(Vector2Int chunkPos)
        {
            var toRemove = new List<Vector3Int>();
            int minX = chunkPos.x * Chunk.Width;
            int minZ = chunkPos.y * Chunk.Depth;
            int maxX = minX + Chunk.Width;
            int maxZ = minZ + Chunk.Depth;

            foreach (var kvp in _entities)
            {
                var p = kvp.Key;
                if (p.x >= minX && p.x < maxX && p.z >= minZ && p.z < maxZ)
                    toRemove.Add(p);
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                _entities[toRemove[i]].OnRemoved();
                _entities.Remove(toRemove[i]);
            }
        }

        public void Clear()
        {
            foreach (var entity in _entities.Values)
                entity.OnRemoved();
            _entities.Clear();
        }
    }
}
