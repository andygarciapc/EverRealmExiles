#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using EverRealm.Exiles.AI;
using EverRealm.Exiles.Combat;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.World;

/// <summary>
/// Editor utility that generates all Phase 11 assets: enemy variants,
/// loot tables, prefabs, audio system, combat feedback, and wires
/// scene references. Run via Tools > EverRealm > Generate Phase 11 Assets.
/// </summary>
public static class Phase11AssetGenerator
{
    private const string SOFolder        = "Assets/ScriptableObjects";
    private const string EnemySOFolder   = "Assets/ScriptableObjects/Enemies";
    private const string LootFolder      = "Assets/ScriptableObjects/LootTables";
    private const string ItemsFolder     = "Assets/ScriptableObjects/Items";
    private const string PrefabFolder    = "Assets/Prefabs";
    private const string MaterialsFolder = "Assets/Materials";
    private const string BiomesFolder    = "Assets/ScriptableObjects/Biomes";

    [MenuItem("Tools/EverRealm/Generate Phase 11 Assets")]
    public static void Generate()
    {
        EnsureFolder(EnemySOFolder);
        EnsureFolder(MaterialsFolder);

        // --- 1. Enemy definitions ---
        var bruteDef  = CreateHeavyBruteDefinition();
        var archerDef = CreateRangedArcherDefinition();

        // --- 2. Loot tables ---
        var bruteLoot  = CreateLootTable("BruteLoot",  CreateBruteLootEntries());
        var archerLoot = CreateLootTable("ArcherLoot", CreateArcherLootEntries());

        WireLootTableToDefinition(bruteDef,  bruteLoot);
        WireLootTableToDefinition(archerDef, archerLoot);

        // --- 3. Materials ---
        var bruteMat  = CreateMaterial("HeavyBrute_Mat",    new Color(0.35f, 0.20f, 0.10f));
        var archerMat = CreateMaterial("RangedArcher_Mat",  new Color(0.15f, 0.35f, 0.15f));
        var projMat   = CreateMaterial("Projectile_Mat",    new Color(0.40f, 0.10f, 0.10f));

        // --- 4. Projectile prefab ---
        var projectilePrefab = CreateProjectilePrefab(projMat);

        // --- 5. Shared prefab dependencies ---
        var lootPickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/LootPickup.prefab");
        var healthBarPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/UI/EnemyHealthBar.prefab");

        if (lootPickupPrefab == null) Debug.LogWarning("[Phase11Gen] LootPickup.prefab not found.");
        if (healthBarPrefab == null)  Debug.LogWarning("[Phase11Gen] EnemyHealthBar.prefab not found.");

        // --- 6. Enemy prefabs ---
        var brutePrefab = CreateEnemyPrefab(
            "HeavyBrute", bruteDef, bruteMat,
            lootPickupPrefab, healthBarPrefab,
            ccRadius: 0.7f, ccHeight: 2.5f);

        var archerPrefab = CreateEnemyPrefab(
            "RangedArcher", archerDef, archerMat,
            lootPickupPrefab, healthBarPrefab,
            ccRadius: 0.5f, ccHeight: 2f,
            projectilePrefab: projectilePrefab);

        // --- 7. Hit VFX prefab ---
        var hitVFXPrefab = CreateHitVFXPrefab();

        // --- 8. SFX Library ---
        var sfxLibrary = CreateSFXLibrary();

        // --- 9. AudioManager prefab ---
        var audioManagerPrefab = CreateAudioManagerPrefab(sfxLibrary);

        // --- 10. Scene wiring ---
        WireWorldManager(brutePrefab, archerPrefab);
        WireCombatFeedback(hitVFXPrefab);
        EnsureAudioManagerInScene(audioManagerPrefab);

        // --- 11. Biome vegetation ---
        SetMeadowlandsTrees();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Phase11Gen] All Phase 11 assets generated and wired successfully.");
        Debug.Log("[Phase11Gen] Import audio files to Assets/Audio/SFX/ and assign to the SFXLibrary asset.");
    }

    // =====================================================================
    // Enemy Definitions
    // =====================================================================

    private static EnemyDefinition CreateHeavyBruteDefinition()
    {
        string path = $"{EnemySOFolder}/HeavyBrute.asset";
        var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
        if (existing != null) { Debug.Log("[Phase11Gen] HeavyBrute definition exists — reusing."); return existing; }

        var def = ScriptableObject.CreateInstance<EnemyDefinition>();
        def.DisplayName      = "Heavy Brute";
        def.MaxHealth         = 150f;
        def.MoveSpeed         = 2.0f;
        def.PatrolRadius      = 8f;
        def.PatrolWaitMin     = 2f;
        def.PatrolWaitMax     = 4f;
        def.DetectionRadius   = 10f;
        def.LoseRadius        = 16f;
        def.AttackRange       = 2.5f;
        def.AttackDamage      = 25f;
        def.AttackWindup      = 0.6f;  // Slow, telegraphed
        def.AttackActive      = 0.2f;
        def.AttackRecovery    = 0.7f;
        def.KnockbackForce   = 6f;
        def.StaggerThreshold  = 50f;   // Hard to stagger
        def.StaggerDuration   = 0.8f;
        def.IsRanged          = false;
        def.Scale             = 1.5f;
        def.BodyColor         = new Color(0.35f, 0.20f, 0.10f);

        AssetDatabase.CreateAsset(def, path);
        EditorUtility.SetDirty(def);
        Debug.Log($"[Phase11Gen] Created {path}");
        return def;
    }

    private static EnemyDefinition CreateRangedArcherDefinition()
    {
        string path = $"{EnemySOFolder}/RangedArcher.asset";
        var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
        if (existing != null) { Debug.Log("[Phase11Gen] RangedArcher definition exists — reusing."); return existing; }

        var def = ScriptableObject.CreateInstance<EnemyDefinition>();
        def.DisplayName      = "Ranged Archer";
        def.MaxHealth         = 40f;
        def.MoveSpeed         = 3.5f;
        def.PatrolRadius      = 12f;
        def.PatrolWaitMin     = 1f;
        def.PatrolWaitMax     = 2.5f;
        def.DetectionRadius   = 16f;
        def.LoseRadius        = 22f;
        def.AttackRange       = 14f;   // Long range
        def.AttackDamage      = 8f;
        def.AttackWindup      = 0.5f;
        def.AttackActive      = 0.1f;
        def.AttackRecovery    = 0.8f;
        def.KnockbackForce   = 1f;
        def.StaggerThreshold  = 15f;   // Easy to stagger
        def.StaggerDuration   = 0.5f;
        def.IsRanged          = true;
        def.PreferredRange    = 10f;
        def.RetreatDistance   = 5f;
        def.ProjectileSpeed   = 15f;
        def.Scale             = 0.9f;
        def.BodyColor         = new Color(0.15f, 0.35f, 0.15f);

        AssetDatabase.CreateAsset(def, path);
        EditorUtility.SetDirty(def);
        Debug.Log($"[Phase11Gen] Created {path}");
        return def;
    }

    // =====================================================================
    // Loot Tables
    // =====================================================================

    private struct LootEntryData
    {
        public string ItemAssetName;
        public int Weight, MinCount, MaxCount;
    }

    private static LootTable CreateLootTable(string name, LootEntryData[] entries)
    {
        string path = $"{LootFolder}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<LootTable>(path);
        if (existing != null) { Debug.Log($"[Phase11Gen] {name} exists — reusing."); return existing; }

        var table = ScriptableObject.CreateInstance<LootTable>();
        AssetDatabase.CreateAsset(table, path);

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
                Debug.LogWarning($"[Phase11Gen] Item '{entries[i].ItemAssetName}' not found — skipping.");
                continue;
            }

            entry.FindPropertyRelative("Item").objectReferenceValue     = itemDef;
            entry.FindPropertyRelative("Weight").intValue               = entries[i].Weight;
            entry.FindPropertyRelative("MinCount").intValue             = entries[i].MinCount;
            entry.FindPropertyRelative("MaxCount").intValue             = entries[i].MaxCount;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(table);
        Debug.Log($"[Phase11Gen] Created {path}");
        return table;
    }

    private static LootEntryData[] CreateBruteLootEntries() => new[]
    {
        new LootEntryData { ItemAssetName = "IronOre",       Weight = 35, MinCount = 2, MaxCount = 4 },
        new LootEntryData { ItemAssetName = "Cloth",         Weight = 30, MinCount = 1, MaxCount = 3 },
        new LootEntryData { ItemAssetName = "HealingPotion", Weight = 20, MinCount = 1, MaxCount = 1 },
        new LootEntryData { ItemAssetName = "IronSword",     Weight = 15, MinCount = 1, MaxCount = 1 },
    };

    private static LootEntryData[] CreateArcherLootEntries() => new[]
    {
        new LootEntryData { ItemAssetName = "GoldCoin",      Weight = 35, MinCount = 1, MaxCount = 3 },
        new LootEntryData { ItemAssetName = "HealingPotion", Weight = 30, MinCount = 1, MaxCount = 2 },
        new LootEntryData { ItemAssetName = "Cloth",         Weight = 25, MinCount = 1, MaxCount = 2 },
        new LootEntryData { ItemAssetName = "IronOre",       Weight = 10, MinCount = 1, MaxCount = 2 },
    };

    private static void WireLootTableToDefinition(EnemyDefinition def, LootTable table)
    {
        var so = new SerializedObject(def);
        so.FindProperty("LootTable").objectReferenceValue = table;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(def);
    }

    // =====================================================================
    // Materials
    // =====================================================================

    private static Material CreateMaterial(string name, Color color)
    {
        string path = $"{MaterialsFolder}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;

        AssetDatabase.CreateAsset(mat, path);
        Debug.Log($"[Phase11Gen] Created material: {path}");
        return mat;
    }

    // =====================================================================
    // Prefabs
    // =====================================================================

    private static GameObject CreateProjectilePrefab(Material mat)
    {
        string path = $"{PrefabFolder}/EnemyProjectile.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "EnemyProjectile";
        go.transform.localScale = Vector3.one * 0.2f;

        var col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        go.AddComponent<Projectile>();
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log($"[Phase11Gen] Created {path}");
        return prefab;
    }

    private static GameObject CreateEnemyPrefab(
        string name, EnemyDefinition def, Material mat,
        GameObject lootPickupPrefab, GameObject healthBarPrefab,
        float ccRadius, float ccHeight,
        GameObject projectilePrefab = null)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) { Debug.Log($"[Phase11Gen] {name} prefab exists — reusing."); return existing; }

        // Capsule primitive provides MeshFilter + MeshRenderer + CapsuleCollider.
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;

        // CharacterController handles physics — remove redundant collider.
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());

        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // EnemyController auto-adds CharacterController, EnemyAttack, EnemyHealth
        // via RequireComponent.
        go.AddComponent<EnemyController>();

        // Configure CharacterController.
        var cc = go.GetComponent<CharacterController>();
        cc.radius     = ccRadius;
        cc.height     = ccHeight;
        cc.center     = new Vector3(0f, ccHeight * 0.5f, 0f);
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.5f;

        // Ranged enemies need the additional ranged attack component.
        if (def.IsRanged)
            go.AddComponent<EnemyRangedAttack>();

        // Save as prefab asset.
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        // Wire serialized fields via SerializedObject.
        var controllerSO = new SerializedObject(prefab.GetComponent<EnemyController>());
        controllerSO.FindProperty("_definition").objectReferenceValue       = def;
        controllerSO.FindProperty("_lootPickupPrefab").objectReferenceValue = lootPickupPrefab;
        controllerSO.FindProperty("_healthBarPrefab").objectReferenceValue  = healthBarPrefab;
        if (projectilePrefab != null)
            controllerSO.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;
        controllerSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire ranged attack's projectile prefab.
        if (def.IsRanged && projectilePrefab != null)
        {
            var ranged = prefab.GetComponent<EnemyRangedAttack>();
            if (ranged != null)
            {
                var rso = new SerializedObject(ranged);
                rso.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;
                rso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EditorUtility.SetDirty(prefab);
        Debug.Log($"[Phase11Gen] Created {path}");
        return prefab;
    }

    private static GameObject CreateHitVFXPrefab()
    {
        string path = $"{PrefabFolder}/HitVFX.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var go = new GameObject("HitVFX");
        var ps = go.AddComponent<ParticleSystem>();

        // Main module — short-lived spark burst.
        var main = ps.main;
        main.duration          = 0.5f;
        main.loop              = false;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize         = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor        = new Color(1f, 0.8f, 0.2f, 1f);
        main.maxParticles      = 12;
        main.gravityModifier   = 1.5f;
        main.simulationSpace   = ParticleSystemSimulationSpace.World;
        main.playOnAwake       = true;
        main.stopAction        = ParticleSystemStopAction.Destroy;

        // Emission — single burst of sparks.
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

        // Shape — small sphere origin.
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.1f;

        // Particle material.
        var rend = go.GetComponent<ParticleSystemRenderer>();
        var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");

        if (particleShader != null)
        {
            string matPath = $"{MaterialsFolder}/HitVFX_Mat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(particleShader);
                mat.SetColor("_BaseColor", new Color(1f, 0.8f, 0.2f, 1f));
                mat.color = new Color(1f, 0.8f, 0.2f, 1f);
                mat.SetFloat("_Surface", 1f); // Transparent
                mat.renderQueue = 3000;
                AssetDatabase.CreateAsset(mat, matPath);
            }
            rend.sharedMaterial = mat;
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log($"[Phase11Gen] Created {path}");
        return prefab;
    }

    private static SFXLibrary CreateSFXLibrary()
    {
        string path = $"{SOFolder}/SFXLibrary.asset";
        var existing = AssetDatabase.LoadAssetAtPath<SFXLibrary>(path);
        if (existing != null) { Debug.Log("[Phase11Gen] SFXLibrary exists — reusing."); return existing; }

        var lib = ScriptableObject.CreateInstance<SFXLibrary>();
        AssetDatabase.CreateAsset(lib, path);
        EditorUtility.SetDirty(lib);
        Debug.Log($"[Phase11Gen] Created {path} — assign audio clips when available.");
        return lib;
    }

    private static GameObject CreateAudioManagerPrefab(SFXLibrary sfxLibrary)
    {
        string path = $"{PrefabFolder}/AudioManager.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        // Wire SFXLibrary reference.
        var am = prefab.GetComponent<AudioManager>();
        var so = new SerializedObject(am);
        so.FindProperty("_sfxLibrary").objectReferenceValue = sfxLibrary;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(prefab);

        Debug.Log($"[Phase11Gen] Created {path}");
        return prefab;
    }

    // =====================================================================
    // Scene Wiring
    // =====================================================================

    private static void WireWorldManager(GameObject brutePrefab, GameObject archerPrefab)
    {
        var wm = Object.FindObjectOfType<WorldManager>();
        if (wm == null)
        {
            Debug.LogWarning("[Phase11Gen] WorldManager not found — open the Game scene and re-run.");
            return;
        }

        var so = new SerializedObject(wm);
        so.FindProperty("_heavyBrutePrefab").objectReferenceValue   = brutePrefab;
        so.FindProperty("_rangedArcherPrefab").objectReferenceValue = archerPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(wm);
        EditorUtility.SetDirty(wm.gameObject);

        Debug.Log("[Phase11Gen] Wired WorldManager with enemy variant prefabs.");
    }

    private static void WireCombatFeedback(GameObject hitVFXPrefab)
    {
        var cf = Object.FindObjectOfType<CombatFeedback>();
        if (cf == null)
        {
            var go = new GameObject("CombatFeedback");
            cf = go.AddComponent<CombatFeedback>();
            Debug.Log("[Phase11Gen] Created CombatFeedback GameObject in scene.");
        }

        var so = new SerializedObject(cf);
        so.FindProperty("_hitVFXPrefab").objectReferenceValue = hitVFXPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cf);
        EditorUtility.SetDirty(cf.gameObject);
    }

    private static void EnsureAudioManagerInScene(GameObject audioManagerPrefab)
    {
        var existing = Object.FindObjectOfType<AudioManager>();
        if (existing != null)
        {
            Debug.Log("[Phase11Gen] AudioManager already in scene.");
            return;
        }

        if (audioManagerPrefab != null)
        {
            PrefabUtility.InstantiatePrefab(audioManagerPrefab);
            Debug.Log("[Phase11Gen] Instantiated AudioManager prefab in scene.");
        }
    }

    private static void SetMeadowlandsTrees()
    {
        string path = $"{BiomesFolder}/Meadowlands.asset";
        var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(path);
        if (biome == null)
        {
            Debug.LogWarning("[Phase11Gen] Meadowlands biome not found.");
            return;
        }

        biome.HasTrees = true;
        EditorUtility.SetDirty(biome);
        Debug.Log("[Phase11Gen] Set Meadowlands HasTrees = true.");
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
