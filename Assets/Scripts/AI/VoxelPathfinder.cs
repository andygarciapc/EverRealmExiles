using System.Collections.Generic;
using UnityEngine;
using EverRealm.Exiles.World;

namespace EverRealm.Exiles.AI
{
    /// <summary>
    /// A* pathfinder on the voxel block grid.
    ///
    /// A node is walkable if its block is Air and the block below is solid.
    /// Agents can step up/down 1 block and move in 4 cardinal directions.
    /// </summary>
    public static class VoxelPathfinder
    {
        private const int MaxIterations = 2048;
        private const int MaxJumpUp    = 2;  // Can jump up 2 blocks.
        private const int MaxDropDown  = 3;  // Can drop down 3 blocks.

        private struct Node
        {
            public Vector3Int Pos;
            public float G; // cost from start
            public float H; // heuristic to goal
            public float F => G + H;
        }

        private static readonly Vector2Int[] CardinalDirs =
        {
            new( 1, 0),
            new(-1, 0),
            new( 0, 1),
            new( 0,-1),
        };

        /// <summary>
        /// Finds a path from <paramref name="start"/> to <paramref name="goal"/>
        /// (both world-space positions). Returns a list of world-space block centres
        /// to walk through, or null if no path exists.
        /// </summary>
        public static List<Vector3> FindPath(Vector3 start, Vector3 goal)
        {
            var wm = WorldManager.Instance;
            if (wm == null) return null;

            int sx = Mathf.FloorToInt(start.x);
            int sz = Mathf.FloorToInt(start.z);
            int sy = wm.GetSurfaceY(sx, sz, Mathf.RoundToInt(start.y) + 2);
            if (sy < 0) return null;

            int gx = Mathf.FloorToInt(goal.x);
            int gz = Mathf.FloorToInt(goal.z);
            int gy = wm.GetSurfaceY(gx, gz, Mathf.RoundToInt(goal.y) + 2);
            if (gy < 0) return null;

            var startNode = new Vector3Int(sx, sy, sz);
            var goalNode  = new Vector3Int(gx, gy, gz);

            if (startNode == goalNode)
                return new List<Vector3> { BlockCenter(goalNode) };

            var open     = new List<(float f, int id, Vector3Int pos)>();
            var gScore   = new Dictionary<Vector3Int, float>();
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            int nextId   = 0;

            float h = Heuristic(startNode, goalNode);
            gScore[startNode] = 0f;
            open.Add((h, nextId++, startNode));

            int iterations = 0;
            while (open.Count > 0 && iterations++ < MaxIterations)
            {
                // Find the entry with the lowest F (then lowest id as tiebreak).
                int bestIdx = 0;
                for (int i = 1; i < open.Count; i++)
                {
                    if (open[i].f < open[bestIdx].f ||
                       (open[i].f == open[bestIdx].f && open[i].id < open[bestIdx].id))
                        bestIdx = i;
                }
                var (_, _, current) = open[bestIdx];
                open.RemoveAt(bestIdx);

                if (current == goalNode)
                    return ReconstructPath(cameFrom, current);

                float currentG = gScore[current];

                for (int d = 0; d < 4; d++)
                {
                    int nx = current.x + CardinalDirs[d].x;
                    int nz = current.z + CardinalDirs[d].y;

                    // Search for a walkable Y within step height range.
                    int ny = FindWalkableNeighborY(wm, nx, nz, current.y);
                    if (ny < 0) continue;

                    var neighbor = new Vector3Int(nx, ny, nz);
                    int heightDiff = Mathf.Abs(ny - current.y);
                    float tentativeG = currentG + 1f + heightDiff * 2f; // Penalise height changes.

                    if (gScore.TryGetValue(neighbor, out float existingG) && tentativeG >= existingG)
                        continue;

                    gScore[neighbor]  = tentativeG;
                    cameFrom[neighbor] = current;
                    float nf = tentativeG + Heuristic(neighbor, goalNode);
                    open.Add((nf, nextId++, neighbor));
                }
            }

            return null; // No path found.
        }

        private static int FindWalkableNeighborY(WorldManager wm, int wx, int wz, int fromY)
        {
            // Same level first (most common).
            if (IsTraversable(wm, wx, fromY, wz)) return fromY;

            // Jump up.
            for (int dy = 1; dy <= MaxJumpUp; dy++)
            {
                if (IsTraversable(wm, wx, fromY + dy, wz)) return fromY + dy;
            }

            // Drop down.
            for (int dy = 1; dy <= MaxDropDown; dy++)
            {
                if (IsTraversable(wm, wx, fromY - dy, wz)) return fromY - dy;
            }

            return -1;
        }

        /// <summary>
        /// A position is traversable if the feet block and the block above (head)
        /// are both Air, and the block below (floor) is solid.
        /// </summary>
        private static bool IsTraversable(WorldManager wm, int wx, int wy, int wz)
        {
            return wy > 0
                && wm.GetBlock(wx, wy, wz)     == BlockType.Air  // feet
                && wm.GetBlock(wx, wy + 1, wz) == BlockType.Air  // head
                && wm.GetBlock(wx, wy - 1, wz) != BlockType.Air; // floor
        }

        private static float Heuristic(Vector3Int a, Vector3Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z) + Mathf.Abs(a.y - b.y) * 0.5f;
        }

        private static List<Vector3> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
        {
            var path = new List<Vector3> { BlockCenter(current) };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(BlockCenter(current));
            }
            path.Reverse();
            return path;
        }

        /// <summary>Returns the world-space centre of the walkable surface at a block position.</summary>
        private static Vector3 BlockCenter(Vector3Int pos)
        {
            return new Vector3(pos.x + 0.5f, pos.y, pos.z + 0.5f);
        }
    }
}
