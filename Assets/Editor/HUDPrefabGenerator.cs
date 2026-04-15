#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using EverRealm.Exiles.UI;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.AI;

/// <summary>
/// Editor utility that generates HUD prefabs and wires references.
/// Run via Tools > EverRealm > Generate HUD Prefabs.
/// </summary>
public static class HUDPrefabGenerator
{
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string GruntPrefabPath = "Assets/Prefabs/Grunt.prefab";

    [MenuItem("Tools/EverRealm/Generate HUD Prefabs")]
    public static void Generate()
    {
        EnsureFolder(PrefabFolder);

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError("[HUDPrefabGenerator] Could not load TMP font at " + FontPath);
            return;
        }

        var crosshairSprite = CreateCrosshairSprite();
        var inventorySlotPrefab = CreateInventorySlotPrefab(font);
        var gameHudPrefab = CreateGameHUDPrefab(font, crosshairSprite, inventorySlotPrefab);
        var enemyHealthBarPrefab = CreateEnemyHealthBarPrefab(font);

        WireRunManager(gameHudPrefab);
        WireEnemyPrefab(enemyHealthBarPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[HUDPrefabGenerator] All HUD prefabs generated and wired successfully.");
    }

    // =====================================================================
    // GameHUD Prefab
    // =====================================================================

    private static GameObject CreateGameHUDPrefab(TMP_FontAsset font, Sprite crosshairSprite, GameObject inventorySlotPrefab)
    {
        string path = $"{PrefabFolder}/GameHUD.prefab";

        // Root Canvas
        var root = new GameObject("GameHUD");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // ---- Health Bar (bottom-left) ----
        var healthBar = CreateBar(root.transform, "HealthBar",
            new Vector2(0, 0), new Vector2(0, 0),    // anchors bottom-left
            new Vector2(30, 80),                       // offset min (moved up)
            new Vector2(330, 110),                     // offset max
            new Color(0.8f, 0.15f, 0.15f, 1f),        // red fill
            new Color(0.15f, 0.15f, 0.15f, 0.8f));    // dark bg

        // ---- Stamina Bar (below health) ----
        var staminaBar = CreateBar(root.transform, "StaminaBar",
            new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(30, 50),                       // offset min (moved up)
            new Vector2(330, 75),                      // offset max
            new Color(0.85f, 0.75f, 0.15f, 1f),       // yellow fill
            new Color(0.15f, 0.15f, 0.15f, 0.8f));

        // ---- Crosshair (center) ----
        var crosshair = new GameObject("Crosshair");
        crosshair.transform.SetParent(root.transform, false);
        var crosshairRect = crosshair.AddComponent<RectTransform>();
        crosshairRect.anchorMin = new Vector2(0.5f, 0.5f);
        crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairRect.sizeDelta = new Vector2(4, 4);
        crosshairRect.anchoredPosition = Vector2.zero;
        var crosshairImg = crosshair.AddComponent<Image>();
        crosshairImg.sprite = crosshairSprite;
        crosshairImg.color = new Color(1f, 1f, 1f, 0.8f);
        crosshairImg.raycastTarget = false;

        // ---- Interaction Prompt (center-bottom) ----
        var interactPrompt = CreateText(root.transform, "InteractPrompt", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 120), new Vector2(500, 40),
            "[E] Interact", 22, TextAlignmentOptions.Center,
            new Color(1f, 1f, 1f, 0.9f));
        AddOutline(interactPrompt);

        // ---- Top-Right Panel ----
        var topRight = new GameObject("TopRight");
        topRight.transform.SetParent(root.transform, false);
        var topRightRect = topRight.AddComponent<RectTransform>();
        topRightRect.anchorMin = new Vector2(1, 1);
        topRightRect.anchorMax = new Vector2(1, 1);
        topRightRect.pivot = new Vector2(1, 1);
        topRightRect.anchoredPosition = new Vector2(-20, -20);
        topRightRect.sizeDelta = new Vector2(200, 80);

        var runTimer = CreateText(topRight.transform, "RunTimer", font,
            new Vector2(0, 1), new Vector2(1, 1),
            Vector2.zero, new Vector2(0, 30),
            "00:00", 24, TextAlignmentOptions.Right,
            new Color(1f, 1f, 1f, 0.85f));
        var runTimerRect = runTimer.GetComponent<RectTransform>();
        runTimerRect.anchoredPosition = new Vector2(0, -5);
        AddOutline(runTimer);

        var killCounter = CreateText(topRight.transform, "KillCounter", font,
            new Vector2(0, 1), new Vector2(1, 1),
            Vector2.zero, new Vector2(0, 25),
            "Kills: 0", 20, TextAlignmentOptions.Right,
            new Color(1f, 1f, 1f, 0.75f));
        var killCounterRect = killCounter.GetComponent<RectTransform>();
        killCounterRect.anchoredPosition = new Vector2(0, -38);
        AddOutline(killCounter);

        // ---- Loot Notification (lower-center) ----
        var lootNotification = CreateText(root.transform, "LootNotification", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 170), new Vector2(400, 30),
            "", 20, TextAlignmentOptions.Center,
            new Color(0.9f, 0.85f, 0.3f, 0f)); // starts invisible
        AddOutline(lootNotification);

        // ---- Extraction Countdown (center-top area) ----
        var extractionPanel = new GameObject("ExtractionPanel");
        extractionPanel.transform.SetParent(root.transform, false);
        var extPanelRect = extractionPanel.AddComponent<RectTransform>();
        extPanelRect.anchorMin = new Vector2(0.5f, 1);
        extPanelRect.anchorMax = new Vector2(0.5f, 1);
        extPanelRect.pivot = new Vector2(0.5f, 1);
        extPanelRect.anchoredPosition = new Vector2(0, -60);
        extPanelRect.sizeDelta = new Vector2(350, 55);

        var extCanvasGroup = extractionPanel.AddComponent<CanvasGroup>();
        extCanvasGroup.alpha = 0f;
        extCanvasGroup.blocksRaycasts = false;

        // Extraction background
        var extBg = extractionPanel.AddComponent<Image>();
        extBg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        extBg.raycastTarget = false;

        // Extraction text
        var extractionText = CreateText(extractionPanel.transform, "ExtractionText", font,
            new Vector2(0, 0.5f), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            "Extracting...", 20, TextAlignmentOptions.Center,
            new Color(1f, 0.9f, 0.4f, 1f));
        var extTextRect = extractionText.GetComponent<RectTransform>();
        extTextRect.anchorMin = new Vector2(0, 0.55f);
        extTextRect.anchorMax = new Vector2(1, 1);
        extTextRect.offsetMin = new Vector2(10, 0);
        extTextRect.offsetMax = new Vector2(-10, -4);
        AddOutline(extractionText);

        // Extraction progress bar
        var extBar = CreateBar(extractionPanel.transform, "ExtractionBar",
            new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(15, 8),
            new Vector2(335, 22),
            new Color(0.3f, 0.8f, 1f, 1f),        // cyan fill
            new Color(0.15f, 0.15f, 0.15f, 0.8f));

        // ---- Inventory Panel (center, toggled with Tab) ----
        var inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(root.transform, false);
        var invPanelRect = inventoryPanel.AddComponent<RectTransform>();
        invPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        invPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        invPanelRect.sizeDelta = new Vector2(500, 500);
        invPanelRect.anchoredPosition = Vector2.zero;

        var invCanvasGroup = inventoryPanel.AddComponent<CanvasGroup>();
        invCanvasGroup.alpha = 0f;
        invCanvasGroup.blocksRaycasts = false;
        invCanvasGroup.interactable = false;

        // Dark background panel
        var invBg = inventoryPanel.AddComponent<Image>();
        invBg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        // Title
        var invTitle = CreateText(inventoryPanel.transform, "Title", font,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -25), new Vector2(0, 40),
            "Inventory (0)", 26, TextAlignmentOptions.Center,
            new Color(0.9f, 0.85f, 0.7f, 1f));
        var invTitleRect = invTitle.GetComponent<RectTransform>();
        invTitleRect.anchorMin = new Vector2(0, 1);
        invTitleRect.anchorMax = new Vector2(1, 1);
        invTitleRect.offsetMin = new Vector2(10, -50);
        invTitleRect.offsetMax = new Vector2(-10, -10);

        // Slot container with GridLayoutGroup
        var slotContainer = new GameObject("SlotContainer");
        slotContainer.transform.SetParent(inventoryPanel.transform, false);
        var slotContainerRect = slotContainer.AddComponent<RectTransform>();
        slotContainerRect.anchorMin = new Vector2(0, 0);
        slotContainerRect.anchorMax = new Vector2(1, 1);
        slotContainerRect.offsetMin = new Vector2(15, 15);
        slotContainerRect.offsetMax = new Vector2(-15, -60);

        var grid = slotContainer.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(70, 70);
        grid.spacing = new Vector2(8, 8);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        var contentFitter = slotContainer.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Attach InventoryUI component and wire fields
        var invUI = inventoryPanel.AddComponent<InventoryUI>();
        var invSo = new SerializedObject(invUI);
        invSo.FindProperty("_slotContainer").objectReferenceValue = slotContainerRect;
        invSo.FindProperty("_slotPrefab").objectReferenceValue = inventorySlotPrefab;
        invSo.FindProperty("_titleText").objectReferenceValue = invTitle.GetComponent<TMP_Text>();
        invSo.FindProperty("_canvasGroup").objectReferenceValue = invCanvasGroup;
        invSo.ApplyModifiedPropertiesWithoutUndo();

        // ---- Attach GameHUD component and wire fields ----
        var hud = root.AddComponent<GameHUD>();

        // Use SerializedObject to set private serialized fields.
        var so = new SerializedObject(hud);
        so.FindProperty("_healthFill").objectReferenceValue = healthBar.fill;
        so.FindProperty("_staminaFill").objectReferenceValue = staminaBar.fill;
        so.FindProperty("_interactPrompt").objectReferenceValue = interactPrompt.GetComponent<TMP_Text>();
        so.FindProperty("_killCountText").objectReferenceValue = killCounter.GetComponent<TMP_Text>();
        so.FindProperty("_runTimerText").objectReferenceValue = runTimer.GetComponent<TMP_Text>();
        so.FindProperty("_lootNotification").objectReferenceValue = lootNotification.GetComponent<TMP_Text>();
        so.FindProperty("_extractionGroup").objectReferenceValue = extCanvasGroup;
        so.FindProperty("_extractionFill").objectReferenceValue = extBar.fill;
        so.FindProperty("_extractionText").objectReferenceValue = extractionText.GetComponent<TMP_Text>();
        so.ApplyModifiedPropertiesWithoutUndo();

        // Save prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Debug.Log($"[HUDPrefabGenerator] Created {path}");
        return prefab;
    }

    // =====================================================================
    // EnemyHealthBar Prefab
    // =====================================================================

    private static GameObject CreateEnemyHealthBarPrefab(TMP_FontAsset font)
    {
        string path = $"{PrefabFolder}/EnemyHealthBar.prefab";

        // Root Canvas (World Space)
        var root = new GameObject("EnemyHealthBar");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        var canvasRect = root.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100, 12);
        root.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); // 1m wide in world

        var canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(root.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        bgImg.raycastTarget = false;

        // Fill
        var fill = new GameObject("Fill");
        fill.transform.SetParent(root.transform, false);
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.8f, 0.15f, 0.15f, 1f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = 0;
        fillImg.fillAmount = 1f;
        fillImg.raycastTarget = false;

        // Attach component and wire
        var bar = root.AddComponent<EnemyHealthBar>();
        var so = new SerializedObject(bar);
        so.FindProperty("_fill").objectReferenceValue = fillImg;
        so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Debug.Log($"[HUDPrefabGenerator] Created {path}");
        return prefab;
    }

    // =====================================================================
    // InventorySlot Prefab
    // =====================================================================

    private static GameObject CreateInventorySlotPrefab(TMP_FontAsset font)
    {
        string path = $"{PrefabFolder}/InventorySlot.prefab";

        // Root: slot container
        var root = new GameObject("InventorySlot");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(70, 70);

        // Border (rarity-coloured frame)
        var border = root.AddComponent<Image>();
        border.color = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        border.raycastTarget = false;

        // Background (inner dark area)
        var bg = new GameObject("Background");
        bg.transform.SetParent(root.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(2, 2);
        bgRect.offsetMax = new Vector2(-2, -2);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.16f, 0.9f);
        bgImg.raycastTarget = false;

        // Icon
        var icon = new GameObject("Icon");
        icon.transform.SetParent(root.transform, false);
        var iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.15f);
        iconRect.anchorMax = new Vector2(0.9f, 0.95f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        var iconImg = icon.AddComponent<Image>();
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        iconImg.enabled = false; // starts empty

        // Count text (bottom-right corner)
        var count = CreateText(root.transform, "Count", font,
            new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-5, 5), new Vector2(40, 22),
            "", 16, TextAlignmentOptions.BottomRight,
            Color.white);
        var countRect = count.GetComponent<RectTransform>();
        countRect.pivot = new Vector2(1, 0);
        count.GetComponent<TMP_Text>().enabled = false;

        // Attach InventorySlotUI and wire
        var slotUI = root.AddComponent<InventorySlotUI>();
        var so = new SerializedObject(slotUI);
        so.FindProperty("_icon").objectReferenceValue = iconImg;
        so.FindProperty("_countText").objectReferenceValue = count.GetComponent<TMP_Text>();
        so.FindProperty("_border").objectReferenceValue = border;
        so.FindProperty("_background").objectReferenceValue = bgImg;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Debug.Log($"[HUDPrefabGenerator] Created {path}");
        return prefab;
    }

    // =====================================================================
    // Wiring
    // =====================================================================

    private static void WireRunManager(GameObject hudPrefab)
    {
        // Try to find RunManager in the active scene.
        var rm = Object.FindObjectOfType<RunManager>();
        if (rm != null)
        {
            var so = new SerializedObject(rm);
            so.FindProperty("_gameHudPrefab").objectReferenceValue = hudPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rm);
            Debug.Log("[HUDPrefabGenerator] Wired GameHUD prefab to RunManager in scene.");
        }
        else
        {
            Debug.LogWarning("[HUDPrefabGenerator] RunManager not found in scene. Open the Game scene and re-run, or assign _gameHudPrefab manually.");
        }
    }

    private static void WireEnemyPrefab(GameObject healthBarPrefab)
    {
        var gruntPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GruntPrefabPath);
        if (gruntPrefab == null)
        {
            Debug.LogWarning($"[HUDPrefabGenerator] Grunt prefab not found at {GruntPrefabPath}. Assign _healthBarPrefab manually.");
            return;
        }

        var controller = gruntPrefab.GetComponent<EnemyController>();
        if (controller == null)
        {
            Debug.LogWarning("[HUDPrefabGenerator] Grunt prefab has no EnemyController.");
            return;
        }

        var so = new SerializedObject(controller);
        so.FindProperty("_healthBarPrefab").objectReferenceValue = healthBarPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gruntPrefab);

        Debug.Log("[HUDPrefabGenerator] Wired EnemyHealthBar prefab to Grunt prefab.");
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private struct BarResult
    {
        public Image fill;
        public Image background;
    }

    private static BarResult CreateBar(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 positionMin, Vector2 positionMax,
        Color fillColor, Color bgColor)
    {
        // Container
        var container = new GameObject(name);
        container.transform.SetParent(parent, false);
        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = anchorMin;
        containerRect.anchorMax = anchorMax;
        containerRect.pivot = new Vector2(0, 0);
        containerRect.offsetMin = positionMin;
        containerRect.offsetMax = positionMax;

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(container.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;
        bgImg.raycastTarget = false;

        // Fill — uses anchor-based width control (anchorMax.x = ratio)
        // instead of Image.Type.Filled, which doesn't persist through prefab generation.
        var fill = new GameObject("Fill");
        fill.transform.SetParent(container.transform, false);
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;
        fillImg.fillAmount = 1f;
        fillImg.raycastTarget = false;

        return new BarResult { fill = fillImg, background = bgImg };
    }

    private static GameObject CreateText(Transform parent, string name, TMP_FontAsset font,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta,
        string defaultText, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
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

    private static void AddOutline(GameObject textGo)
    {
        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontMaterial.EnableKeyword("OUTLINE_ON");
            tmp.outlineWidth = 0.15f;
            tmp.outlineColor = new Color32(0, 0, 0, 180);
        }
    }

    private static Sprite CreateCrosshairSprite()
    {
        string spritePath = "Assets/Textures/Crosshair.png";
        EnsureFolder("Assets/Textures");

        // Check if it already exists.
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (existing != null) return existing;

        // Create a small white dot texture.
        int size = 8;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        float center = (size - 1) / 2f;
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = dist <= radius ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(spritePath, png);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);

        // Configure import settings as sprite.
        var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 8;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string[] parts = folder.Split('/');
        string current = parts[0]; // "Assets"
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
