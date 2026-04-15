using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace EverRealm.Exiles.World
{
    /// <summary>
    /// Builds a Unity Mesh from Chunk data using greedy face-merging.
    ///
    /// Algorithm: for each of the 6 face directions, sweep through axis-aligned
    /// slices. Build a 2-D mask of exposed faces, then greedily merge adjacent
    /// same-type faces into the largest possible quads.
    ///
    /// UV layout: each block type occupies one row in a vertical texture atlas
    /// (row index = (int)BlockType). Create a Texture2D with one column and
    /// <see cref="BlockTypeCount"/> rows, one colour per block type.
    ///
    /// Call only from the main thread — Mesh creation is not thread-safe.
    /// PROTOTYPE: move ChunkMesher to a NativeArray + Job System in Phase 11.
    /// </summary>
    public static class ChunkMesher
    {
        public static readonly int BlockTypeCount = Enum.GetValues(typeof(BlockType)).Length;

        /// <summary>Toggle to fall back to synchronous meshing if Burst is unavailable.</summary>
        public static bool UseJobs = true;

        private static readonly int[] ChunkDims = { Chunk.Width, Chunk.Height, Chunk.Depth };

        // -----------------------------------------------------------------
        // Job System path
        // -----------------------------------------------------------------

        /// <summary>Holds native buffers for an in-flight mesh job.</summary>
        public struct MeshJobData
        {
            public Chunk Chunk;
            public JobHandle Handle;
            public NativeArray<byte> Blocks;
            public NativeList<float3> Vertices;
            public NativeList<int> Triangles;
            public NativeList<float2> UVs;
        }

        /// <summary>
        /// Flattens chunk block data into a NativeArray and schedules a
        /// Burst-compiled <see cref="ChunkMeshJob"/>. Call <see cref="CompleteMesh"/>
        /// after the job finishes to create the Mesh and dispose native buffers.
        /// </summary>
        public static MeshJobData ScheduleJob(Chunk chunk)
        {
            int totalBlocks = Chunk.Width * Chunk.Height * Chunk.Depth;
            var blocks   = new NativeArray<byte>(totalBlocks, Allocator.TempJob);
            var vertices = new NativeList<float3>(2048, Allocator.TempJob);
            var tris     = new NativeList<int>(4096, Allocator.TempJob);
            var uvs      = new NativeList<float2>(2048, Allocator.TempJob);

            chunk.CopyBlocksFlat(blocks);

            var job = new ChunkMeshJob
            {
                Blocks         = blocks,
                BlockTypeCount = BlockTypeCount,
                Vertices       = vertices,
                Triangles      = tris,
                UVs            = uvs
            };

            return new MeshJobData
            {
                Chunk    = chunk,
                Handle   = job.Schedule(),
                Blocks   = blocks,
                Vertices = vertices,
                Triangles = tris,
                UVs      = uvs
            };
        }

        /// <summary>
        /// Completes a scheduled mesh job, creates a Mesh from the results,
        /// and disposes all native buffers.
        /// </summary>
        public static Mesh CompleteMesh(MeshJobData data)
        {
            data.Handle.Complete();

            int vertCount = data.Vertices.Length;
            int triCount  = data.Triangles.Length;

            var mesh = new Mesh { name = "ChunkMesh" };
            if (vertCount > 65535) mesh.indexFormat = IndexFormat.UInt32;

            // Copy NativeList → managed arrays for Mesh API.
            var verts = new Vector3[vertCount];
            var uvArr = new Vector2[vertCount];
            for (int i = 0; i < vertCount; i++)
            {
                var v  = data.Vertices[i];
                verts[i] = new Vector3(v.x, v.y, v.z);
                var uv = data.UVs[i];
                uvArr[i] = new Vector2(uv.x, uv.y);
            }

            var triArr = new int[triCount];
            for (int i = 0; i < triCount; i++)
                triArr[i] = data.Triangles[i];

            mesh.vertices  = verts;
            mesh.triangles = triArr;
            mesh.uv        = uvArr;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            data.Blocks.Dispose();
            data.Vertices.Dispose();
            data.Triangles.Dispose();
            data.UVs.Dispose();

            return mesh;
        }

        /// <summary>Disposes native buffers without creating a mesh (cleanup on destroy).</summary>
        public static void DisposeJobData(MeshJobData data)
        {
            data.Handle.Complete();
            data.Blocks.Dispose();
            data.Vertices.Dispose();
            data.Triangles.Dispose();
            data.UVs.Dispose();
        }

        // -----------------------------------------------------------------
        // Synchronous fallback path
        // -----------------------------------------------------------------

        public static Mesh BuildMesh(Chunk chunk)
        {
            var verts = new List<Vector3>(2048);
            var tris  = new List<int>(4096);
            var uvs   = new List<Vector2>(2048);

            // Mask is reused for each slice; size is the largest possible 2-D slice.
            int maskSize = Mathf.Max(Chunk.Width, Mathf.Max(Chunk.Height, Chunk.Depth));
            maskSize *= maskSize;
            var mask = new BlockType[maskSize];

            // 3 axes × 2 directions (front/back)
            for (int d = 0; d < 3; d++)
            {
                int uAxis = (d + 1) % 3;
                int vAxis = (d + 2) % 3;
                int sliceU = ChunkDims[uAxis];
                int sliceV = ChunkDims[vAxis];

                for (int backFace = 0; backFace <= 1; backFace++)
                {
                    bool isBack = backFace == 1;

                    // Iterate over slice boundaries: 0 … Dims[d]
                    // Boundary s lies between block (s-1) and block (s).
                    for (int s = 0; s <= ChunkDims[d]; s++)
                    {
                        // ----- Build mask -----
                        for (int j = 0; j < sliceV; j++)
                        {
                            for (int i = 0; i < sliceU; i++)
                            {
                                int[] pa = BuildPos(d, s - 1, uAxis, i, vAxis, j);
                                int[] pb = BuildPos(d, s,     uAxis, i, vAxis, j);

                                BlockType a = InBounds(pa) ? chunk.GetBlock(pa[0], pa[1], pa[2]) : BlockType.Air;
                                BlockType b = InBounds(pb) ? chunk.GetBlock(pb[0], pb[1], pb[2]) : BlockType.Air;

                                bool aOpaque = a != BlockType.Air;
                                bool bOpaque = b != BlockType.Air;

                                // Front face: solid block on the inside (A), air on the outside (B).
                                // Back face: air on the inside (A), solid on the outside (B).
                                mask[j * sliceU + i] = !isBack
                                    ? (aOpaque && !bOpaque ? a : BlockType.Air)
                                    : (!aOpaque && bOpaque ? b : BlockType.Air);
                            }
                        }

                        // ----- Greedy merge -----
                        for (int j = 0; j < sliceV; j++)
                        {
                            for (int i = 0; i < sliceU; )
                            {
                                BlockType bt = mask[j * sliceU + i];
                                if (bt == BlockType.Air) { i++; continue; }

                                // Maximum width in u direction
                                int w = 1;
                                while (i + w < sliceU && mask[j * sliceU + i + w] == bt)
                                    w++;

                                // Maximum height in v direction
                                int h = 1;
                                bool heightDone = false;
                                while (!heightDone && j + h < sliceV)
                                {
                                    for (int k = 0; k < w; k++)
                                        if (mask[(j + h) * sliceU + i + k] != bt) { heightDone = true; break; }
                                    if (!heightDone) h++;
                                }

                                // Emit quad — face plane is at d = s
                                int[] origin = BuildPos(d, s, uAxis, i, vAxis, j);
                                int[] du     = new int[3]; du[uAxis] = w;
                                int[] dv     = new int[3]; dv[vAxis] = h;
                                AddQuad(verts, tris, uvs, origin, du, dv, bt, isBack);

                                // Clear merged region
                                for (int jj = 0; jj < h; jj++)
                                    for (int ii = 0; ii < w; ii++)
                                        mask[(j + jj) * sliceU + i + ii] = BlockType.Air;

                                i += w;
                            }
                        }
                    }
                }
            }

            var mesh = new Mesh { name = "ChunkMesh" };
            if (verts.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // -------------------------------------------------------------------------

        private static int[] BuildPos(int d, int dVal, int u, int uVal, int v, int vVal)
        {
            var p = new int[3];
            p[d] = dVal;
            p[u] = uVal;
            p[v] = vVal;
            return p;
        }

        private static bool InBounds(int[] p)
            => (uint)p[0] < Chunk.Width && (uint)p[1] < Chunk.Height && (uint)p[2] < Chunk.Depth;

        private static void AddQuad(
            List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            int[] origin, int[] du, int[] dv,
            BlockType blockType, bool isBack)
        {
            int vi = verts.Count;

            var v0 = new Vector3(origin[0], origin[1], origin[2]);
            var duV = new Vector3(du[0], du[1], du[2]);
            var dvV = new Vector3(dv[0], dv[1], dv[2]);

            verts.Add(v0);
            verts.Add(v0 + duV);
            verts.Add(v0 + duV + dvV);
            verts.Add(v0 + dvV);

            // Tiling atlas UV: the atlas is N-pixels wide × BlockTypeCount rows.
            // U spans [0, quadWidth] so the texture pattern tiles per block face.
            // V selects the block type row center. Material wrap mode = Repeat.
            float quadU = duV.magnitude; // width in blocks — tiles in U
            float quadVSize = dvV.magnitude; // height in blocks — tiles in V
            float rowCenter = ((int)blockType + 0.5f) / BlockTypeCount;

            uvs.Add(new Vector2(0f,       rowCenter));
            uvs.Add(new Vector2(quadU,    rowCenter));
            uvs.Add(new Vector2(quadU,    rowCenter));
            uvs.Add(new Vector2(0f,       rowCenter));

            // Unity: cross product of winding = front-face normal direction.
            // !isBack needs normal in +d direction; isBack needs -d.
            if (!isBack)
            {
                tris.Add(vi);     tris.Add(vi + 1); tris.Add(vi + 2);
                tris.Add(vi);     tris.Add(vi + 2); tris.Add(vi + 3);
            }
            else
            {
                tris.Add(vi);     tris.Add(vi + 2); tris.Add(vi + 1);
                tris.Add(vi);     tris.Add(vi + 3); tris.Add(vi + 2);
            }
        }
    }
}
