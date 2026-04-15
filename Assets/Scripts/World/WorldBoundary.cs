using UnityEngine;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.World
{
    /// <summary>
    /// Creates invisible collision walls at the edges of the playable map.
    /// Add to the same GameObject as WorldManager and assign _settings.
    /// </summary>
    public sealed class WorldBoundary : MonoBehaviour
    {
        [SerializeField] private WorldGenSettings _settings;

        private void Start()
        {
            if (_settings == null)
            {
                Debug.LogWarning("[WorldBoundary] No WorldGenSettings assigned.");
                return;
            }
            CreateBoundaryWalls();
        }

        private void CreateBoundaryWalls()
        {
            int half = _settings.ChunkRadius * Chunk.Width;
            float wallHeight = Chunk.Height;
            const float thickness = 2f;

            // Map spans from -half to +half (approximately) in both X and Z.
            // Center the walls at the map edges.
            float minEdge = -half;
            float maxEdge = half + Chunk.Width; // account for chunk (chunkRadius, ...) extending 16 blocks
            float centerX = (minEdge + maxEdge) * 0.5f;
            float centerZ = (minEdge + maxEdge) * 0.5f;
            float spanX = maxEdge - minEdge;
            float spanZ = maxEdge - minEdge;

            // North wall (positive Z edge)
            CreateWall("Boundary_North",
                new Vector3(centerX, wallHeight * 0.5f, maxEdge + thickness * 0.5f),
                new Vector3(spanX + thickness * 2f, wallHeight, thickness));

            // South wall (negative Z edge)
            CreateWall("Boundary_South",
                new Vector3(centerX, wallHeight * 0.5f, minEdge - thickness * 0.5f),
                new Vector3(spanX + thickness * 2f, wallHeight, thickness));

            // East wall (positive X edge)
            CreateWall("Boundary_East",
                new Vector3(maxEdge + thickness * 0.5f, wallHeight * 0.5f, centerZ),
                new Vector3(thickness, wallHeight, spanZ + thickness * 2f));

            // West wall (negative X edge)
            CreateWall("Boundary_West",
                new Vector3(minEdge - thickness * 0.5f, wallHeight * 0.5f, centerZ),
                new Vector3(thickness, wallHeight, spanZ + thickness * 2f));

            Debug.Log($"[WorldBoundary] Created boundary walls. Map range: ({minEdge}, {minEdge}) to ({maxEdge}, {maxEdge})");
        }

        private void CreateWall(string wallName, Vector3 center, Vector3 size)
        {
            var go = new GameObject(wallName);
            go.transform.SetParent(transform, false);
            go.transform.position = center;
            var col = go.AddComponent<BoxCollider>();
            col.size = size;
        }
    }
}
