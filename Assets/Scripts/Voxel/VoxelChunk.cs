using UnityEngine;
using System.Collections.Generic;

namespace EverRealmExiles.Voxel
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class VoxelChunk : MonoBehaviour
    {
        public const int CHUNK_SIZE = 16;
        public const int CHUNK_HEIGHT = 128;

        public Vector3Int chunkPosition;
        public VoxelWorld world;

        private BlockType[,,] blocks;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        private List<Vector3> vertices = new List<Vector3>();
        private List<int> triangles = new List<int>();
        private List<Vector2> uvs = new List<Vector2>();
        private List<Color> colors = new List<Color>();

        private bool isDirty = true;
        private bool isGenerated = false;

        private const int TEXTURE_ATLAS_SIZE = 16;
        private const float TEXTURE_UNIT = 1f / TEXTURE_ATLAS_SIZE;

        public void Initialize(Vector3Int position, VoxelWorld voxelWorld)
        {
            chunkPosition = position;
            world = voxelWorld;
            blocks = new BlockType[CHUNK_SIZE, CHUNK_HEIGHT, CHUNK_SIZE];

            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();

            transform.position = new Vector3(
                position.x * CHUNK_SIZE,
                0,
                position.z * CHUNK_SIZE
            );
        }

        public void SetBlock(int x, int y, int z, BlockType type)
        {
            if (IsInBounds(x, y, z))
            {
                blocks[x, y, z] = type;
                isDirty = true;
            }
        }

        public BlockType GetBlock(int x, int y, int z)
        {
            if (IsInBounds(x, y, z))
                return blocks[x, y, z];
            return BlockType.Air;
        }

        public BlockType GetBlockWorldPosition(int worldX, int worldY, int worldZ)
        {
            int localX = worldX - chunkPosition.x * CHUNK_SIZE;
            int localZ = worldZ - chunkPosition.z * CHUNK_SIZE;
            return GetBlock(localX, worldY, localZ);
        }

        private bool IsInBounds(int x, int y, int z)
        {
            return x >= 0 && x < CHUNK_SIZE &&
                   y >= 0 && y < CHUNK_HEIGHT &&
                   z >= 0 && z < CHUNK_SIZE;
        }

        public void MarkDirty()
        {
            isDirty = true;
        }

        public void SetGenerated(bool generated)
        {
            isGenerated = generated;
        }

        public bool IsGenerated => isGenerated;

        public void GenerateMesh()
        {
            if (!isDirty) return;

            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
            colors.Clear();

            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                for (int y = 0; y < CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < CHUNK_SIZE; z++)
                    {
                        BlockType block = blocks[x, y, z];
                        if (block != BlockType.Air)
                        {
                            AddBlockMesh(x, y, z, block);
                        }
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.colors = colors.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;

            isDirty = false;
        }

        private void AddBlockMesh(int x, int y, int z, BlockType block)
        {
            BlockData data = BlockDatabase.GetBlockData(block);
            Vector3 pos = new Vector3(x, y, z);

            // Check each face
            if (ShouldRenderFace(x, y + 1, z)) AddFace(pos, Direction.Top, data.textureTop, data.isTransparent);
            if (ShouldRenderFace(x, y - 1, z)) AddFace(pos, Direction.Bottom, data.textureBottom, data.isTransparent);
            if (ShouldRenderFace(x - 1, y, z)) AddFace(pos, Direction.Left, data.textureSide, data.isTransparent);
            if (ShouldRenderFace(x + 1, y, z)) AddFace(pos, Direction.Right, data.textureSide, data.isTransparent);
            if (ShouldRenderFace(x, y, z + 1)) AddFace(pos, Direction.Front, data.textureSide, data.isTransparent);
            if (ShouldRenderFace(x, y, z - 1)) AddFace(pos, Direction.Back, data.textureSide, data.isTransparent);
        }

        private bool ShouldRenderFace(int x, int y, int z)
        {
            BlockType neighbor;

            if (IsInBounds(x, y, z))
            {
                neighbor = blocks[x, y, z];
            }
            else if (world != null)
            {
                int worldX = chunkPosition.x * CHUNK_SIZE + x;
                int worldZ = chunkPosition.z * CHUNK_SIZE + z;
                neighbor = world.GetBlock(worldX, y, worldZ);
            }
            else
            {
                return true;
            }

            return !BlockDatabase.IsSolid(neighbor) || BlockDatabase.IsTransparent(neighbor);
        }

        private enum Direction { Top, Bottom, Left, Right, Front, Back }

        private void AddFace(Vector3 pos, Direction dir, int textureIndex, bool isTransparent)
        {
            int vertCount = vertices.Count;
            float ao = 1f;
            Color vertColor = new Color(ao, ao, ao, isTransparent ? 0.8f : 1f);

            Vector2[] faceUVs = GetTextureUVs(textureIndex);

            switch (dir)
            {
                case Direction.Top:
                    vertices.Add(pos + new Vector3(0, 1, 0));
                    vertices.Add(pos + new Vector3(0, 1, 1));
                    vertices.Add(pos + new Vector3(1, 1, 1));
                    vertices.Add(pos + new Vector3(1, 1, 0));
                    break;
                case Direction.Bottom:
                    vertices.Add(pos + new Vector3(0, 0, 1));
                    vertices.Add(pos + new Vector3(0, 0, 0));
                    vertices.Add(pos + new Vector3(1, 0, 0));
                    vertices.Add(pos + new Vector3(1, 0, 1));
                    break;
                case Direction.Left:
                    vertices.Add(pos + new Vector3(0, 0, 1));
                    vertices.Add(pos + new Vector3(0, 1, 1));
                    vertices.Add(pos + new Vector3(0, 1, 0));
                    vertices.Add(pos + new Vector3(0, 0, 0));
                    break;
                case Direction.Right:
                    vertices.Add(pos + new Vector3(1, 0, 0));
                    vertices.Add(pos + new Vector3(1, 1, 0));
                    vertices.Add(pos + new Vector3(1, 1, 1));
                    vertices.Add(pos + new Vector3(1, 0, 1));
                    break;
                case Direction.Front:
                    vertices.Add(pos + new Vector3(0, 0, 1));
                    vertices.Add(pos + new Vector3(1, 0, 1));
                    vertices.Add(pos + new Vector3(1, 1, 1));
                    vertices.Add(pos + new Vector3(0, 1, 1));
                    break;
                case Direction.Back:
                    vertices.Add(pos + new Vector3(1, 0, 0));
                    vertices.Add(pos + new Vector3(0, 0, 0));
                    vertices.Add(pos + new Vector3(0, 1, 0));
                    vertices.Add(pos + new Vector3(1, 1, 0));
                    break;
            }

            triangles.Add(vertCount);
            triangles.Add(vertCount + 1);
            triangles.Add(vertCount + 2);
            triangles.Add(vertCount);
            triangles.Add(vertCount + 2);
            triangles.Add(vertCount + 3);

            uvs.AddRange(faceUVs);

            colors.Add(vertColor);
            colors.Add(vertColor);
            colors.Add(vertColor);
            colors.Add(vertColor);
        }

        private Vector2[] GetTextureUVs(int textureIndex)
        {
            int x = textureIndex % TEXTURE_ATLAS_SIZE;
            int y = textureIndex / TEXTURE_ATLAS_SIZE;

            float u = x * TEXTURE_UNIT;
            float v = y * TEXTURE_UNIT;

            return new Vector2[]
            {
                new Vector2(u, v),
                new Vector2(u, v + TEXTURE_UNIT),
                new Vector2(u + TEXTURE_UNIT, v + TEXTURE_UNIT),
                new Vector2(u + TEXTURE_UNIT, v)
            };
        }

        public void SetMaterial(Material material)
        {
            if (meshRenderer != null)
                meshRenderer.material = material;
        }
    }
}
