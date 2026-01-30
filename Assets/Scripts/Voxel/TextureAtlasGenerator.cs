using UnityEngine;

namespace EverRealmExiles.Voxel
{
    public static class TextureAtlasGenerator
    {
        private const int TEXTURE_SIZE = 16;
        private const int ATLAS_SIZE = 256;

        public static Texture2D GenerateAtlas()
        {
            Texture2D atlas = new Texture2D(ATLAS_SIZE, ATLAS_SIZE, TextureFormat.RGBA32, false);
            atlas.filterMode = FilterMode.Point;
            atlas.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[ATLAS_SIZE * ATLAS_SIZE];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.magenta;
            atlas.SetPixels(pixels);

            DrawTexture(atlas, 0, GenerateGrassTop());
            DrawTexture(atlas, 1, GenerateGrassSide());
            DrawTexture(atlas, 2, GenerateDirt());
            DrawTexture(atlas, 3, GenerateStone());
            DrawTexture(atlas, 4, GenerateSand());
            DrawTexture(atlas, 5, GenerateWater());
            DrawTexture(atlas, 6, GenerateWoodSide());
            DrawTexture(atlas, 7, GenerateWoodTop());
            DrawTexture(atlas, 8, GenerateLeaves());
            DrawTexture(atlas, 9, GenerateCobblestone());
            DrawTexture(atlas, 10, GenerateBedrock());
            DrawTexture(atlas, 11, GenerateGravel());
            DrawTexture(atlas, 12, GenerateCoalOre());
            DrawTexture(atlas, 13, GenerateIronOre());
            DrawTexture(atlas, 14, GenerateGoldOre());
            DrawTexture(atlas, 15, GenerateDiamondOre());
            DrawTexture(atlas, 16, GenerateSnow());

            atlas.Apply();
            return atlas;
        }

        private static void DrawTexture(Texture2D atlas, int index, Color[] texturePixels)
        {
            int x = (index % (ATLAS_SIZE / TEXTURE_SIZE)) * TEXTURE_SIZE;
            int y = (index / (ATLAS_SIZE / TEXTURE_SIZE)) * TEXTURE_SIZE;
            atlas.SetPixels(x, y, TEXTURE_SIZE, TEXTURE_SIZE, texturePixels);
        }

        private static Color[] GenerateGrassTop()
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            Color baseGreen = new Color(0.3f, 0.6f, 0.2f);
            System.Random rand = new System.Random(1);

            for (int i = 0; i < pixels.Length; i++)
            {
                float variation = (float)rand.NextDouble() * 0.15f - 0.075f;
                pixels[i] = new Color(
                    Mathf.Clamp01(baseGreen.r + variation),
                    Mathf.Clamp01(baseGreen.g + variation),
                    Mathf.Clamp01(baseGreen.b + variation)
                );
            }
            return pixels;
        }

        private static Color[] GenerateGrassSide()
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            Color grass = new Color(0.3f, 0.6f, 0.2f);
            Color dirt = new Color(0.55f, 0.35f, 0.2f);
            System.Random rand = new System.Random(2);

            for (int y = 0; y < TEXTURE_SIZE; y++)
            {
                for (int x = 0; x < TEXTURE_SIZE; x++)
                {
                    int i = y * TEXTURE_SIZE + x;
                    float variation = (float)rand.NextDouble() * 0.1f - 0.05f;

                    if (y >= TEXTURE_SIZE - 3)
                    {
                        pixels[i] = new Color(
                            Mathf.Clamp01(grass.r + variation),
                            Mathf.Clamp01(grass.g + variation),
                            Mathf.Clamp01(grass.b + variation)
                        );
                    }
                    else
                    {
                        pixels[i] = new Color(
                            Mathf.Clamp01(dirt.r + variation),
                            Mathf.Clamp01(dirt.g + variation),
                            Mathf.Clamp01(dirt.b + variation)
                        );
                    }
                }
            }
            return pixels;
        }

        private static Color[] GenerateDirt()
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            Color baseDirt = new Color(0.55f, 0.35f, 0.2f);
            System.Random rand = new System.Random(3);

            for (int i = 0; i < pixels.Length; i++)
            {
                float variation = (float)rand.NextDouble() * 0.15f - 0.075f;
                pixels[i] = new Color(
                    Mathf.Clamp01(baseDirt.r + variation),
                    Mathf.Clamp01(baseDirt.g + variation),
                    Mathf.Clamp01(baseDirt.b + variation)
                );
            }
            return pixels;
        }

        private static Color[] GenerateStone()
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            Color baseStone = new Color(0.5f, 0.5f, 0.5f);
            System.Random rand = new System.Random(4);

            for (int i = 0; i < pixels.Length; i++)
            {
                float variation = (float)rand.NextDouble() * 0.2f - 0.1f;
                float gray = baseStone.r + variation;
                pixels[i] = new Color(gray, gray, gray);
            }
            return pixels;
        }

        private static Color[] GenerateSand()
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            Color baseSand = new Color(0.85f, 0.8f, 0.55f);
            System.Random rand = new System.Random(5);

            for (int i = 0; i < pixels.Length; i++)
            {
                float variation = (float)rand.NextDouble() * 0.1f - 0.05f;
                pixels[i] = new Color(
                    Mathf.Clamp01(baseSand.r + variation),
                    Mathf.Clamp01(baseSand.g + variation),
                    Mathf.Clamp01(baseSand.b + variation)
                );
            }
            return pixels;
        }

        private static Color[] GenerateWater()
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            Color baseWater = new Color(0.2f, 0.4f, 0.8f, 0.7f);
            System.Random rand = new System.Random(6);

            for (int i = 0; i < pixels.Length; i++)
            {
                float variation = (float)rand.NextDouble() * 0.1f - 0.05f;
                pixels[i] = new Color(
                    Mathf.Clamp01(baseWater.r + variation),
                    Mathf.Clamp01(baseWater.g + variation),
                    Mathf.Clamp01(baseWater.b + variation),
                    baseWater.a
                );
            }
            return pixels;
        }

        private static Color[] GenerateWoodSide()
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            Color baseWood = new Color(0.4f, 0.25f, 0.1f);
            Color darkWood = new Color(0.3f, 0.18f, 0.08f);
            System.Random rand = new System.Random(7);

            for (int y = 0; y < TEXTURE_SIZE; y++)
            {
                for (int x = 0; x < TEXTURE_SIZE; x++)
                {
                    int i = y * TEXTURE_SIZE + x;
                    float variation = (float)rand.NextDouble() * 0.1f - 0.05f;

                    Color wood = (x == 4 || x == 8 || x == 12) ? darkWood : baseWood;
                    pixels[i] = new Color(
                        Mathf.Clamp01(wood.r + variation),
                        Mathf.Clamp01(wood.g + variation),
                        Mathf.Clamp01(wood.b + variation)
                    );
                }
            }
            return pixels;
        }

        private static Color[] GenerateWoodTop()
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            Color baseWood = new Color(0.5f, 0.35f, 0.15f);
            Color ringColor = new Color(0.4f, 0.25f, 0.1f);
            int center = TEXTURE_SIZE / 2;

            for (int y = 0; y < TEXTURE_SIZE; y++)
            {
                for (int x = 0; x < TEXTURE_SIZE; x++)
                {
                    int i = y * TEXTURE_SIZE + x;
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    int ring = (int)dist % 3;
                    pixels[i] = ring == 0 ? ringColor : baseWood;
                }
            }
            return pixels;
        }

        private static Color[] GenerateLeaves()
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            Color baseLeaf = new Color(0.15f, 0.45f, 0.1f);
            System.Random rand = new System.Random(9);

            for (int i = 0; i < pixels.Length; i++)
            {
                float variation = (float)rand.NextDouble() * 0.2f - 0.1f;
                float alpha = rand.NextDouble() > 0.2 ? 1f : 0f;
                pixels[i] = new Color(
                    Mathf.Clamp01(baseLeaf.r + variation),
                    Mathf.Clamp01(baseLeaf.g + variation),
                    Mathf.Clamp01(baseLeaf.b + variation),
                    alpha
                );
            }
            return pixels;
        }

        private static Color[] GenerateCobblestone()
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            Color light = new Color(0.55f, 0.55f, 0.55f);
            Color dark = new Color(0.35f, 0.35f, 0.35f);
            System.Random rand = new System.Random(10);

            for (int y = 0; y < TEXTURE_SIZE; y++)
            {
                for (int x = 0; x < TEXTURE_SIZE; x++)
                {
                    int i = y * TEXTURE_SIZE + x;
                    bool isEdge = (x % 4 == 0) || (y % 4 == 0);
                    float variation = (float)rand.NextDouble() * 0.1f - 0.05f;
                    Color baseColor = isEdge ? dark : light;
                    pixels[i] = new Color(
                        Mathf.Clamp01(baseColor.r + variation),
                        Mathf.Clamp01(baseColor.g + variation),
                        Mathf.Clamp01(baseColor.b + variation)
                    );
                }
            }
            return pixels;
        }

        private static Color[] GenerateBedrock()
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            Color dark = new Color(0.15f, 0.15f, 0.15f);
            System.Random rand = new System.Random(11);

            for (int i = 0; i < pixels.Length; i++)
            {
                float variation = (float)rand.NextDouble() * 0.15f;
                float gray = dark.r + variation;
                pixels[i] = new Color(gray, gray, gray);
            }
            return pixels;
        }

        private static Color[] GenerateGravel()
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            System.Random rand = new System.Random(12);

            for (int i = 0; i < pixels.Length; i++)
            {
                float gray = 0.4f + (float)rand.NextDouble() * 0.3f;
                pixels[i] = new Color(gray, gray, gray);
            }
            return pixels;
        }

        private static Color[] GenerateCoalOre()
        {
            return GenerateOreTexture(new Color(0.5f, 0.5f, 0.5f), new Color(0.1f, 0.1f, 0.1f), 13);
        }

        private static Color[] GenerateIronOre()
        {
            return GenerateOreTexture(new Color(0.5f, 0.5f, 0.5f), new Color(0.7f, 0.55f, 0.45f), 14);
        }

        private static Color[] GenerateGoldOre()
        {
            return GenerateOreTexture(new Color(0.5f, 0.5f, 0.5f), new Color(0.9f, 0.8f, 0.2f), 15);
        }

        private static Color[] GenerateDiamondOre()
        {
            return GenerateOreTexture(new Color(0.5f, 0.5f, 0.5f), new Color(0.3f, 0.9f, 0.9f), 16);
        }

        private static Color[] GenerateOreTexture(Color stoneColor, Color oreColor, int seed)
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            System.Random rand = new System.Random(seed);

            for (int i = 0; i < pixels.Length; i++)
            {
                float variation = (float)rand.NextDouble() * 0.1f - 0.05f;
                pixels[i] = new Color(
                    Mathf.Clamp01(stoneColor.r + variation),
                    Mathf.Clamp01(stoneColor.g + variation),
                    Mathf.Clamp01(stoneColor.b + variation)
                );
            }

            int numOres = 5 + rand.Next(5);
            for (int o = 0; o < numOres; o++)
            {
                int ox = rand.Next(TEXTURE_SIZE - 2) + 1;
                int oy = rand.Next(TEXTURE_SIZE - 2) + 1;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (rand.NextDouble() > 0.4)
                        {
                            int px = ox + dx;
                            int py = oy + dy;
                            if (px >= 0 && px < TEXTURE_SIZE && py >= 0 && py < TEXTURE_SIZE)
                            {
                                int idx = py * TEXTURE_SIZE + px;
                                pixels[idx] = oreColor;
                            }
                        }
                    }
                }
            }

            return pixels;
        }

        private static Color[] GenerateSnow()
        {
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            Color baseSnow = new Color(0.95f, 0.97f, 1f);
            System.Random rand = new System.Random(17);

            for (int i = 0; i < pixels.Length; i++)
            {
                float variation = (float)rand.NextDouble() * 0.05f;
                pixels[i] = new Color(
                    Mathf.Clamp01(baseSnow.r - variation),
                    Mathf.Clamp01(baseSnow.g - variation),
                    Mathf.Clamp01(baseSnow.b - variation)
                );
            }
            return pixels;
        }

        public static Material CreateVoxelMaterial()
        {
            Texture2D atlas = GenerateAtlas();

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.mainTexture = atlas;
            mat.SetFloat("_Smoothness", 0f);

            return mat;
        }
    }
}
