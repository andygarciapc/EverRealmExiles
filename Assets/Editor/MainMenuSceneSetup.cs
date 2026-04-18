#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.UI;
using EverRealm.Exiles.World;

/// <summary>
/// Editor utility for building the MainMenu scene and related assets.
///
/// Menu items:
///   Tools > EverRealm > Generate Biome Assets     — creates biome SOs + registry
///   Tools > EverRealm > Generate BiomeCard Prefab  — creates the biome card UI prefab
///   Tools > EverRealm > Setup MainMenu Scene       — rebuilds the MainMenu scene with
///                                                    the full tabbed UI visible in edit mode
///   Tools > EverRealm > Fix Game Scene             — removes Bootstrap from Game scene
/// </summary>
public static class MainMenuSceneSetup
{
    // ----- Paths -----
    private const string MainMenuScenePath   = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath       = "Assets/Scenes/Game.unity";
    private const string InputActionsPath    = "Assets/InputSystem_Actions.inputactions";
    private const string FontPath            = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string ItemRegistryPath    = "Assets/ScriptableObjects/ItemRegistry.asset";
    private const string WeaponRegistryPath  = "Assets/ScriptableObjects/WeaponRegistry.asset";
    private const string BiomeRegistryPath   = "Assets/ScriptableObjects/BiomeRegistry.asset";
    private const string BiomeFolderPath     = "Assets/ScriptableObjects/Biomes";
    private const string PrefabFolder        = "Assets/Prefabs/UI";
    private const string BiomeCardPrefabPath = "Assets/Prefabs/UI/BiomeCard.prefab";
    private const string MapPointPrefabPath  = "Assets/Prefabs/UI/MapPoint.prefab";
    private const string MainMenuUiPrefabPath = "Assets/Prefabs/UI/MainMenuUI.prefab";

    // Existing prefabs reused in Exile tab
    private const string InventorySlotPrefabPath = "Assets/Prefabs/UI/InventorySlot.prefab";
    private const string WeaponButtonPrefabPath  = "Assets/Prefabs/UI/WeaponButton.prefab";

    // =====================================================================
    // 1. Generate Biome Assets
    // =====================================================================

    [MenuItem("Tools/EverRealm/Generate Biome Assets")]
    public static void GenerateBiomeAssets()
    {
        EnsureFolder(BiomeFolderPath);

        var meadowlands = CreateBiome("meadowlands", "Meadowlands",
            "Rolling green hills with gentle terrain. A good starting zone.",
            new Color(0.30f, 0.65f, 0.20f), 1,
            28, 48, 0.04f, 4, 0.5f, 2.0f,
            BlockType.Grass, BlockType.Dirt,
            new Vector2(0.35f, 0.55f));

        var ashlands = CreateBiome("ashlands", "Ashlands",
            "Charred volcanic wasteland. Jagged peaks and deep ravines hide rare ores.",
            new Color(0.55f, 0.20f, 0.10f), 3,
            20, 55, 0.06f, 5, 0.45f, 2.2f,
            BlockType.Stone, BlockType.Stone,
            new Vector2(0.70f, 0.30f));

        var frostpeak = CreateBiome("frostpeak", "Frostpeak",
            "Frozen mountain range. Towering cliffs and treacherous ice shelves.",
            new Color(0.70f, 0.80f, 0.95f), 4,
            35, 56, 0.03f, 6, 0.55f, 2.5f,
            BlockType.Sand, BlockType.Stone, // Sand = snow placeholder until we add Snow block
            new Vector2(0.50f, 0.80f));

        var sandstone = CreateBiome("sandstone_wastes", "Sandstone Wastes",
            "Endless dunes and exposed sandstone mesas. Flat but exposed.",
            new Color(0.85f, 0.75f, 0.45f), 2,
            25, 40, 0.035f, 3, 0.6f, 1.8f,
            BlockType.Sand, BlockType.Sand,
            new Vector2(0.20f, 0.25f));

        // Create the registry.
        var registry = ScriptableObject.CreateInstance<BiomeRegistry>();
        var regSo = new SerializedObject(registry);
        var biomesArr = regSo.FindProperty("_biomes");
        biomesArr.arraySize = 4;
        biomesArr.GetArrayElementAtIndex(0).objectReferenceValue = meadowlands;
        biomesArr.GetArrayElementAtIndex(1).objectReferenceValue = ashlands;
        biomesArr.GetArrayElementAtIndex(2).objectReferenceValue = frostpeak;
        biomesArr.GetArrayElementAtIndex(3).objectReferenceValue = sandstone;
        regSo.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(registry, BiomeRegistryPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MainMenuSetup] Generated 4 biome definitions + BiomeRegistry.");
    }

    private static BiomeDefinition CreateBiome(string id, string name, string desc,
        Color cardColor, int difficulty,
        int heightMin, int heightMax, float noiseScale, int octaves, float persistence, float lacunarity,
        BlockType surface, BlockType subSurface, Vector2 mapPosition)
    {
        string path = $"{BiomeFolderPath}/{name}.asset";

        // Reuse existing asset if present — update MapPosition in case it changed.
        var existing = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(path);
        if (existing != null)
        {
            existing.MapPosition = mapPosition;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var biome = ScriptableObject.CreateInstance<BiomeDefinition>();
        biome.BiomeId         = id;
        biome.BiomeName       = name;
        biome.Description     = desc;
        biome.CardColor       = cardColor;
        biome.DifficultyTier  = difficulty;
        biome.HeightMin       = heightMin;
        biome.HeightMax       = heightMax;
        biome.NoiseScale      = noiseScale;
        biome.NoiseOctaves    = octaves;
        biome.NoisePersistence = persistence;
        biome.NoiseLacunarity = lacunarity;
        biome.SurfaceBlock    = surface;
        biome.SubSurfaceBlock = subSurface;
        biome.MapPosition     = mapPosition;

        AssetDatabase.CreateAsset(biome, path);
        Debug.Log($"[MainMenuSetup] Created biome: {name}");
        return biome;
    }

    // =====================================================================
    // 2. Generate BiomeCard Prefab
    // =====================================================================

    [MenuItem("Tools/EverRealm/Generate BiomeCard Prefab")]
    public static void GenerateBiomeCardPrefab()
    {
        EnsureFolder(PrefabFolder);

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError("[MainMenuSetup] TMP font not found at " + FontPath);
            return;
        }

        var root = new GameObject("BiomeCard");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(220, 140);

        // Background (biome color).
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.25f, 0.30f, 1f);
        bg.raycastTarget = true;

        // Selection border.
        var border = new GameObject("SelectionBorder");
        border.transform.SetParent(root.transform, false);
        border.layer = 5;
        var borderRect = border.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-3, -3);
        borderRect.offsetMax = new Vector2(3, 3);
        var borderImg = border.AddComponent<Image>();
        borderImg.color = new Color(0.95f, 0.85f, 0.3f, 1f); // gold
        borderImg.raycastTarget = false;
        borderImg.enabled = false; // hidden by default
        // Move border behind the background.
        border.transform.SetAsFirstSibling();

        // Icon (optional, shown over the background color).
        var icon = new GameObject("Icon");
        icon.transform.SetParent(root.transform, false);
        icon.layer = 5;
        var iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.15f, 0.3f);
        iconRect.anchorMax = new Vector2(0.85f, 0.85f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        var iconImg = icon.AddComponent<Image>();
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        iconImg.enabled = false;

        // Biome name.
        var nameText = CreateText(root.transform, "Name", font,
            new Vector2(0, 0), new Vector2(1, 0),
            Vector2.zero, Vector2.zero,
            "Biome", 16, TextAlignmentOptions.Center,
            Color.white);
        var nameRect = nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.05f);
        nameRect.anchorMax = new Vector2(1, 0.3f);
        nameRect.offsetMin = new Vector2(5, 0);
        nameRect.offsetMax = new Vector2(-5, 0);

        // Difficulty text (stars).
        var diffText = CreateText(root.transform, "Difficulty", font,
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-8, -6), new Vector2(100, 18),
            "\u2605\u2606\u2606\u2606\u2606", 12, TextAlignmentOptions.Right,
            new Color(1f, 0.9f, 0.4f, 0.9f));
        var diffRect = diffText.GetComponent<RectTransform>();
        diffRect.pivot = new Vector2(1, 1);

        // Button component.
        var btn = root.AddComponent<Button>();
        btn.targetGraphic = bg;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = colors;

        // Wire BiomeCardUI component.
        var card = root.AddComponent<BiomeCardUI>();
        var so = new SerializedObject(card);
        so.FindProperty("_background").objectReferenceValue = bg;
        so.FindProperty("_icon").objectReferenceValue = iconImg;
        so.FindProperty("_selectionBorder").objectReferenceValue = borderImg;
        so.FindProperty("_nameText").objectReferenceValue = nameText.GetComponent<TMP_Text>();
        so.FindProperty("_difficultyText").objectReferenceValue = diffText.GetComponent<TMP_Text>();
        so.FindProperty("_button").objectReferenceValue = btn;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, BiomeCardPrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        Debug.Log("[MainMenuSetup] Generated BiomeCard prefab.");
    }

    // =====================================================================
    // 2b. Generate MapPoint Prefab
    // =====================================================================

    [MenuItem("Tools/EverRealm/Generate MapPoint Prefab")]
    public static void GenerateMapPointPrefab()
    {
        EnsureFolder(PrefabFolder);

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError("[MainMenuSetup] TMP font not found at " + FontPath);
            return;
        }

        // Root — small clickable point.
        var root = new GameObject("MapPoint");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(50, 50);

        // Glow ring (behind marker, shown when selected).
        var glow = new GameObject("Glow");
        glow.transform.SetParent(root.transform, false);
        glow.layer = 5;
        var glowRect = glow.AddComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(60, 60);
        var glowImg = glow.AddComponent<Image>();
        glowImg.color = new Color(1f, 0.85f, 0.3f, 0.4f);
        glowImg.raycastTarget = false;
        glowImg.enabled = false; // Hidden by default.

        // Marker dot.
        var marker = new GameObject("Marker");
        marker.transform.SetParent(root.transform, false);
        marker.layer = 5;
        var markerRect = marker.AddComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(30, 30);
        var markerImg = marker.AddComponent<Image>();
        markerImg.color = new Color(0.3f, 0.65f, 0.2f, 1f);
        markerImg.raycastTarget = false;

        // Label below marker.
        var label = new GameObject("Label");
        label.transform.SetParent(root.transform, false);
        label.layer = 5;
        var labelRect = label.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0, -22);
        labelRect.sizeDelta = new Vector2(120, 20);
        var labelTmp = label.AddComponent<TextMeshProUGUI>();
        labelTmp.font = font;
        labelTmp.text = "Location";
        labelTmp.fontSize = 14;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        labelTmp.raycastTarget = false;
        labelTmp.enableWordWrapping = false;
        labelTmp.overflowMode = TextOverflowModes.Overflow;

        // Button on root for clicks.
        var btnImg = root.AddComponent<Image>();
        btnImg.color = new Color(0, 0, 0, 0); // Invisible hit area.
        btnImg.raycastTarget = true;
        var btn = root.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = colors;

        // Wire MapPointUI component.
        var mapPoint = root.AddComponent<MapPointUI>();
        var so = new SerializedObject(mapPoint);
        so.FindProperty("_marker").objectReferenceValue = markerImg;
        so.FindProperty("_glow").objectReferenceValue   = glowImg;
        so.FindProperty("_label").objectReferenceValue   = labelTmp;
        so.FindProperty("_button").objectReferenceValue  = btn;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, MapPointPrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        Debug.Log("[MainMenuSetup] Generated MapPoint prefab.");
    }

    // =====================================================================
    // 3. Setup MainMenu Scene (full tabbed UI)
    // =====================================================================

    [MenuItem("Tools/EverRealm/Setup MainMenu Scene")]
    public static void SetupMainMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Setup MainMenu Scene",
                "This will DELETE all existing objects in MainMenu.unity and rebuild the scene with:\n\n" +
                "- Bootstrap (GameBootstrap + StashManager)\n" +
                "- MainMenuManager (MainMenuController)\n" +
                "- Tabbed MainMenuUI (Play, Craft, Exile, Shop)\n" +
                "- Main Camera, Light, EventSystem\n\n" +
                "Continue?",
                "Setup", "Cancel"))
        {
            return;
        }

        // Ensure prerequisites exist.
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) { Debug.LogError("[MainMenuSetup] TMP font not found at " + FontPath); return; }

        var itemRegistry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(ItemRegistryPath);
        var weaponRegistry = AssetDatabase.LoadAssetAtPath<WeaponRegistry>(WeaponRegistryPath);
        var biomeRegistry = AssetDatabase.LoadAssetAtPath<BiomeRegistry>(BiomeRegistryPath);
        if (biomeRegistry == null)
        {
            Debug.Log("[MainMenuSetup] BiomeRegistry not found — auto-generating biome assets...");
            GenerateBiomeAssets();
            biomeRegistry = AssetDatabase.LoadAssetAtPath<BiomeRegistry>(BiomeRegistryPath);
        }

        var mapPointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapPointPrefabPath);
        if (mapPointPrefab == null)
        {
            Debug.Log("[MainMenuSetup] MapPoint prefab not found — auto-generating...");
            GenerateMapPointPrefab();
            mapPointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapPointPrefabPath);
        }

        var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InventorySlotPrefabPath);
        var weaponBtnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponButtonPrefabPath);

        // Open and clear the scene.
        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);

        // ---- Bootstrap ----
        // Re-load registries fresh to avoid stale references after scene open.
        itemRegistry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(ItemRegistryPath);
        weaponRegistry = AssetDatabase.LoadAssetAtPath<WeaponRegistry>(WeaponRegistryPath);
        biomeRegistry = AssetDatabase.LoadAssetAtPath<BiomeRegistry>(BiomeRegistryPath);

        Debug.Log($"[MainMenuSetup] Registries — item={itemRegistry != null}, weapon={weaponRegistry != null}, biome={biomeRegistry != null}");

        var bootstrapGo = new GameObject("Bootstrap");
        bootstrapGo.AddComponent<GameBootstrap>();
        var stash = bootstrapGo.AddComponent<StashManager>();
        {
            var so = new SerializedObject(stash);
            if (itemRegistry != null)
                so.FindProperty("_itemRegistry").objectReferenceValue = itemRegistry;
            if (weaponRegistry != null)
                so.FindProperty("_weaponRegistry").objectReferenceValue = weaponRegistry;
            if (biomeRegistry != null)
                so.FindProperty("_biomeRegistry").objectReferenceValue = biomeRegistry;

            // Wire starter items for new players.
            var swordItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/ScriptableObjects/Items/IronSword.asset");
            var potionItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/ScriptableObjects/Items/HealingPotion.asset");
            var starterProp = so.FindProperty("_starterItems");
            starterProp.arraySize = 3;
            starterProp.GetArrayElementAtIndex(0).objectReferenceValue = swordItem;
            starterProp.GetArrayElementAtIndex(1).objectReferenceValue = potionItem;
            starterProp.GetArrayElementAtIndex(2).objectReferenceValue = potionItem;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- MainMenuManager ----
        var menuManagerGo = new GameObject("MainMenuManager");
        menuManagerGo.AddComponent<MainMenuController>();
        // MainMenuController finds MainMenuUI in the scene — no prefab ref needed.

        // ---- Camera ----
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 1f);
        cameraGo.AddComponent<AudioListener>();

        // ---- Light ----
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.5f;
        light.color = new Color(1f, 0.95f, 0.9f, 1f);
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // ---- EventSystem ----
        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        var uiInputModule = eventSystemGo.AddComponent<InputSystemUIInputModule>();
        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        if (inputActions != null)
        {
            var so = new SerializedObject(uiInputModule);
            so.FindProperty("m_ActionsAsset").objectReferenceValue = inputActions;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- MainMenuUI Canvas (built directly in scene for edit-mode visibility) ----
        try
        {
            BuildMainMenuUI(font, mapPointPrefab, slotPrefab, weaponBtnPrefab);
            Debug.Log("[MainMenuSetup] MainMenuUI hierarchy built successfully.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MainMenuSetup] BuildMainMenuUI FAILED: {e.Message}\n{e.StackTrace}");
        }

        // Save.
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[MainMenuSetup] MainMenu scene rebuilt with tabbed UI.");
    }

    // =====================================================================
    // Build the MainMenuUI hierarchy
    // =====================================================================

    private static void BuildMainMenuUI(TMP_FontAsset font,
        GameObject mapPointPrefab, GameObject slotPrefab, GameObject weaponBtnPrefab)
    {
        // Root canvas.
        var root = new GameObject("MainMenuUI");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // Full-screen dark background.
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(root.transform, false);
        bgGo.layer = 5;
        var bgImg = bgGo.AddComponent<Image>(); // Image auto-adds RectTransform.
        bgImg.color = new Color(0.04f, 0.04f, 0.06f, 1f);
        bgImg.raycastTarget = true;
        StretchFull(bgGo);

        // =========== TOP BAR ===========
        var topBar = CreatePanel(root.transform, "TopBar");
        var topBarRect = topBar.GetComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0, 1);
        topBarRect.anchorMax = new Vector2(1, 1);
        topBarRect.pivot = new Vector2(0.5f, 1);
        topBarRect.offsetMin = new Vector2(0, -60);
        topBarRect.offsetMax = Vector2.zero;
        var topBarBg = topBar.AddComponent<Image>();
        topBarBg.color = new Color(0.06f, 0.06f, 0.09f, 0.95f);
        topBarBg.raycastTarget = false;

        // --- Tab buttons (left side) ---
        var tabBar = CreatePanel(topBar.transform, "TabBar");
        var tabBarRect = tabBar.GetComponent<RectTransform>();
        tabBarRect.anchorMin = new Vector2(0, 0);
        tabBarRect.anchorMax = new Vector2(0.5f, 1);
        tabBarRect.offsetMin = new Vector2(20, 5);
        tabBarRect.offsetMax = new Vector2(0, -5);
        var tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 8;
        tabLayout.childAlignment = TextAnchor.MiddleLeft;
        tabLayout.childForceExpandWidth = false;
        tabLayout.childForceExpandHeight = true;
        tabLayout.padding = new RectOffset(0, 0, 2, 2);

        var playTabBtn   = CreateTabButton(tabBar.transform, "PlayTab",   "PLAY",   font);
        var craftTabBtn  = CreateTabButton(tabBar.transform, "CraftTab",  "CRAFT",  font);
        var exileTabBtn  = CreateTabButton(tabBar.transform, "ExileTab",  "EXILE",  font);
        var shopTabBtn   = CreateTabButton(tabBar.transform, "ShopTab",   "SHOP",   font);

        // --- Player info (right side) ---
        var playerInfo = CreatePanel(topBar.transform, "PlayerInfo");
        var playerInfoRect = playerInfo.GetComponent<RectTransform>();
        playerInfoRect.anchorMin = new Vector2(0.5f, 0);
        playerInfoRect.anchorMax = new Vector2(1, 1);
        playerInfoRect.offsetMin = new Vector2(0, 5);
        playerInfoRect.offsetMax = new Vector2(-20, -5);
        var infoLayout = playerInfo.AddComponent<HorizontalLayoutGroup>();
        infoLayout.spacing = 25;
        infoLayout.childAlignment = TextAnchor.MiddleRight;
        infoLayout.childForceExpandWidth = false;
        infoLayout.childForceExpandHeight = false;
        infoLayout.reverseArrangement = false;

        // Spacer to push items right.
        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(playerInfo.transform, false);
        spacer.layer = 5;
        spacer.AddComponent<RectTransform>();
        var spacerLayout = spacer.AddComponent<LayoutElement>();
        spacerLayout.flexibleWidth = 1;

        var currencyText = CreateInfoLabel(playerInfo.transform, "Currency", font, "\u00A4 0");
        var notifText    = CreateInfoLabel(playerInfo.transform, "Notifications", font, "\u2709 0");
        var levelText    = CreateInfoLabel(playerInfo.transform, "Level", font, "Lv. 1");
        var nameText     = CreateInfoLabel(playerInfo.transform, "PlayerName", font, "Exile");

        // =========== CONTENT AREA ===========
        var contentArea = CreatePanel(root.transform, "ContentArea");
        var contentRect = contentArea.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(0, 0);
        contentRect.offsetMax = new Vector2(0, -60); // Below top bar.

        // --- Play Panel ---
        var playPanel = CreateContentPanel(contentArea.transform, "PlayPanel");

        // Title.
        var mapTitle = CreateText(playPanel.transform, "MapTitle", font,
            new Vector2(0, 1), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            "SELECT DEPLOYMENT ZONE", 22, TextAlignmentOptions.Left,
            new Color(0.85f, 0.80f, 0.65f, 1f));
        var mapTitleRect = mapTitle.GetComponent<RectTransform>();
        mapTitleRect.anchorMin = new Vector2(0, 1);
        mapTitleRect.anchorMax = new Vector2(1, 1);
        mapTitleRect.pivot = new Vector2(0, 1);
        mapTitleRect.offsetMin = new Vector2(30, -40);
        mapTitleRect.offsetMax = new Vector2(-30, -8);

        // World Map area (fills most of the panel).
        var mapArea = new GameObject("MapArea");
        mapArea.transform.SetParent(playPanel.transform, false);
        mapArea.layer = 5;
        var mapAreaImg = mapArea.AddComponent<Image>();
        mapAreaImg.color = new Color(0.08f, 0.12f, 0.08f, 1f); // Dark terrain-like green.
        mapAreaImg.raycastTarget = false;
        var mapAreaRect = mapArea.GetComponent<RectTransform>();
        mapAreaRect.anchorMin = new Vector2(0, 0.18f);
        mapAreaRect.anchorMax = new Vector2(1, 1);
        mapAreaRect.offsetMin = new Vector2(20, 0);
        mapAreaRect.offsetMax = new Vector2(-20, -45);

        // Terrain decoration lines on the map.
        // Horizontal grid line 1.
        var gridLine1 = new GameObject("GridLine1");
        gridLine1.transform.SetParent(mapArea.transform, false);
        gridLine1.layer = 5;
        var gl1Img = gridLine1.AddComponent<Image>();
        gl1Img.color = new Color(0.15f, 0.20f, 0.15f, 0.3f);
        gl1Img.raycastTarget = false;
        var gl1Rect = gridLine1.GetComponent<RectTransform>();
        gl1Rect.anchorMin = new Vector2(0.05f, 0.33f);
        gl1Rect.anchorMax = new Vector2(0.95f, 0.335f);
        gl1Rect.offsetMin = Vector2.zero;
        gl1Rect.offsetMax = Vector2.zero;

        // Horizontal grid line 2.
        var gridLine2 = new GameObject("GridLine2");
        gridLine2.transform.SetParent(mapArea.transform, false);
        gridLine2.layer = 5;
        var gl2Img = gridLine2.AddComponent<Image>();
        gl2Img.color = new Color(0.15f, 0.20f, 0.15f, 0.3f);
        gl2Img.raycastTarget = false;
        var gl2Rect = gridLine2.GetComponent<RectTransform>();
        gl2Rect.anchorMin = new Vector2(0.05f, 0.66f);
        gl2Rect.anchorMax = new Vector2(0.95f, 0.665f);
        gl2Rect.offsetMin = Vector2.zero;
        gl2Rect.offsetMax = Vector2.zero;

        // Vertical grid line 1.
        var gridLine3 = new GameObject("GridLine3");
        gridLine3.transform.SetParent(mapArea.transform, false);
        gridLine3.layer = 5;
        var gl3Img = gridLine3.AddComponent<Image>();
        gl3Img.color = new Color(0.15f, 0.20f, 0.15f, 0.3f);
        gl3Img.raycastTarget = false;
        var gl3Rect = gridLine3.GetComponent<RectTransform>();
        gl3Rect.anchorMin = new Vector2(0.33f, 0.05f);
        gl3Rect.anchorMax = new Vector2(0.335f, 0.95f);
        gl3Rect.offsetMin = Vector2.zero;
        gl3Rect.offsetMax = Vector2.zero;

        // Vertical grid line 2.
        var gridLine4 = new GameObject("GridLine4");
        gridLine4.transform.SetParent(mapArea.transform, false);
        gridLine4.layer = 5;
        var gl4Img = gridLine4.AddComponent<Image>();
        gl4Img.color = new Color(0.15f, 0.20f, 0.15f, 0.3f);
        gl4Img.raycastTarget = false;
        var gl4Rect = gridLine4.GetComponent<RectTransform>();
        gl4Rect.anchorMin = new Vector2(0.66f, 0.05f);
        gl4Rect.anchorMax = new Vector2(0.665f, 0.95f);
        gl4Rect.offsetMin = Vector2.zero;
        gl4Rect.offsetMax = Vector2.zero;

        // Map point container (points are anchored within this).
        var mapContainer = CreatePanel(mapArea.transform, "MapPointContainer");
        StretchFull(mapContainer);

        // Map border frame.
        var mapBorder = new GameObject("MapBorder");
        mapBorder.transform.SetParent(mapArea.transform, false);
        mapBorder.layer = 5;
        var mbImg = mapBorder.AddComponent<Image>();
        mbImg.color = new Color(0.25f, 0.22f, 0.18f, 0.8f);
        mbImg.raycastTarget = false;
        // Use Outline component to create a border effect.
        var outline = mapBorder.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.22f, 0.18f, 0.6f);
        outline.effectDistance = new Vector2(2, 2);
        // Make the image itself transparent — just shows border.
        mbImg.color = new Color(0, 0, 0, 0);
        var mbRect = mapBorder.GetComponent<RectTransform>();
        mbRect.anchorMin = Vector2.zero;
        mbRect.anchorMax = Vector2.one;
        mbRect.offsetMin = Vector2.zero;
        mbRect.offsetMax = Vector2.zero;

        // Bottom detail bar.
        var detailBar = new GameObject("DetailBar");
        detailBar.transform.SetParent(playPanel.transform, false);
        detailBar.layer = 5;
        var detailBarImg = detailBar.AddComponent<Image>();
        detailBarImg.color = new Color(0.06f, 0.06f, 0.09f, 0.95f);
        detailBarImg.raycastTarget = false;
        var detailBarRect = detailBar.GetComponent<RectTransform>();
        detailBarRect.anchorMin = new Vector2(0, 0);
        detailBarRect.anchorMax = new Vector2(1, 0.18f);
        detailBarRect.offsetMin = new Vector2(20, 10);
        detailBarRect.offsetMax = new Vector2(-20, 0);

        // Biome preview color swatch (left of detail bar).
        var preview = new GameObject("BiomePreview");
        preview.transform.SetParent(detailBar.transform, false);
        preview.layer = 5;
        var previewRect = preview.AddComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0, 0.1f);
        previewRect.anchorMax = new Vector2(0, 0.9f);
        previewRect.pivot = new Vector2(0, 0.5f);
        previewRect.anchoredPosition = new Vector2(15, 0);
        previewRect.sizeDelta = new Vector2(100, 0);
        var previewImg = preview.AddComponent<Image>();
        previewImg.color = new Color(0.3f, 0.6f, 0.3f, 1f);
        previewImg.raycastTarget = false;

        // Biome name.
        var biomeName = CreateText(detailBar.transform, "BiomeName", font,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(135, 10), new Vector2(300, 30),
            "Select a zone", 24, TextAlignmentOptions.Left,
            new Color(0.95f, 0.90f, 0.75f, 1f));
        var biomeNameRect = biomeName.GetComponent<RectTransform>();
        biomeNameRect.pivot = new Vector2(0, 0.5f);

        // Biome description.
        var biomeDesc = CreateText(detailBar.transform, "BiomeDesc", font,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(135, -12), new Vector2(400, 20),
            "", 14, TextAlignmentOptions.Left,
            new Color(0.65f, 0.65f, 0.65f, 0.9f));
        var biomeDescTmp = biomeDesc.GetComponent<TextMeshProUGUI>();
        biomeDescTmp.enableWordWrapping = true;
        var biomeDescRect = biomeDesc.GetComponent<RectTransform>();
        biomeDescRect.pivot = new Vector2(0, 0.5f);

        // Difficulty.
        var biomeDiff = CreateText(detailBar.transform, "BiomeDifficulty", font,
            new Vector2(0.6f, 0.5f), new Vector2(0.6f, 0.5f),
            Vector2.zero, new Vector2(250, 25),
            "Difficulty: \u2606\u2606\u2606\u2606\u2606", 16, TextAlignmentOptions.Center,
            new Color(1f, 0.9f, 0.4f, 0.9f));

        // Launch button (right side of detail bar).
        var launchBtn = CreateMenuButton(detailBar.transform, "LaunchButton", font,
            "DEPLOY SOLO", new Color(0.15f, 0.55f, 0.25f, 1f), 200, 45);
        var launchBtnRect = launchBtn.GetComponent<RectTransform>();
        launchBtnRect.anchorMin = new Vector2(1, 0.5f);
        launchBtnRect.anchorMax = new Vector2(1, 0.5f);
        launchBtnRect.pivot = new Vector2(1, 0.5f);
        launchBtnRect.anchoredPosition = new Vector2(-15, 0);

        // --- Craft Panel (placeholder) ---
        var craftPanel = CreateContentPanel(contentArea.transform, "CraftPanel");
        craftPanel.SetActive(false);
        CreatePlaceholderLabel(craftPanel.transform, "CRAFTING", "Coming Soon", font);

        // --- Exile Panel (character info) ---
        var exilePanel = CreateContentPanel(contentArea.transform, "ExilePanel");
        exilePanel.SetActive(false);
        var exileContent = BuildExilePanel(exilePanel.transform, font);

        // --- Shop Panel (placeholder) ---
        var shopPanel = CreateContentPanel(contentArea.transform, "ShopPanel");
        shopPanel.SetActive(false);
        CreatePlaceholderLabel(shopPanel.transform, "SHOP", "Coming Soon", font);

        // --- Inventory / Loadout Overlay (Tab key, renders on top) ---
        var inventoryOverlay = BuildInventoryOverlay(root.transform, font, slotPrefab, weaponBtnPrefab);

        // =========== WIRE MainMenuUI COMPONENT ===========
        var menuUI = root.AddComponent<MainMenuUI>();
        var menuSo = new SerializedObject(menuUI);

        // Tab buttons.
        menuSo.FindProperty("_playTabButton").objectReferenceValue  = playTabBtn.GetComponent<Button>();
        menuSo.FindProperty("_craftTabButton").objectReferenceValue = craftTabBtn.GetComponent<Button>();
        menuSo.FindProperty("_exileTabButton").objectReferenceValue = exileTabBtn.GetComponent<Button>();
        menuSo.FindProperty("_shopTabButton").objectReferenceValue  = shopTabBtn.GetComponent<Button>();

        // Player info.
        menuSo.FindProperty("_currencyText").objectReferenceValue     = currencyText.GetComponent<TMP_Text>();
        menuSo.FindProperty("_notificationText").objectReferenceValue = notifText.GetComponent<TMP_Text>();
        menuSo.FindProperty("_levelText").objectReferenceValue        = levelText.GetComponent<TMP_Text>();
        menuSo.FindProperty("_playerNameText").objectReferenceValue   = nameText.GetComponent<TMP_Text>();

        // Content panels.
        menuSo.FindProperty("_playPanel").objectReferenceValue  = playPanel;
        menuSo.FindProperty("_craftPanel").objectReferenceValue = craftPanel;
        menuSo.FindProperty("_exilePanel").objectReferenceValue = exilePanel;
        menuSo.FindProperty("_shopPanel").objectReferenceValue  = shopPanel;

        // Play tab — world map.
        menuSo.FindProperty("_mapContainer").objectReferenceValue = mapContainer.GetComponent<RectTransform>();
        if (mapPointPrefab != null)
            menuSo.FindProperty("_mapPointPrefab").objectReferenceValue = mapPointPrefab;

        // Play tab — selection detail.
        menuSo.FindProperty("_selectedBiomeName").objectReferenceValue       = biomeName.GetComponent<TMP_Text>();
        menuSo.FindProperty("_selectedBiomeDesc").objectReferenceValue       = biomeDesc.GetComponent<TMP_Text>();
        menuSo.FindProperty("_selectedBiomeDifficulty").objectReferenceValue = biomeDiff.GetComponent<TMP_Text>();
        menuSo.FindProperty("_selectedBiomePreview").objectReferenceValue    = previewImg;
        menuSo.FindProperty("_launchButton").objectReferenceValue            = launchBtn.GetComponent<Button>();

        // Exile tab — character info + inventory overlay.
        menuSo.FindProperty("_exileTabUI").objectReferenceValue = exileContent;
        menuSo.FindProperty("_inventoryOverlay").objectReferenceValue = inventoryOverlay;

        menuSo.ApplyModifiedPropertiesWithoutUndo();
    }

    // =====================================================================
    // Exile Panel (character info)
    // =====================================================================

    /// <summary>
    /// Builds the Exile tab content — character name, level, title, stats,
    /// and a hint to press Tab for the inventory overlay. Returns the ExileTabUI component.
    /// </summary>
    private static ExileTabUI BuildExilePanel(Transform parent, TMP_FontAsset font)
    {
        var container = CreatePanel(parent, "ExileContent");
        StretchFull(container);

        // Title.
        CreateText(container.transform, "ExileTitle", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -25), new Vector2(400, 35),
            "YOUR EXILE", 28, TextAlignmentOptions.Center,
            new Color(0.90f, 0.85f, 0.70f, 1f));

        // Character name.
        var nameText = CreateText(container.transform, "CharacterName", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -70), new Vector2(400, 30),
            "Exile", 24, TextAlignmentOptions.Center,
            Color.white);

        // Level.
        var levelText = CreateText(container.transform, "Level", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -100), new Vector2(300, 25),
            "Level 1", 18, TextAlignmentOptions.Center,
            new Color(0.75f, 0.70f, 0.60f, 1f));

        // Title / rank.
        var titleText = CreateText(container.transform, "Title", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -125), new Vector2(300, 22),
            "Survivor", 16, TextAlignmentOptions.Center,
            new Color(0.60f, 0.55f, 0.45f, 0.9f));

        // Lifetime stats.
        var statsText = CreateText(container.transform, "Stats", font,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(400, 200),
            "Runs: 0\nExtractions: 0\nKills: 0\nTime Survived: 0m\nCurrency: 0",
            16, TextAlignmentOptions.Center,
            new Color(0.55f, 0.55f, 0.55f, 0.9f));
        var statsTmp = statsText.GetComponent<TextMeshProUGUI>();
        statsTmp.enableWordWrapping = true;

        // Hint to open inventory.
        var hintText = CreateText(container.transform, "InventoryHint", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 40), new Vector2(500, 30),
            "Press [Tab] to manage stash & loadout", 16, TextAlignmentOptions.Center,
            new Color(0.65f, 0.60f, 0.45f, 0.8f));

        // Wire ExileTabUI component.
        var exileTab = container.AddComponent<ExileTabUI>();
        var so = new SerializedObject(exileTab);
        so.FindProperty("_nameText").objectReferenceValue          = nameText.GetComponent<TMP_Text>();
        so.FindProperty("_levelText").objectReferenceValue         = levelText.GetComponent<TMP_Text>();
        so.FindProperty("_titleText").objectReferenceValue         = titleText.GetComponent<TMP_Text>();
        so.FindProperty("_statsText").objectReferenceValue         = statsText.GetComponent<TMP_Text>();
        so.FindProperty("_inventoryHintText").objectReferenceValue = hintText.GetComponent<TMP_Text>();
        so.ApplyModifiedPropertiesWithoutUndo();

        return exileTab;
    }

    // =====================================================================
    // Inventory / Loadout Overlay (Tab key) — Tarkov-style split layout
    // =====================================================================

    /// <summary>
    /// Builds the full-screen inventory overlay with stash grid on the left
    /// and equipment slots + backpack on the right. Starts hidden (alpha 0).
    /// Returns the MainMenuInventoryUI component.
    /// </summary>
    private static MainMenuInventoryUI BuildInventoryOverlay(Transform canvasRoot,
        TMP_FontAsset font, GameObject slotPrefab, GameObject weaponBtnPrefab)
    {
        // Root overlay — full screen, on top of all panels.
        var overlay = CreatePanel(canvasRoot, "InventoryOverlay");
        StretchFull(overlay);

        // Dark backdrop.
        var backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(overlay.transform, false);
        backdrop.layer = 5;
        var bdImg = backdrop.AddComponent<Image>();
        bdImg.color = new Color(0f, 0f, 0f, 0.7f);
        bdImg.raycastTarget = true;
        StretchFull(backdrop);

        // CanvasGroup for show/hide.
        var canvasGroup = overlay.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // Center container.
        var center = CreatePanel(overlay.transform, "Center");
        var centerRect = center.GetComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.05f, 0.03f);
        centerRect.anchorMax = new Vector2(0.95f, 0.97f);
        centerRect.offsetMin = Vector2.zero;
        centerRect.offsetMax = Vector2.zero;

        // ==================== LEFT PANEL — STASH ====================
        var leftPanel = CreatePanel(center.transform, "LeftPanel");
        var leftRect = leftPanel.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0.55f, 1);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;
        var leftBg = leftPanel.AddComponent<Image>();
        leftBg.color = new Color(0.07f, 0.07f, 0.10f, 0.95f);
        leftBg.raycastTarget = false;

        // Stash title.
        var stashTitle = CreateText(leftPanel.transform, "StashTitle", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -15), new Vector2(300, 28),
            "STASH (0)", 20, TextAlignmentOptions.Center,
            new Color(0.85f, 0.80f, 0.65f, 1f));

        // Stash grid area.
        var stashArea = CreatePanel(leftPanel.transform, "StashArea");
        var stashAreaRect = stashArea.GetComponent<RectTransform>();
        stashAreaRect.anchorMin = new Vector2(0.02f, 0.06f);
        stashAreaRect.anchorMax = new Vector2(0.98f, 0.92f);
        stashAreaRect.offsetMin = Vector2.zero;
        stashAreaRect.offsetMax = Vector2.zero;
        var stashBg = stashArea.AddComponent<Image>();
        stashBg.color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
        stashBg.raycastTarget = false;

        var slotGrid = CreatePanel(stashArea.transform, "SlotGrid");
        var slotGridRect = slotGrid.GetComponent<RectTransform>();
        slotGridRect.anchorMin = Vector2.zero;
        slotGridRect.anchorMax = Vector2.one;
        slotGridRect.offsetMin = new Vector2(10, 10);
        slotGridRect.offsetMax = new Vector2(-10, -10);
        var grid = slotGrid.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(65, 65);
        grid.spacing = new Vector2(6, 6);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 7;

        // Stash info text.
        var stashInfoText = CreateText(leftPanel.transform, "StashInfo", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 15), new Vector2(400, 22),
            "Items: 0  |  Value: 0g", 13, TextAlignmentOptions.Center,
            new Color(0.50f, 0.50f, 0.50f, 0.8f));

        // ==================== RIGHT PANEL — LOADOUT ====================
        var rightPanel = CreatePanel(center.transform, "RightPanel");
        var rightRect = rightPanel.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.57f, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.offsetMin = Vector2.zero;
        rightRect.offsetMax = Vector2.zero;
        var rightBg = rightPanel.AddComponent<Image>();
        rightBg.color = new Color(0.07f, 0.07f, 0.10f, 0.95f);
        rightBg.raycastTarget = false;

        // Loadout title.
        CreateText(rightPanel.transform, "LoadoutTitle", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -15), new Vector2(300, 28),
            "LOADOUT", 20, TextAlignmentOptions.Center,
            new Color(0.85f, 0.80f, 0.65f, 1f));

        // --- Equipment slots (vertical layout in upper-right) ---
        var equipArea = CreatePanel(rightPanel.transform, "EquipmentArea");
        var equipRect = equipArea.GetComponent<RectTransform>();
        equipRect.anchorMin = new Vector2(0.05f, 0.48f);
        equipRect.anchorMax = new Vector2(0.95f, 0.92f);
        equipRect.offsetMin = Vector2.zero;
        equipRect.offsetMax = Vector2.zero;
        var equipLayout = equipArea.AddComponent<VerticalLayoutGroup>();
        equipLayout.spacing = 6;
        equipLayout.childAlignment = TextAnchor.UpperCenter;
        equipLayout.childControlWidth = true;
        equipLayout.childControlHeight = true;
        equipLayout.childForceExpandWidth = true;
        equipLayout.childForceExpandHeight = false;
        equipLayout.padding = new RectOffset(5, 5, 5, 5);

        // Create 5 equipment slots.
        var headSlot     = BuildEquipmentSlot(equipArea.transform, font, EverRealm.Exiles.Data.EquipSlot.Head);
        var chestSlot    = BuildEquipmentSlot(equipArea.transform, font, EverRealm.Exiles.Data.EquipSlot.Chest);
        var legsSlot     = BuildEquipmentSlot(equipArea.transform, font, EverRealm.Exiles.Data.EquipSlot.Legs);
        var primarySlot  = BuildEquipmentSlot(equipArea.transform, font, EverRealm.Exiles.Data.EquipSlot.PrimaryWeapon);
        var secondarySlot = BuildEquipmentSlot(equipArea.transform, font, EverRealm.Exiles.Data.EquipSlot.SecondaryWeapon);

        // --- Backpack section (lower-right) ---
        var backpackTitle = CreateText(rightPanel.transform, "BackpackTitle", font,
            new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f),
            new Vector2(0, -10), new Vector2(300, 22),
            "BACKPACK (0/12)", 16, TextAlignmentOptions.Center,
            new Color(0.75f, 0.70f, 0.60f, 1f));

        var backpackArea = CreatePanel(rightPanel.transform, "BackpackArea");
        var backpackAreaRect = backpackArea.GetComponent<RectTransform>();
        backpackAreaRect.anchorMin = new Vector2(0.05f, 0.10f);
        backpackAreaRect.anchorMax = new Vector2(0.95f, 0.44f);
        backpackAreaRect.offsetMin = Vector2.zero;
        backpackAreaRect.offsetMax = Vector2.zero;
        var backpackBg = backpackArea.AddComponent<Image>();
        backpackBg.color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
        backpackBg.raycastTarget = false;

        var backpackGrid = CreatePanel(backpackArea.transform, "BackpackGrid");
        var backpackGridRect = backpackGrid.GetComponent<RectTransform>();
        backpackGridRect.anchorMin = Vector2.zero;
        backpackGridRect.anchorMax = Vector2.one;
        backpackGridRect.offsetMin = new Vector2(10, 10);
        backpackGridRect.offsetMax = new Vector2(-10, -10);
        var bpGrid = backpackGrid.AddComponent<GridLayoutGroup>();
        bpGrid.cellSize = new Vector2(65, 65);
        bpGrid.spacing = new Vector2(6, 6);
        bpGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        bpGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        bpGrid.childAlignment = TextAnchor.UpperLeft;
        bpGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        bpGrid.constraintCount = 4;

        // Loadout info text.
        var loadoutInfoText = CreateText(rightPanel.transform, "LoadoutInfo", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 15), new Vector2(400, 22),
            "Defense: 0  |  Backpack: 0/12", 13, TextAlignmentOptions.Center,
            new Color(0.50f, 0.50f, 0.50f, 0.8f));

        // ==================== CLOSE HINT ====================
        CreateText(center.transform, "CloseHint", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, -15), new Vector2(400, 22),
            "Press [Tab] to close", 13, TextAlignmentOptions.Center,
            new Color(0.45f, 0.45f, 0.45f, 0.6f));

        // ==================== TOOLTIP ====================
        var tooltipGo = CreatePanel(overlay.transform, "Tooltip");
        var tooltipRect = tooltipGo.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(260, 230);
        tooltipRect.pivot = new Vector2(0, 1);
        var tooltipBg = tooltipGo.AddComponent<Image>();
        tooltipBg.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
        tooltipBg.raycastTarget = false;

        var tooltipCg = tooltipGo.AddComponent<CanvasGroup>();
        tooltipCg.alpha = 0f;
        tooltipCg.blocksRaycasts = false;
        tooltipCg.interactable = false;

        var ttName = CreateText(tooltipGo.transform, "Name", font,
            new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            "Item Name", 18, TextAlignmentOptions.Left, Color.white);
        var ttNameRect = ttName.GetComponent<RectTransform>();
        ttNameRect.anchorMin = new Vector2(0, 1);
        ttNameRect.anchorMax = new Vector2(1, 1);
        ttNameRect.pivot = new Vector2(0, 1);
        ttNameRect.offsetMin = new Vector2(10, -30);
        ttNameRect.offsetMax = new Vector2(-10, -8);

        var ttRarity = CreateText(tooltipGo.transform, "Rarity", font,
            new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            "Common", 13, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
        var ttRarityRect = ttRarity.GetComponent<RectTransform>();
        ttRarityRect.anchorMin = new Vector2(0, 1);
        ttRarityRect.anchorMax = new Vector2(1, 1);
        ttRarityRect.pivot = new Vector2(0, 1);
        ttRarityRect.offsetMin = new Vector2(10, -50);
        ttRarityRect.offsetMax = new Vector2(-10, -32);

        var ttType = CreateText(tooltipGo.transform, "Type", font,
            new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            "Type", 13, TextAlignmentOptions.Right, new Color(0.5f, 0.5f, 0.5f));
        var ttTypeRect = ttType.GetComponent<RectTransform>();
        ttTypeRect.anchorMin = new Vector2(0, 1);
        ttTypeRect.anchorMax = new Vector2(1, 1);
        ttTypeRect.pivot = new Vector2(0, 1);
        ttTypeRect.offsetMin = new Vector2(10, -50);
        ttTypeRect.offsetMax = new Vector2(-10, -32);

        var ttDesc = CreateText(tooltipGo.transform, "Description", font,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            "", 13, TextAlignmentOptions.TopLeft, new Color(0.7f, 0.7f, 0.7f));
        var ttDescTmp = ttDesc.GetComponent<TextMeshProUGUI>();
        ttDescTmp.enableWordWrapping = true;
        var ttDescRect = ttDesc.GetComponent<RectTransform>();
        ttDescRect.anchorMin = new Vector2(0, 0);
        ttDescRect.anchorMax = new Vector2(1, 1);
        ttDescRect.offsetMin = new Vector2(10, 60);
        ttDescRect.offsetMax = new Vector2(-10, -55);

        // Equipment tooltip fields.
        var ttEquipSlot = CreateText(tooltipGo.transform, "EquipSlot", font,
            new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero,
            "", 13, TextAlignmentOptions.Left, new Color(0.3f, 0.8f, 1f));
        var ttEquipSlotRect = ttEquipSlot.GetComponent<RectTransform>();
        ttEquipSlotRect.anchorMin = new Vector2(0, 0);
        ttEquipSlotRect.anchorMax = new Vector2(1, 0);
        ttEquipSlotRect.pivot = new Vector2(0, 0);
        ttEquipSlotRect.offsetMin = new Vector2(10, 38);
        ttEquipSlotRect.offsetMax = new Vector2(-10, 55);

        var ttDefense = CreateText(tooltipGo.transform, "Defense", font,
            new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero,
            "", 13, TextAlignmentOptions.Right, new Color(0.4f, 0.9f, 0.4f));
        var ttDefenseRect = ttDefense.GetComponent<RectTransform>();
        ttDefenseRect.anchorMin = new Vector2(0, 0);
        ttDefenseRect.anchorMax = new Vector2(1, 0);
        ttDefenseRect.pivot = new Vector2(1, 0);
        ttDefenseRect.offsetMin = new Vector2(10, 38);
        ttDefenseRect.offsetMax = new Vector2(-10, 55);

        // Weapon stats — damage (left) and speed (right), same row.
        var ttDamage = CreateText(tooltipGo.transform, "Damage", font,
            new Vector2(0, 0), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero,
            "", 13, TextAlignmentOptions.Left, new Color(1f, 0.6f, 0.3f));
        var ttDamageRect = ttDamage.GetComponent<RectTransform>();
        ttDamageRect.anchorMin = new Vector2(0, 0);
        ttDamageRect.anchorMax = new Vector2(0.5f, 0);
        ttDamageRect.pivot = new Vector2(0, 0);
        ttDamageRect.offsetMin = new Vector2(10, 58);
        ttDamageRect.offsetMax = new Vector2(-5, 75);

        var ttSpeed = CreateText(tooltipGo.transform, "Speed", font,
            new Vector2(0.5f, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero,
            "", 13, TextAlignmentOptions.Right, new Color(0.6f, 0.8f, 1f));
        var ttSpeedRect = ttSpeed.GetComponent<RectTransform>();
        ttSpeedRect.anchorMin = new Vector2(0.5f, 0);
        ttSpeedRect.anchorMax = new Vector2(1, 0);
        ttSpeedRect.pivot = new Vector2(1, 0);
        ttSpeedRect.offsetMin = new Vector2(5, 58);
        ttSpeedRect.offsetMax = new Vector2(-10, 75);

        var ttValue = CreateText(tooltipGo.transform, "Value", font,
            new Vector2(0, 0), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero,
            "Value: 0g", 13, TextAlignmentOptions.Left, new Color(0.8f, 0.75f, 0.4f));
        var ttValueRect = ttValue.GetComponent<RectTransform>();
        ttValueRect.anchorMin = new Vector2(0, 0);
        ttValueRect.anchorMax = new Vector2(0.5f, 0);
        ttValueRect.pivot = new Vector2(0, 0);
        ttValueRect.offsetMin = new Vector2(10, 10);
        ttValueRect.offsetMax = new Vector2(-5, 32);

        var ttWeight = CreateText(tooltipGo.transform, "Weight", font,
            new Vector2(0.5f, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero,
            "Weight: 0.0", 13, TextAlignmentOptions.Right, new Color(0.6f, 0.6f, 0.6f));
        var ttWeightRect = ttWeight.GetComponent<RectTransform>();
        ttWeightRect.anchorMin = new Vector2(0.5f, 0);
        ttWeightRect.anchorMax = new Vector2(1, 0);
        ttWeightRect.pivot = new Vector2(1, 0);
        ttWeightRect.offsetMin = new Vector2(5, 10);
        ttWeightRect.offsetMax = new Vector2(-10, 32);

        // Wire ItemTooltipUI.
        var tooltip = tooltipGo.AddComponent<ItemTooltipUI>();
        var ttSo = new SerializedObject(tooltip);
        ttSo.FindProperty("_canvasGroup").objectReferenceValue      = tooltipCg;
        ttSo.FindProperty("_panelRect").objectReferenceValue        = tooltipRect;
        ttSo.FindProperty("_nameText").objectReferenceValue         = ttName.GetComponent<TMP_Text>();
        ttSo.FindProperty("_rarityText").objectReferenceValue       = ttRarity.GetComponent<TMP_Text>();
        ttSo.FindProperty("_typeText").objectReferenceValue         = ttType.GetComponent<TMP_Text>();
        ttSo.FindProperty("_descriptionText").objectReferenceValue  = ttDesc.GetComponent<TMP_Text>();
        ttSo.FindProperty("_valueText").objectReferenceValue        = ttValue.GetComponent<TMP_Text>();
        ttSo.FindProperty("_weightText").objectReferenceValue       = ttWeight.GetComponent<TMP_Text>();
        ttSo.FindProperty("_defenseText").objectReferenceValue      = ttDefense.GetComponent<TMP_Text>();
        ttSo.FindProperty("_equipSlotText").objectReferenceValue    = ttEquipSlot.GetComponent<TMP_Text>();
        ttSo.FindProperty("_damageText").objectReferenceValue       = ttDamage.GetComponent<TMP_Text>();
        ttSo.FindProperty("_speedText").objectReferenceValue        = ttSpeed.GetComponent<TMP_Text>();
        ttSo.ApplyModifiedPropertiesWithoutUndo();

        // ==================== WIRE MainMenuInventoryUI ====================
        var inventoryUI = overlay.AddComponent<MainMenuInventoryUI>();
        var invSo = new SerializedObject(inventoryUI);

        invSo.FindProperty("_canvasGroup").objectReferenceValue          = canvasGroup;
        invSo.FindProperty("_stashSlotContainer").objectReferenceValue   = slotGridRect;
        if (slotPrefab != null)
            invSo.FindProperty("_stashSlotPrefab").objectReferenceValue  = slotPrefab;
        invSo.FindProperty("_stashTitle").objectReferenceValue           = stashTitle.GetComponent<TMP_Text>();

        invSo.FindProperty("_headSlot").objectReferenceValue             = headSlot;
        invSo.FindProperty("_chestSlot").objectReferenceValue            = chestSlot;
        invSo.FindProperty("_legsSlot").objectReferenceValue             = legsSlot;
        invSo.FindProperty("_primaryWeaponSlot").objectReferenceValue    = primarySlot;
        invSo.FindProperty("_secondaryWeaponSlot").objectReferenceValue  = secondarySlot;

        invSo.FindProperty("_backpackSlotContainer").objectReferenceValue = backpackGridRect;
        invSo.FindProperty("_backpackTitle").objectReferenceValue        = backpackTitle.GetComponent<TMP_Text>();

        invSo.FindProperty("_stashInfoText").objectReferenceValue        = stashInfoText.GetComponent<TMP_Text>();
        invSo.FindProperty("_loadoutInfoText").objectReferenceValue      = loadoutInfoText.GetComponent<TMP_Text>();
        invSo.FindProperty("_tooltip").objectReferenceValue              = tooltip;

        invSo.ApplyModifiedPropertiesWithoutUndo();

        return inventoryUI;
    }

    /// <summary>
    /// Builds a single equipment slot (background + icon + label)
    /// and wires the EquipmentSlotUI component. Returns the component.
    /// </summary>
    private static EquipmentSlotUI BuildEquipmentSlot(Transform parent, TMP_FontAsset font,
        EverRealm.Exiles.Data.EquipSlot slotType)
    {
        string slotName = slotType.ToString();

        var go = new GameObject(slotName + "Slot");
        go.transform.SetParent(parent, false);
        go.layer = 5;

        var layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = 55;
        layout.flexibleWidth = 1;

        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 55);

        // Background.
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.10f, 0.14f, 0.9f);
        bg.raycastTarget = true;

        // Border.
        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(go.transform, false);
        borderGo.layer = 5;
        var borderRect = borderGo.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2, -2);
        borderRect.offsetMax = new Vector2(2, 2);
        var borderImg = borderGo.AddComponent<Image>();
        borderImg.color = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        borderImg.raycastTarget = false;
        borderGo.transform.SetAsFirstSibling();

        // Icon.
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        iconGo.layer = 5;
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.1f);
        iconRect.anchorMax = new Vector2(0, 0.9f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(10, 0);
        iconRect.sizeDelta = new Vector2(40, 0);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        iconImg.enabled = false;

        // Slot label.
        string displayName = slotType switch
        {
            EverRealm.Exiles.Data.EquipSlot.Head            => "HEAD",
            EverRealm.Exiles.Data.EquipSlot.Chest           => "CHEST",
            EverRealm.Exiles.Data.EquipSlot.Legs            => "LEGS",
            EverRealm.Exiles.Data.EquipSlot.PrimaryWeapon   => "PRIMARY WEAPON",
            EverRealm.Exiles.Data.EquipSlot.SecondaryWeapon => "SECONDARY WEAPON",
            _                                                => "SLOT"
        };

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        labelGo.layer = 5;
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(60, 5);
        labelRect.offsetMax = new Vector2(-10, -5);
        var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.font = font;
        labelTmp.text = displayName;
        labelTmp.fontSize = 14;
        labelTmp.alignment = TextAlignmentOptions.Left;
        labelTmp.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        labelTmp.raycastTarget = false;

        // Wire EquipmentSlotUI component.
        var equipSlot = go.AddComponent<EquipmentSlotUI>();
        var so = new SerializedObject(equipSlot);
        so.FindProperty("_slotType").enumValueIndex = (int)slotType;
        so.FindProperty("_icon").objectReferenceValue       = iconImg;
        so.FindProperty("_border").objectReferenceValue     = borderImg;
        so.FindProperty("_background").objectReferenceValue = bg;
        so.FindProperty("_slotLabel").objectReferenceValue  = labelTmp;
        so.ApplyModifiedPropertiesWithoutUndo();

        return equipSlot;
    }

    // =====================================================================
    // Fix Game Scene
    // =====================================================================

    [MenuItem("Tools/EverRealm/Fix Game Scene (Remove Bootstrap)")]
    public static void FixGameScene()
    {
        if (!EditorUtility.DisplayDialog(
                "Fix Game Scene",
                "This will remove GameBootstrap and StashManager components from the Game scene.\n" +
                "RunManager will remain on GameManager.\n\nContinue?",
                "Fix", "Cancel"))
        {
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();
        bool removedAny = false;

        foreach (var root in roots)
        {
            foreach (var bootstrap in root.GetComponentsInChildren<GameBootstrap>(true))
            {
                Debug.Log($"[MainMenuSetup] Removing GameBootstrap from '{bootstrap.gameObject.name}'");
                Object.DestroyImmediate(bootstrap);
                EditorUtility.SetDirty(root);
                removedAny = true;
            }
            foreach (var sm in root.GetComponentsInChildren<StashManager>(true))
            {
                Debug.Log($"[MainMenuSetup] Removing StashManager from '{sm.gameObject.name}'");
                Object.DestroyImmediate(sm);
                EditorUtility.SetDirty(root);
                removedAny = true;
            }
        }

        if (!removedAny)
            Debug.Log("[MainMenuSetup] No GameBootstrap or StashManager found — already clean.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MainMenuSetup] Game scene fixed.");
    }

    // =====================================================================
    // UI Helpers
    // =====================================================================

    private static GameObject CreatePanel(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = 5;
        go.AddComponent<RectTransform>();
        return go;
    }

    private static GameObject CreateContentPanel(Transform parent, string name)
    {
        var go = CreatePanel(parent, name);
        StretchFull(go);
        return go;
    }

    private static void StretchFull(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect == null)
            rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreateTabButton(Transform parent, string name, string label, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = 5;

        var layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = 120;
        layout.preferredHeight = 45;

        go.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 45);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.16f, 0.9f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        textGo.layer = 5;
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        tmp.raycastTarget = false;
        tmp.fontStyle = FontStyles.Bold;

        return go;
    }

    private static GameObject CreateInfoLabel(Transform parent, string name, TMP_FontAsset font, string defaultText)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = 5;

        var layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = 120;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 40);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = defaultText;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        tmp.raycastTarget = false;

        return go;
    }

    private static void CreatePlaceholderLabel(Transform parent, string title, string subtitle, TMP_FontAsset font)
    {
        CreateText(parent, "Title", font,
            new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
            Vector2.zero, new Vector2(400, 50),
            title, 36, TextAlignmentOptions.Center,
            new Color(0.4f, 0.4f, 0.4f, 0.6f));

        CreateText(parent, "Subtitle", font,
            new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f),
            Vector2.zero, new Vector2(300, 30),
            subtitle, 22, TextAlignmentOptions.Center,
            new Color(0.35f, 0.35f, 0.35f, 0.5f));
    }

    private static GameObject CreateMenuButton(Transform parent, string name, TMP_FontAsset font,
        string label, Color bgColor, float width, float height)
    {
        var btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);
        btnGo.layer = 5;

        var layoutElem = btnGo.AddComponent<LayoutElement>();
        layoutElem.preferredWidth = width;
        layoutElem.preferredHeight = height;

        btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = bgColor;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = colors;

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(btnGo.transform, false);
        textGo.layer = 5;
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5, 5);
        textRect.offsetMax = new Vector2(-5, -5);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.fontStyle = FontStyles.Bold;

        return btnGo;
    }

    private static GameObject CreateText(Transform parent, string name, TMP_FontAsset font,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta,
        string defaultText, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = 5;

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        return go;
    }

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
