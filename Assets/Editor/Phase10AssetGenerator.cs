#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.World;

/// <summary>
/// Editor utility that generates Phase 10 assets: POISettings, zone-specific
/// loot tables, and wires all new references on WorldManager.
/// Run via Tools > EverRealm > Generate Phase 10 Assets.
/// </summary>
public static class Phase10AssetGenerator
{
    private const string SOFolder       = "Assets/ScriptableObjects";
    private const string LootFolder     = "Assets/ScriptableObjects/LootTables";
    private const string ItemsFolder    = "Assets/ScriptableObjects/Items";
    private const string PrefabFolder   = "Assets/Prefabs";

    [MenuItem("Tools/EverRealm/Generate Phase 10 Assets")]
    public static void Generate()
    {
        EnsureFolder(LootFolder);

        // Step 1: Create POISettings ScriptableObject.
        var poiSettings = CreatePOISettings();

        // Step 2: Create zone-specific loot tables.
        var safeLoot   = CreateZoneLootTable("SafeCacheLoot",   CreateSafeEntries());
        var mediumLoot = CreateZoneLootTable("MediumCacheLoot", CreateMediumEntries());
        var highLoot   = CreateZoneLootTable("HighCacheLoot",   CreateHighEntries());

        // Step 3: Wire WorldManager in the active scene.
        WireWorldManager(poiSettings, safeLoot, mediumLoot, highLoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Phase10Gen] All Phase 10 assets generated and wired successfully.");
        Debug.Log("[Phase10Gen] Open the Game scene and re-run if WorldManager was not found.");
    }

    // =====================================================================
    // POISettings
    // =====================================================================

    private static POISettings CreatePOISettings()
    {
        string path = $"{SOFolder}/POISettings.asset";
        var existing = AssetDatabase.LoadAssetAtPath<POISettings>(path);
        if (existing != null)
        {
            Debug.Log("[Phase10Gen] POISettings already exists — reusing.");
            return existing;
        }

        var settings = ScriptableObject.CreateInstance<POISettings>();
        // Defaults are set in the class itself; just create the asset.
        AssetDatabase.CreateAsset(settings, path);
        EditorUtility.SetDirty(settings);
        Debug.Log($"[Phase10Gen] Created {path}");
        return settings;
    }

    // =====================================================================
    // Zone Loot Tables
    // =====================================================================

    private static LootTable CreateZoneLootTable(string name, LootEntryData[] entries)
    {
        string path = $"{LootFolder}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<LootTable>(path);
        if (existing != null)
        {
            Debug.Log($"[Phase10Gen] {name} already exists — reusing.");
            return existing;
        }

        var table = ScriptableObject.CreateInstance<LootTable>();
        AssetDatabase.CreateAsset(table, path);

        // Wire entries via SerializedObject.
        var so = new SerializedObject(table);
        var entriesProp = so.FindProperty("_entries");
        entriesProp.arraySize = entries.Length;

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entriesProp.GetArrayElementAtIndex(i);
            var itemDef = AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                $"{ItemsFolder}/{entries[i].ItemAssetName}.asset");

            if (itemDef == null)
            {
                Debug.LogWarning($"[Phase10Gen] Item '{entries[i].ItemAssetName}' not found — skipping entry.");
                continue;
            }

            entry.FindPropertyRelative("Item").objectReferenceValue = itemDef;
            entry.FindPropertyRelative("Weight").intValue = entries[i].Weight;
            entry.FindPropertyRelative("MinCount").intValue = entries[i].MinCount;
            entry.FindPropertyRelative("MaxCount").intValue = entries[i].MaxCount;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(table);
        Debug.Log($"[Phase10Gen] Created {path} with {entries.Length} entries.");
        return table;
    }

    private struct LootEntryData
    {
        public string ItemAssetName;
        public int Weight;
        public int MinCount;
        public int MaxCount;
    }

    private static LootEntryData[] CreateSafeEntries()
    {
        // Common items, small quantities.
        return new[]
        {
            new LootEntryData { ItemAssetName = "Cloth",         Weight = 40, MinCount = 1, MaxCount = 3 },
            new LootEntryData { ItemAssetName = "IronOre",       Weight = 35, MinCount = 1, MaxCount = 2 },
            new LootEntryData { ItemAssetName = "HealingPotion", Weight = 25, MinCount = 1, MaxCount = 1 },
        };
    }

    private static LootEntryData[] CreateMediumEntries()
    {
        // Mix of common and uncommon.
        return new[]
        {
            new LootEntryData { ItemAssetName = "Cloth",         Weight = 20, MinCount = 1, MaxCount = 3 },
            new LootEntryData { ItemAssetName = "IronOre",       Weight = 25, MinCount = 1, MaxCount = 3 },
            new LootEntryData { ItemAssetName = "GoldCoin",      Weight = 25, MinCount = 1, MaxCount = 3 },
            new LootEntryData { ItemAssetName = "HealingPotion", Weight = 20, MinCount = 1, MaxCount = 2 },
            new LootEntryData { ItemAssetName = "IronSword",     Weight = 10, MinCount = 1, MaxCount = 1 },
        };
    }

    private static LootEntryData[] CreateHighEntries()
    {
        // Rarer items, higher quantities.
        return new[]
        {
            new LootEntryData { ItemAssetName = "GoldCoin",      Weight = 30, MinCount = 2, MaxCount = 5 },
            new LootEntryData { ItemAssetName = "HealingPotion", Weight = 20, MinCount = 1, MaxCount = 3 },
            new LootEntryData { ItemAssetName = "IronSword",     Weight = 20, MinCount = 1, MaxCount = 1 },
            new LootEntryData { ItemAssetName = "IronOre",       Weight = 15, MinCount = 2, MaxCount = 5 },
            new LootEntryData { ItemAssetName = "Cloth",         Weight = 15, MinCount = 2, MaxCount = 4 },
        };
    }

    // =====================================================================
    // Scene Wiring
    // =====================================================================

    private static void WireWorldManager(POISettings poiSettings, LootTable safeLoot, LootTable mediumLoot, LootTable highLoot)
    {
        var wm = Object.FindObjectOfType<WorldManager>();
        if (wm == null)
        {
            Debug.LogWarning("[Phase10Gen] WorldManager not found in scene — open the Game scene and re-run.");
            return;
        }

        // Load prefabs.
        var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Grunt.prefab");
        var lootPickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/LootPickup.prefab");

        if (enemyPrefab == null) Debug.LogWarning("[Phase10Gen] Grunt.prefab not found.");
        if (lootPickupPrefab == null) Debug.LogWarning("[Phase10Gen] LootPickup.prefab not found.");

        var so = new SerializedObject(wm);
        so.FindProperty("_poiSettings").objectReferenceValue       = poiSettings;
        so.FindProperty("_enemyPrefab").objectReferenceValue       = enemyPrefab;
        so.FindProperty("_lootPickupPrefab").objectReferenceValue  = lootPickupPrefab;
        so.FindProperty("_safeLootTable").objectReferenceValue     = safeLoot;
        so.FindProperty("_mediumLootTable").objectReferenceValue   = mediumLoot;
        so.FindProperty("_highLootTable").objectReferenceValue     = highLoot;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(wm);

        Debug.Log("[Phase10Gen] Wired WorldManager with POI settings, prefabs, and loot tables.");

        // Add WorldBoundary if not already present.
        var boundary = wm.GetComponent<WorldBoundary>();
        if (boundary == null)
        {
            boundary = wm.gameObject.AddComponent<WorldBoundary>();
            Debug.Log("[Phase10Gen] Added WorldBoundary component to WorldManager.");
        }

        // Wire WorldBoundary settings.
        var worldGenSettings = AssetDatabase.LoadAssetAtPath<WorldGenSettings>($"{SOFolder}/WorldGenSettings.asset");
        if (worldGenSettings != null)
        {
            var bso = new SerializedObject(boundary);
            bso.FindProperty("_settings").objectReferenceValue = worldGenSettings;
            bso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(boundary);
            Debug.Log("[Phase10Gen] Wired WorldBoundary with WorldGenSettings.");
        }

        EditorUtility.SetDirty(wm.gameObject);
    }

    // =====================================================================
    // Helpers
    // =====================================================================

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
