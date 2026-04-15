#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.UI;

/// <summary>
/// Editor utility that generates Phase 9 assets: ScriptableObject registries,
/// UI prefabs (HideoutUI hub, WeaponButton), updates RunSummaryUI with a Continue
/// button, and wires all references.
/// Run via Tools > EverRealm > Generate Phase 9 Assets.
/// </summary>
public static class Phase9PrefabGenerator
{
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string SOFolder = "Assets/ScriptableObjects";
    private const string ItemsFolder = "Assets/ScriptableObjects/Items";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/EverRealm/Generate Phase 9 Assets")]
    public static void Generate()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError("[Phase9Gen] Could not load TMP font at " + FontPath);
            return;
        }

        EnsureFolder(PrefabFolder);

        // Step 1: Create ScriptableObject registries.
        var itemRegistry = CreateItemRegistry();
        var weaponRegistry = CreateWeaponRegistry();

        // Step 2: Set WeaponId on existing Sword.asset if empty.
        SetSwordWeaponId();

        // Step 3: Create UI prefabs.
        var inventorySlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/InventorySlot.prefab");
        var weaponButtonPrefab = CreateWeaponButtonPrefab(font);
        var hideoutPrefab = CreateHideoutPrefab(font, inventorySlotPrefab, weaponButtonPrefab);

        // Step 4: Update RunSummaryUI with Continue button.
        UpdateRunSummaryPrefab(font);

        // Step 5: Wire scene objects.
        WireSceneObjects(hideoutPrefab, itemRegistry, weaponRegistry);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Phase9Gen] All Phase 9 assets generated and wired successfully.");
        Debug.Log("[Phase9Gen] NOTE: Open the MainMenu scene and re-run to wire MainMenuController.");
    }

    // =====================================================================
    // ScriptableObject Registries
    // =====================================================================

    private static ItemRegistry CreateItemRegistry()
    {
        string path = $"{SOFolder}/ItemRegistry.asset";

        var existing = AssetDatabase.LoadAssetAtPath<ItemRegistry>(path);

        var registry = existing ?? ScriptableObject.CreateInstance<ItemRegistry>();

        // Auto-discover all ItemDefinition assets.
        var guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { ItemsFolder });
        var items = new ItemDefinition[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            items[i] = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
        }

        var so = new SerializedObject(registry);
        var prop = so.FindProperty("_items");
        prop.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        if (existing == null)
            AssetDatabase.CreateAsset(registry, path);

        EditorUtility.SetDirty(registry);
        Debug.Log($"[Phase9Gen] ItemRegistry: {items.Length} items registered.");
        return registry;
    }

    private static WeaponRegistry CreateWeaponRegistry()
    {
        string path = $"{SOFolder}/WeaponRegistry.asset";

        var existing = AssetDatabase.LoadAssetAtPath<WeaponRegistry>(path);

        var registry = existing ?? ScriptableObject.CreateInstance<WeaponRegistry>();

        // Auto-discover all WeaponDefinition assets.
        var guids = AssetDatabase.FindAssets("t:WeaponDefinition");
        var weapons = new WeaponDefinition[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            weapons[i] = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(assetPath);
        }

        var so = new SerializedObject(registry);
        var prop = so.FindProperty("_weapons");
        prop.arraySize = weapons.Length;
        for (int i = 0; i < weapons.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = weapons[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        if (existing == null)
            AssetDatabase.CreateAsset(registry, path);

        EditorUtility.SetDirty(registry);
        Debug.Log($"[Phase9Gen] WeaponRegistry: {weapons.Length} weapons registered.");
        return registry;
    }

    private static void SetSwordWeaponId()
    {
        string swordPath = $"{SOFolder}/Sword.asset";
        var sword = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(swordPath);
        if (sword == null)
        {
            Debug.LogWarning("[Phase9Gen] Sword.asset not found — skipping WeaponId assignment.");
            return;
        }

        if (string.IsNullOrEmpty(sword.WeaponId))
        {
            sword.WeaponId = "sword_iron";
            EditorUtility.SetDirty(sword);
            Debug.Log("[Phase9Gen] Set Sword.asset WeaponId to 'sword_iron'.");
        }
    }

    // =====================================================================
    // WeaponButton Prefab
    // =====================================================================

    private static GameObject CreateWeaponButtonPrefab(TMP_FontAsset font)
    {
        string path = $"{PrefabFolder}/WeaponButton.prefab";

        var root = new GameObject("WeaponButton");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(180, 60);

        var border = root.AddComponent<Image>();
        border.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        var inner = new GameObject("Inner");
        inner.transform.SetParent(root.transform, false);
        var innerRect = inner.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(3, 3);
        innerRect.offsetMax = new Vector2(-3, -3);
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        innerImg.raycastTarget = false;

        var nameGo = new GameObject("WeaponName");
        nameGo.transform.SetParent(root.transform, false);
        var nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = Vector2.one;
        nameRect.offsetMin = new Vector2(10, 5);
        nameRect.offsetMax = new Vector2(-10, -5);
        var nameText = nameGo.AddComponent<TextMeshProUGUI>();
        nameText.font = font;
        nameText.text = "Weapon";
        nameText.fontSize = 22;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(0.9f, 0.85f, 0.7f, 1f);
        nameText.raycastTarget = false;
        nameText.enableWordWrapping = false;

        var button = root.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        button.colors = colors;
        button.targetGraphic = border;

        var btnUI = root.AddComponent<WeaponButtonUI>();
        var so = new SerializedObject(btnUI);
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_border").objectReferenceValue = border;
        so.FindProperty("_button").objectReferenceValue = button;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Debug.Log($"[Phase9Gen] Created {path}");
        return prefab;
    }

    // =====================================================================
    // HideoutUI Prefab (Main Menu Hub)
    // =====================================================================

    private static GameObject CreateHideoutPrefab(TMP_FontAsset font, GameObject slotPrefab, GameObject weaponBtnPrefab)
    {
        string path = $"{PrefabFolder}/HideoutUI.prefab";

        // Root Canvas.
        var root = new GameObject("HideoutUI");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // Full-screen dark background.
        var bg = new GameObject("Background");
        bg.transform.SetParent(root.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.04f, 0.06f, 1f);
        bgImg.raycastTarget = true;

        // ----- Title -----
        CreateText(bg.transform, "Title", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -40), new Vector2(600, 50),
            "EVERREALM: EXILES", 38, TextAlignmentOptions.Center,
            new Color(0.9f, 0.85f, 0.7f, 1f));

        // ----- Stats line -----
        var stats = CreateText(bg.transform, "Stats", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -85), new Vector2(800, 25),
            "Runs: 0  |  Extractions: 0  |  Kills: 0  |  Time: 0m",
            16, TextAlignmentOptions.Center,
            new Color(0.55f, 0.55f, 0.55f, 0.9f));

        // ----- Stash Section -----
        var stashTitle = CreateText(bg.transform, "StashTitle", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -125), new Vector2(300, 30),
            "STASH (0)", 20, TextAlignmentOptions.Center,
            new Color(0.75f, 0.7f, 0.6f, 1f));

        // Stash grid container with background.
        var stashContainer = new GameObject("StashContainer");
        stashContainer.transform.SetParent(bg.transform, false);
        var stashRect = stashContainer.AddComponent<RectTransform>();
        stashRect.anchorMin = new Vector2(0.1f, 0.38f);
        stashRect.anchorMax = new Vector2(0.9f, 0.85f);
        stashRect.offsetMin = Vector2.zero;
        stashRect.offsetMax = new Vector2(0, -10);
        var stashBg = stashContainer.AddComponent<Image>();
        stashBg.color = new Color(0.06f, 0.06f, 0.09f, 0.8f);
        stashBg.raycastTarget = false;

        var slotGrid = new GameObject("SlotGrid");
        slotGrid.transform.SetParent(stashContainer.transform, false);
        var slotGridRect = slotGrid.AddComponent<RectTransform>();
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

        // ----- Loadout Section -----
        CreateText(bg.transform, "LoadoutTitle", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 250), new Vector2(300, 25),
            "LOADOUT", 18, TextAlignmentOptions.Center,
            new Color(0.75f, 0.7f, 0.6f, 1f));

        var weaponRow = new GameObject("WeaponRow");
        weaponRow.transform.SetParent(bg.transform, false);
        var weaponRowRect = weaponRow.AddComponent<RectTransform>();
        weaponRowRect.anchorMin = new Vector2(0.5f, 0);
        weaponRowRect.anchorMax = new Vector2(0.5f, 0);
        weaponRowRect.pivot = new Vector2(0.5f, 0);
        weaponRowRect.anchoredPosition = new Vector2(0, 185);
        weaponRowRect.sizeDelta = new Vector2(800, 55);

        var hLayout = weaponRow.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 12;
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        // ----- Play Buttons Section -----
        CreateText(bg.transform, "PlayTitle", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 150), new Vector2(300, 25),
            "PLAY", 18, TextAlignmentOptions.Center,
            new Color(0.75f, 0.7f, 0.6f, 1f));

        // Button row.
        var buttonRow = new GameObject("ButtonRow");
        buttonRow.transform.SetParent(bg.transform, false);
        var buttonRowRect = buttonRow.AddComponent<RectTransform>();
        buttonRowRect.anchorMin = new Vector2(0.5f, 0);
        buttonRowRect.anchorMax = new Vector2(0.5f, 0);
        buttonRowRect.pivot = new Vector2(0.5f, 0);
        buttonRowRect.anchoredPosition = new Vector2(0, 40);
        buttonRowRect.sizeDelta = new Vector2(520, 80);

        var btnLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        btnLayout.spacing = 20;
        btnLayout.childAlignment = TextAnchor.MiddleCenter;
        btnLayout.childForceExpandWidth = false;
        btnLayout.childForceExpandHeight = false;

        // Solo button.
        var soloBtn = CreateMenuButton(buttonRow.transform, "SoloButton", font,
            "SOLO", new Color(0.15f, 0.55f, 0.25f, 1f), 250, 70);

        // Multiplayer button (disabled).
        var multiBtn = CreateMenuButton(buttonRow.transform, "MultiplayerButton", font,
            "MULTIPLAYER", new Color(0.25f, 0.25f, 0.3f, 1f), 250, 70);

        // Add "Coming Soon" sub-label to multiplayer.
        var comingSoon = CreateText(multiBtn.transform, "ComingSoon", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 8), new Vector2(200, 18),
            "COMING SOON", 11, TextAlignmentOptions.Center,
            new Color(0.6f, 0.6f, 0.6f, 0.8f));

        // ----- Attach HideoutUI component and wire fields -----
        var hideout = root.AddComponent<HideoutUI>();
        var so = new SerializedObject(hideout);
        so.FindProperty("_stashSlotContainer").objectReferenceValue = slotGridRect;
        so.FindProperty("_stashSlotPrefab").objectReferenceValue = slotPrefab;
        so.FindProperty("_stashTitle").objectReferenceValue = stashTitle.GetComponent<TMP_Text>();
        so.FindProperty("_weaponListContainer").objectReferenceValue = weaponRowRect;
        so.FindProperty("_weaponButtonPrefab").objectReferenceValue = weaponBtnPrefab;
        so.FindProperty("_statsText").objectReferenceValue = stats.GetComponent<TMP_Text>();
        so.FindProperty("_soloButton").objectReferenceValue = soloBtn.GetComponent<Button>();
        so.FindProperty("_multiplayerButton").objectReferenceValue = multiBtn.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Debug.Log($"[Phase9Gen] Created {path}");
        return prefab;
    }

    // =====================================================================
    // Update RunSummaryUI Prefab
    // =====================================================================

    private static void UpdateRunSummaryPrefab(TMP_FontAsset font)
    {
        string path = $"{PrefabFolder}/RunSummaryUI.prefab";

        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefabAsset == null)
        {
            Debug.LogWarning("[Phase9Gen] RunSummaryUI.prefab not found — skipping Continue button.");
            return;
        }

        var summaryUI = prefabAsset.GetComponent<RunSummaryUI>();
        if (summaryUI == null)
        {
            Debug.LogWarning("[Phase9Gen] RunSummaryUI component not found on prefab.");
            return;
        }

        var checkSo = new SerializedObject(summaryUI);
        var existingBtn = checkSo.FindProperty("_continueButton");
        if (existingBtn != null && existingBtn.objectReferenceValue != null)
        {
            Debug.Log("[Phase9Gen] RunSummaryUI already has Continue button — skipping.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(prefabAsset);
        var prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

        var bgTransform = prefabRoot.transform.Find("Background");
        if (bgTransform == null)
            bgTransform = prefabRoot.transform;

        // Create the Continue button.
        var btnGo = new GameObject("ContinueButton");
        btnGo.transform.SetParent(bgTransform, false);
        btnGo.layer = 5;

        var btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0);
        btnRect.anchorMax = new Vector2(0.5f, 0);
        btnRect.pivot = new Vector2(0.5f, 0);
        btnRect.anchoredPosition = new Vector2(0, 40);
        btnRect.sizeDelta = new Vector2(220, 50);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.5f, 0.8f, 1f);

        var button = btnGo.AddComponent<Button>();
        button.targetGraphic = btnImg;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.9f, 1f, 1f);
        colors.pressedColor = new Color(0.6f, 0.7f, 0.85f, 1f);
        button.colors = colors;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        textGo.layer = 5;
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = "CONTINUE";
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        var prefabSummaryUI = prefabRoot.GetComponent<RunSummaryUI>();
        if (prefabSummaryUI != null)
        {
            var so = new SerializedObject(prefabSummaryUI);
            so.FindProperty("_continueButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log("[Phase9Gen] Added Continue button to RunSummaryUI prefab.");
    }

    // =====================================================================
    // Scene Wiring
    // =====================================================================

    private static void WireSceneObjects(GameObject hideoutPrefab, ItemRegistry itemRegistry, WeaponRegistry weaponRegistry)
    {
        // Wire StashManager on GameBootstrap (works in either scene).
        var bootstrap = Object.FindObjectOfType<GameBootstrap>();
        if (bootstrap != null)
        {
            var stash = bootstrap.GetComponent<StashManager>();
            if (stash == null)
                stash = bootstrap.gameObject.AddComponent<StashManager>();

            var so = new SerializedObject(stash);
            so.FindProperty("_itemRegistry").objectReferenceValue = itemRegistry;
            so.FindProperty("_weaponRegistry").objectReferenceValue = weaponRegistry;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stash);
            EditorUtility.SetDirty(bootstrap.gameObject);
            Debug.Log("[Phase9Gen] Wired StashManager on GameBootstrap with registries.");
        }
        else
        {
            Debug.LogWarning("[Phase9Gen] GameBootstrap not found in scene — open the Game or MainMenu scene and re-run.");
        }

        // Wire MainMenuController if present in this scene.
        var menuCtrl = Object.FindObjectOfType<MainMenuController>();
        if (menuCtrl != null)
        {
            var so = new SerializedObject(menuCtrl);
            so.FindProperty("_hubUiPrefab").objectReferenceValue = hideoutPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menuCtrl);
            Debug.Log("[Phase9Gen] Wired HideoutUI prefab to MainMenuController.");
        }
        else
        {
            Debug.Log("[Phase9Gen] MainMenuController not found — open the MainMenu scene and re-run to wire hub prefab.");
        }
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static GameObject CreateMenuButton(Transform parent, string name, TMP_FontAsset font,
        string label, Color bgColor, float width, float height)
    {
        var btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);
        btnGo.layer = 5;

        var layoutElem = btnGo.AddComponent<LayoutElement>();
        layoutElem.preferredWidth = width;
        layoutElem.preferredHeight = height;

        var btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(width, height);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = bgColor;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
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
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;

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
