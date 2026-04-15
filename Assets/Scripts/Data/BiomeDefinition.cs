using UnityEngine;
using EverRealm.Exiles.World;

namespace EverRealm.Exiles.Data
{
    /// <summary>
    /// Data asset describing a biome's world generation parameters and display info.
    /// Each biome produces a distinct-feeling terrain when selected from the Play tab.
    /// Create via Assets > Create > EverRealm > Biome Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "EverRealm/Biome Definition", fileName = "Biome")]
    public sealed class BiomeDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Machine-readable key (e.g. 'meadowlands'). Stable across renames.")]
        public string BiomeId;
        public string BiomeName = "Unknown";
        [TextArea(2, 4)]
        public string Description = "";

        [Header("Display")]
        [Tooltip("Color shown on the biome card when no icon is assigned.")]
        public Color CardColor = new(0.3f, 0.6f, 0.3f, 1f);
        public Sprite Icon;

        [Header("Difficulty")]
        [Range(1, 5)]
        [Tooltip("1 = easy, 5 = extreme. Affects future enemy density/types.")]
        public int DifficultyTier = 1;

        [Header("Terrain — Height")]
        [Range(8, 32)]  public int HeightMin = 28;
        [Range(32, 56)] public int HeightMax = 48;

        [Header("Terrain — Noise")]
        public float NoiseScale      = 0.04f;
        [Range(1, 8)] public int    NoiseOctaves     = 4;
        [Range(0f, 1f)] public float NoisePersistence = 0.5f;
        [Range(1f, 4f)] public float NoiseLacunarity  = 2.0f;

        [Header("Terrain — Surface Blocks")]
        public BlockType SurfaceBlock    = BlockType.Grass;
        public BlockType SubSurfaceBlock = BlockType.Dirt;

        [Header("Vegetation")]
        [Tooltip("Generate voxel trees on the surface.")]
        public bool HasTrees = false;

        [Header("Map")]
        [Tooltip("Position on the world map as normalized coordinates (0-1).")]
        public Vector2 MapPosition = new(0.5f, 0.5f);
    }
}
