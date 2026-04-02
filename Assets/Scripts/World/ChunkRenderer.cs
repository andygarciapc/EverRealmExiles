using UnityEngine;

namespace EverRealm.Exiles.World
{
    /// <summary>
    /// Owns the MeshFilter, MeshRenderer, and MeshCollider for one chunk.
    /// Receives a pre-built Mesh from <see cref="WorldManager"/>; does no generation.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class ChunkRenderer : MonoBehaviour
    {
        private MeshFilter   _filter;
        private MeshRenderer _renderer;
        private MeshCollider _collider;

        private void Awake()
        {
            _filter   = GetComponent<MeshFilter>();
            _renderer = GetComponent<MeshRenderer>();
            _collider = GetComponent<MeshCollider>();
        }

        /// <summary>Assigns a mesh and material. Call after AddComponent (Awake has already run).</summary>
        public void Initialize(Mesh mesh, Material material)
        {
            _filter.sharedMesh    = mesh;
            _renderer.sharedMaterial = material;
            _collider.sharedMesh  = mesh;
        }
    }
}
