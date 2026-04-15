#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using EverRealm.Exiles.World;

/// <summary>
/// Generates a procedural pixel-art block atlas texture.
/// Each block type gets a 16x16 tileable pattern.
/// Run via Tools > EverRealm > Generate Block Atlas.
/// </summary>
public static class BlockTextureGenerator
{
    private const int TileSize = 16;
    private const string TextureFolder = "Assets/Textures";
    private const string TexturePath = "Assets/Textures/BlockAtlas.png";

    [MenuItem("Tools/EverRealm/Generate Block Atlas")]
    public static void Generate()
    {
        EnsureFolder(TextureFolder);

        int blockCount = System.Enum.GetValues(typeof(BlockType)).Length;
        int atlasWidth = TileSize;
        int atlasHeight = blockCount * TileSize;

        var tex = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
            name = "BlockAtlas"
        };

        for (int b = 0; b < blockCount; b++)
            PaintTile(tex, (BlockType)b, b * TileSize);

        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(TexturePath, png);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);

        // Configure import settings.
        var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
        }

        Debug.Log($"[BlockTextureGen] Created {TexturePath} ({atlasWidth}x{atlasHeight}, {blockCount} block types).");
    }

    // -----------------------------------------------------------------
    // Per-block-type tile painting
    // -----------------------------------------------------------------

    private static void PaintTile(Texture2D tex, BlockType type, int yOffset)
    {
        switch (type)
        {
            case BlockType.Air:
                FillSolid(tex, yOffset, Color.clear);
                break;

            case BlockType.Grass:
                FillWithNoise(tex, yOffset,
                    new Color(0.30f, 0.65f, 0.20f),
                    new Color(0.22f, 0.52f, 0.14f), 0.15f);
                break;

            case BlockType.Dirt:
                FillWithNoise(tex, yOffset,
                    new Color(0.55f, 0.37f, 0.18f),
                    new Color(0.45f, 0.30f, 0.14f), 0.12f);
                break;

            case BlockType.Stone:
                PaintStone(tex, yOffset);
                break;

            case BlockType.Sand:
                FillWithNoise(tex, yOffset,
                    new Color(0.90f, 0.85f, 0.55f),
                    new Color(0.85f, 0.78f, 0.50f), 0.08f);
                break;

            case BlockType.CoalOre:
                PaintOre(tex, yOffset,
                    new Color(0.50f, 0.50f, 0.50f),
                    new Color(0.12f, 0.12f, 0.12f));
                break;

            case BlockType.IronOre:
                PaintOre(tex, yOffset,
                    new Color(0.50f, 0.50f, 0.50f),
                    new Color(0.72f, 0.56f, 0.42f));
                break;

            case BlockType.GoldOre:
                PaintOre(tex, yOffset,
                    new Color(0.50f, 0.50f, 0.50f),
                    new Color(0.95f, 0.82f, 0.20f));
                break;

            case BlockType.Wood:
                PaintWood(tex, yOffset);
                break;

            case BlockType.Leaves:
                FillWithNoise(tex, yOffset,
                    new Color(0.18f, 0.50f, 0.12f),
                    new Color(0.10f, 0.38f, 0.08f), 0.20f);
                break;

            case BlockType.Snow:
                FillWithNoise(tex, yOffset,
                    new Color(0.92f, 0.95f, 0.98f),
                    new Color(0.82f, 0.88f, 0.95f), 0.06f);
                break;

            case BlockType.Chest:
                PaintChest(tex, yOffset);
                break;

            case BlockType.ExtractionCore:
                FillWithNoise(tex, yOffset,
                    new Color(0.10f, 0.85f, 0.95f),
                    new Color(0.05f, 0.65f, 0.80f), 0.12f);
                break;

            case BlockType.EnemyCampCore:
                FillWithNoise(tex, yOffset,
                    new Color(0.70f, 0.15f, 0.15f),
                    new Color(0.55f, 0.10f, 0.10f), 0.10f);
                break;

            case BlockType.TreasureCacheCore:
                FillWithNoise(tex, yOffset,
                    new Color(0.85f, 0.70f, 0.10f),
                    new Color(0.70f, 0.55f, 0.08f), 0.12f);
                break;

            default:
                FillSolid(tex, yOffset, Color.magenta);
                break;
        }
    }

    // -----------------------------------------------------------------
    // Pattern helpers
    // -----------------------------------------------------------------

    private static void FillSolid(Texture2D tex, int yOff, Color c)
    {
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
                tex.SetPixel(x, yOff + y, c);
    }

    private static void FillWithNoise(Texture2D tex, int yOff, Color baseCol, Color altCol, float noiseScale)
    {
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.5f + yOff * 3.7f, y * 0.5f + yOff * 1.3f);
                Color c = n > (1f - noiseScale * 2f) ? altCol : baseCol;
                // Subtle per-pixel variation.
                float v = (Mathf.PerlinNoise(x * 1.3f + yOff, y * 1.3f) - 0.5f) * 0.06f;
                c = new Color(
                    Mathf.Clamp01(c.r + v),
                    Mathf.Clamp01(c.g + v),
                    Mathf.Clamp01(c.b + v), 1f);
                tex.SetPixel(x, yOff + y, c);
            }
    }

    private static void PaintStone(Texture2D tex, int yOff)
    {
        Color baseCol = new Color(0.50f, 0.50f, 0.50f);
        Color crackCol = new Color(0.38f, 0.38f, 0.38f);

        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.4f + 10f, y * 0.4f + 10f);
                Color c = n > 0.65f ? crackCol : baseCol;
                float v = (Mathf.PerlinNoise(x * 1.1f, y * 1.1f) - 0.5f) * 0.08f;
                c = new Color(Mathf.Clamp01(c.r + v), Mathf.Clamp01(c.g + v), Mathf.Clamp01(c.b + v), 1f);
                tex.SetPixel(x, yOff + y, c);
            }
    }

    private static void PaintOre(Texture2D tex, int yOff, Color stoneCol, Color oreCol)
    {
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
            {
                // Base stone with ore flecks.
                float n = Mathf.PerlinNoise(x * 0.6f + yOff * 0.1f, y * 0.6f + yOff * 0.3f);
                Color c = n > 0.58f ? oreCol : stoneCol;
                float v = (Mathf.PerlinNoise(x * 1.2f + yOff, y * 1.2f) - 0.5f) * 0.06f;
                c = new Color(Mathf.Clamp01(c.r + v), Mathf.Clamp01(c.g + v), Mathf.Clamp01(c.b + v), 1f);
                tex.SetPixel(x, yOff + y, c);
            }
    }

    private static void PaintWood(Texture2D tex, int yOff)
    {
        Color baseCol = new Color(0.45f, 0.28f, 0.13f);
        Color grainCol = new Color(0.38f, 0.22f, 0.10f);

        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
            {
                // Vertical grain lines.
                float grain = Mathf.PerlinNoise(x * 0.3f + 5f, y * 0.05f + 5f);
                Color c = grain > 0.55f ? grainCol : baseCol;
                float v = (Mathf.PerlinNoise(x * 0.8f + yOff, y * 0.8f) - 0.5f) * 0.05f;
                c = new Color(Mathf.Clamp01(c.r + v), Mathf.Clamp01(c.g + v), Mathf.Clamp01(c.b + v), 1f);
                tex.SetPixel(x, yOff + y, c);
            }
    }

    private static void PaintChest(Texture2D tex, int yOff)
    {
        Color woodCol = new Color(0.55f, 0.35f, 0.10f);
        Color metalCol = new Color(0.65f, 0.60f, 0.45f);

        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
            {
                // Metal band across the middle.
                bool isBand = y >= 6 && y <= 9;
                Color c = isBand ? metalCol : woodCol;
                float v = (Mathf.PerlinNoise(x * 1.0f + yOff, y * 1.0f) - 0.5f) * 0.06f;
                c = new Color(Mathf.Clamp01(c.r + v), Mathf.Clamp01(c.g + v), Mathf.Clamp01(c.b + v), 1f);
                tex.SetPixel(x, yOff + y, c);
            }
    }

    // -----------------------------------------------------------------

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
