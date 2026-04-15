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

        // --- Exile Panel (stash + loadout + stats) ---
        var exilePanel = CreateContentPanel(contentArea.transform, "ExilePanel");
        exilePanel.SetActive(false);
        var exileContent = BuildExilePanel(exilePanel.transform, font, slotPrefab, weaponBtnPrefab);

        // --- Shop Panel (placeholder) ---
        var shopPanel = CreateContentPanel(contentArea.transform, "ShopPanel");
        shopPanel.SetActive(false);
        CreatePlaceholderLabel(shopPanel.transform, "SHOP", "Coming Soon", font);

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

        // Exile tab — HideoutUI.
        menuSo.FindProperty("_hideoutUI").objectReferenceValue = exileContent;

        menuSo.ApplyModifiedPropertiesWithoutUndo();
    }

    // =====================================================================
    // Exile Panel (stash + loadout + stats)
    // =====================================================================

    /// <summary>
    /// Builds the Exile tab content — a simplified version of the old HideoutUI
    /// showing stash, weapon loadout, and stats. Returns the HideoutUI component.
    /// </summary>
    private static HideoutUI BuildExilePanel(Transform parent, TMP_FontAsset font,
        GameObject slotPrefab, GameObject weaponBtnPrefab)
    {
        var container = CreatePanel(parent, "ExileContent");
        StretchFull(container);

        // Title.
        CreateText(container.transform, "ExileTitle", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -25), new Vector2(400, 35),
            "YOUR EXILE", 28, TextAlignmentOptions.Center,
            new Color(0.90f, 0.85f, 0.70f, 1f));

        // Stats.
        var statsText = CreateText(container.transform, "Stats", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -60), new Vector2(800, 25),
            "Runs: 0  |  Extractions: 0  |  Kills: 0  |  Time: 0m",
            16, TextAlignmentOptions.Center,
            new Color(0.55f, 0.55f, 0.55f, 0.9f));

        // Stash title.
        var stashTitle = CreateText(container.transform, "StashTitle", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -95), new Vector2(300, 25),
            "STASH (0)", 18, TextAlignmentOptions.Center,
            new Color(0.75f, 0.70f, 0.60f, 1f));

        // Stash grid.
        var stashContainer = CreatePanel(container.transform, "StashContainer");
        var stashRect = stashContainer.GetComponent<RectTransform>();
        stashRect.anchorMin = new Vector2(0.1f, 0.30f);
        stashRect.anchorMax = new Vector2(0.9f, 0.83f);
        stashRect.offsetMin = Vector2.zero;
        stashRect.offsetMax = Vector2.zero;
        var stashBg = stashContainer.AddComponent<Image>();
        stashBg.color = new Color(0.06f, 0.06f, 0.09f, 0.8f);
        stashBg.raycastTarget = false;

        var slotGrid = CreatePanel(stashContainer.transform, "SlotGrid");
        var slotGridRect = slotGrid.GetComponent<RectTransform>();
        slotGridRect.anchorMin = Vector2.zero;
        slotGridRect.anchorMax = Vector2.one;
        slotGridRect.offsetMin = new Vector2(15, 15);
        slotGridRect.offsetMax = new Vector2(-15, -15);
        var grid = slotGrid.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(70, 70);
        grid.spacing = new Vector2(8, 8);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;

        // Loadout title.
        CreateText(container.transform, "LoadoutTitle", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 205), new Vector2(300, 25),
            "LOADOUT", 18, TextAlignmentOptions.Center,
            new Color(0.75f, 0.70f, 0.60f, 1f));

        // Weapon row.
        var weaponRow = CreatePanel(container.transform, "WeaponRow");
        var weaponRowRect = weaponRow.GetComponent<RectTransform>();
        weaponRowRect.anchorMin = new Vector2(0.5f, 0);
        weaponRowRect.anchorMax = new Vector2(0.5f, 0);
        weaponRowRect.pivot = new Vector2(0.5f, 0);
        weaponRowRect.anchoredPosition = new Vector2(0, 140);
        weaponRowRect.sizeDelta = new Vector2(800, 55);
        var hLayout = weaponRow.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 12;
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        // Wire HideoutUI component (no Solo/Multiplayer buttons — launch is in Play tab).
        var hideout = container.AddComponent<HideoutUI>();
        var so = new SerializedObject(hideout);
        so.FindProperty("_stashSlotContainer").objectReferenceValue = slotGridRect;
        if (slotPrefab != null)
            so.FindProperty("_stashSlotPrefab").objectReferenceValue = slotPrefab;
        so.FindProperty("_stashTitle").objectReferenceValue         = stashTitle.GetComponent<TMP_Text>();
        so.FindProperty("_weaponListContainer").objectReferenceValue = weaponRowRect;
        if (weaponBtnPrefab != null)
            so.FindProperty("_weaponButtonPrefab").objectReferenceValue = weaponBtnPrefab;
        so.FindProperty("_statsText").objectReferenceValue           = statsText.GetComponent<TMP_Text>();
        // Solo/Multiplayer buttons left null — handled gracefully by HideoutUI.Show().
        so.ApplyModifiedPropertiesWithoutUndo();

        return hideout;
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
