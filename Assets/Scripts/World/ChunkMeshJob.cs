using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EverRealm.Exiles.World
{
    /// <summary>
    /// Burst-compiled greedy meshing job. Same algorithm as
    /// <see cref="ChunkMesher.BuildMesh"/> but uses NativeArrays
    /// for zero GC allocation and Burst compilation.
    ///
    /// Index layout for flat block array:
    ///   index = x + z * Width + y * Width * Depth
    /// where Width=16, Height=64, Depth=16.
    /// </summary>
    [BurstCompile]
    public struct ChunkMeshJob : IJob
    {
        private const int W = 16; // Chunk.Width
        private const int H = 64; // Chunk.Height
        private const int D = 16; // Chunk.Depth

        [ReadOnly] public NativeArray<byte> Blocks;
        public int BlockTypeCount;

        public NativeList<float3> Vertices;
        public NativeList<int> Triangles;
        public NativeList<float2> UVs;

        public void Execute()
        {
            // Dimension arrays: [X, Y, Z]
            var dims = new int3(W, H, D);

            // Mask buffer — reused for each slice.
            int maskSize = math.max(W, math.max(H, D));
            maskSize *= maskSize;
            var mask = new NativeArray<byte>(maskSize, Allocator.Temp);

            // 3 axes × 2 directions
            for (int d = 0; d < 3; d++)
            {
                int uAxis = (d + 1) % 3;
                int vAxis = (d + 2) % 3;
                int sliceU = GetDim(dims, uAxis);
                int sliceV = GetDim(dims, vAxis);

                for (int backFace = 0; backFace <= 1; backFace++)
                {
                    bool isBack = backFace == 1;

                    for (int s = 0; s <= GetDim(dims, d); s++)
                    {
                        // Build mask
                        for (int j = 0; j < sliceV; j++)
                        {
                            for (int i = 0; i < sliceU; i++)
                            {
                                byte a = GetBlockAt(d, s - 1, uAxis, i, vAxis, j, dims);
                                byte b = GetBlockAt(d, s,     uAxis, i, vAxis, j, dims);

                                bool aOpaque = a != 0; // Air = 0
                                bool bOpaque = b != 0;

                                mask[j * sliceU + i] = !isBack
                                    ? (byte)(aOpaque && !bOpaque ? a : 0)
                                    : (byte)(!aOpaque && bOpaque ? b : 0);
                            }
                        }

                        // Greedy merge
                        for (int j = 0; j < sliceV; j++)
                        {
                            int i = 0;
                            while (i < sliceU)
                            {
                                byte bt = mask[j * sliceU + i];
                                if (bt == 0) { i++; continue; }

                                // Max width
                                int w = 1;
                                while (i + w < sliceU && mask[j * sliceU + i + w] == bt) w++;

                                // Max height
                                int h = 1;
                                bool heightDone = false;
                                while (!heightDone && j + h < sliceV)
                                {
                                    for (int k = 0; k < w; k++)
                                    {
                                        if (mask[(j + h) * sliceU + i + k] != bt)
                                        {
                                            heightDone = true;
                                            break;
                                        }
                                    }
                                    if (!heightDone) h++;
                                }

                                // Build origin and direction vectors
                                var origin = BuildFloat3(d, s, uAxis, i, vAxis, j);
                                var du = BuildFloat3Dir(uAxis, w);
                                var dv = BuildFloat3Dir(vAxis, h);

                                AddQuad(origin, du, dv, bt, isBack, w, h);

                                // Clear merged region
                                for (int jj = 0; jj < h; jj++)
                                    for (int ii = 0; ii < w; ii++)
                                        mask[(j + jj) * sliceU + i + ii] = 0;

                                i += w;
                            }
                        }
                    }
                }
            }

            mask.Dispose();
        }

        // -----------------------------------------------------------------

        private byte GetBlockAt(int dAxis, int dVal, int uAxis, int uVal, int vAxis, int vVal, int3 dims)
        {
            var p = new int3();
            SetAxis(ref p, dAxis, dVal);
            SetAxis(ref p, uAxis, uVal);
            SetAxis(ref p, vAxis, vVal);

            if ((uint)p.x >= W || (uint)p.y >= H || (uint)p.z >= D) return 0;
            return Blocks[p.x + p.z * W + p.y * W * D];
        }

        private static int GetDim(int3 dims, int axis)
        {
            return axis == 0 ? dims.x : axis == 1 ? dims.y : dims.z;
        }

        private static void SetAxis(ref int3 v, int axis, int val)
        {
            switch (axis) { case 0: v.x = val; break; case 1: v.y = val; break; default: v.z = val; break; }
        }

        private static float3 BuildFloat3(int dAxis, int dVal, int uAxis, int uVal, int vAxis, int vVal)
        {
            var p = new float3();
            switch (dAxis) { case 0: p.x = dVal; break; case 1: p.y = dVal; break; default: p.z = dVal; break; }
            switch (uAxis) { case 0: p.x = uVal; break; case 1: p.y = uVal; break; default: p.z = uVal; break; }
            switch (vAxis) { case 0: p.x = vVal; break; case 1: p.y = vVal; break; default: p.z = vVal; break; }
            return p;
        }

        private static float3 BuildFloat3Dir(int axis, int val)
        {
            return axis == 0 ? new float3(val, 0, 0) : axis == 1 ? new float3(0, val, 0) : new float3(0, 0, val);
        }

        private void AddQuad(float3 origin, float3 du, float3 dv, byte blockType, bool isBack, int quadW, int quadH)
        {
            int vi = Vertices.Length;

            Vertices.Add(origin);
            Vertices.Add(origin + du);
            Vertices.Add(origin + du + dv);
            Vertices.Add(origin + dv);

            float quadU = math.length(du);
            float rowCenter = (blockType + 0.5f) / BlockTypeCount;

            UVs.Add(new float2(0f,    rowCenter));
            UVs.Add(new float2(quadU, rowCenter));
            UVs.Add(new float2(quadU, rowCenter));
            UVs.Add(new float2(0f,    rowCenter));

            if (!isBack)
            {
                Triangles.Add(vi);     Triangles.Add(vi + 1); Triangles.Add(vi + 2);
                Triangles.Add(vi);     Triangles.Add(vi + 2); Triangles.Add(vi + 3);
            }
            else
            {
                Triangles.Add(vi);     Triangles.Add(vi + 2); Triangles.Add(vi + 1);
                Triangles.Add(vi);     Triangles.Add(vi + 3); Triangles.Add(vi + 2);
            }
        }
    }
}
