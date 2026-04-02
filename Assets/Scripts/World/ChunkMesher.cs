using System;
using System.Collections.Generic;
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

        private static readonly int[] ChunkDims = { Chunk.Width, Chunk.Height, Chunk.Depth };

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

            // Simple atlas UV: each block type is one row in a Nx1 texture (height = BlockTypeCount).
            // u = 0.5 (single column), v = (row + 0.5) / BlockTypeCount.
            float atlasV = ((int)blockType + 0.5f) / BlockTypeCount;
            var uv = new Vector2(0.5f, atlasV);
            uvs.Add(uv); uvs.Add(uv); uvs.Add(uv); uvs.Add(uv);

            // Winding: front faces use CCW (Unity default), back faces are reversed.
            if (!isBack)
            {
                tris.Add(vi); tris.Add(vi + 3); tris.Add(vi + 2);
                tris.Add(vi); tris.Add(vi + 2); tris.Add(vi + 1);
            }
            else
            {
                tris.Add(vi); tris.Add(vi + 1); tris.Add(vi + 2);
                tris.Add(vi); tris.Add(vi + 2); tris.Add(vi + 3);
            }
        }
    }
}
