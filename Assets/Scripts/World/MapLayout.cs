using UnityEngine;

namespace EverRealm.Exiles.World
{
    /// <summary>
    /// Risk zones for map regions. Affects enemy density and loot quality.
    /// </summary>
    public enum RiskZone : byte
    {
        Safe,
        Medium,
        High
    }

    /// <summary>
    /// Divides the map into concentric risk zones around the center.
    /// Pure static math — safe to call from any thread.
    ///
    /// Zone layout (fraction of map radius from center):
    ///   Inner 40%  → Safe   (spawn area, light resistance)
    ///   40% – 70%  → Medium (moderate enemies, decent loot)
    ///   Outer 70%+ → High   (dense camps, best loot)
    /// </summary>
    public static class MapLayout
    {
        private const float SafeThreshold   = 0.40f;
        private const float MediumThreshold = 0.70f;

        /// <summary>
        /// Returns the risk zone for a world-space position.
        /// </summary>
        public static RiskZone GetZone(int wx, int wz, int chunkRadius)
        {
            float dist = DistanceFromCenter(wx, wz, chunkRadius);
            float radius = MapHalfSize(chunkRadius);
            if (radius <= 0f) return RiskZone.Safe;

            float t = dist / radius;

            if (t <= SafeThreshold)   return RiskZone.Safe;
            if (t <= MediumThreshold) return RiskZone.Medium;
            return RiskZone.High;
        }

        /// <summary>Map center in world coordinates (origin-centered map).</summary>
        public static Vector2Int MapCenter(int chunkRadius)
        {
            // Map spans from chunk -chunkRadius to +chunkRadius.
            // Center is at the midpoint: chunk 0 center = block 8.
            return new Vector2Int(Chunk.Width / 2, Chunk.Depth / 2);
        }

        /// <summary>Half-size of the playable map in blocks.</summary>
        public static float MapHalfSize(int chunkRadius)
        {
            return chunkRadius * Chunk.Width;
        }

        /// <summary>
        /// Returns true if (wx, wz) is within the playable map boundary.
        /// Boundary is inset 2 blocks from the chunk edge.
        /// </summary>
        public static bool IsInBounds(int wx, int wz, int chunkRadius)
        {
            int half = chunkRadius * Chunk.Width;
            const int inset = 2;
            return wx >= -half + inset && wx < half + Chunk.Width - inset
                && wz >= -half + inset && wz < half + Chunk.Depth - inset;
        }

        private static float DistanceFromCenter(int wx, int wz, int chunkRadius)
        {
            var center = MapCenter(chunkRadius);
            float dx = wx - center.x;
            float dz = wz - center.y;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
