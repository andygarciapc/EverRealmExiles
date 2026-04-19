#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using EverRealm.Exiles.UI;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.AI;
using EverRealm.Exiles.Data;

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

        // ---- Inventory Overlay (full-screen, toggled with Tab) ----
        BuildInventoryOverlay(root.transform, font, inventorySlotPrefab);

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

        // Border (rarity-coloured frame). raycastTarget=true so the slot root
        // (which holds InventorySlotUI) receives pointer events — this border
        // is the only raycastable graphic on the slot.
        var border = root.AddComponent<Image>();
        border.color = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        border.raycastTarget = true;

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

    // =====================================================================
    // Inventory Overlay — split layout (run inventory + equipment slots)
    // Mirrors the main-menu loadout screen so the in-game inventory feels
    // consistent with the pre-run preparation view.
    // =====================================================================

    private static GameObject BuildInventoryOverlay(Transform canvasRoot, TMP_FontAsset font,
        GameObject inventorySlotPrefab)
    {
        // Root overlay — full-screen container.
        var overlay = new GameObject("InventoryPanel");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.layer = 5;
        var overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        var canvasGroup = overlay.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // Dark backdrop — blocks world clicks passing through, dims HUD beneath.
        var backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(overlay.transform, false);
        backdrop.layer = 5;
        var backdropRect = backdrop.AddComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        var backdropImg = backdrop.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.7f);
        backdropImg.raycastTarget = true;

        // Center container with margin from screen edges.
        var center = new GameObject("Center");
        center.transform.SetParent(overlay.transform, false);
        center.layer = 5;
        var centerRect = center.AddComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.05f, 0.03f);
        centerRect.anchorMax = new Vector2(0.95f, 0.97f);
        centerRect.offsetMin = Vector2.zero;
        centerRect.offsetMax = Vector2.zero;

        // ==================== LEFT PANEL — RUN INVENTORY ====================
        var leftPanel = new GameObject("LeftPanel");
        leftPanel.transform.SetParent(center.transform, false);
        leftPanel.layer = 5;
        var leftRect = leftPanel.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0.55f, 1);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;
        var leftBg = leftPanel.AddComponent<Image>();
        leftBg.color = new Color(0.07f, 0.07f, 0.10f, 0.95f);
        leftBg.raycastTarget = false;

        var inventoryTitle = CreateText(leftPanel.transform, "InventoryTitle", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -15), new Vector2(300, 28),
            "INVENTORY (0)", 20, TextAlignmentOptions.Center,
            new Color(0.85f, 0.80f, 0.65f, 1f));

        var inventoryArea = new GameObject("InventoryArea");
        inventoryArea.transform.SetParent(leftPanel.transform, false);
        inventoryArea.layer = 5;
        var inventoryAreaRect = inventoryArea.AddComponent<RectTransform>();
        inventoryAreaRect.anchorMin = new Vector2(0.02f, 0.06f);
        inventoryAreaRect.anchorMax = new Vector2(0.98f, 0.92f);
        inventoryAreaRect.offsetMin = Vector2.zero;
        inventoryAreaRect.offsetMax = Vector2.zero;
        var inventoryAreaBg = inventoryArea.AddComponent<Image>();
        inventoryAreaBg.color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
        inventoryAreaBg.raycastTarget = false;

        var inventoryGrid = new GameObject("SlotGrid");
        inventoryGrid.transform.SetParent(inventoryArea.transform, false);
        inventoryGrid.layer = 5;
        var inventoryGridRect = inventoryGrid.AddComponent<RectTransform>();
        inventoryGridRect.anchorMin = Vector2.zero;
        inventoryGridRect.anchorMax = Vector2.one;
        inventoryGridRect.offsetMin = new Vector2(10, 10);
        inventoryGridRect.offsetMax = new Vector2(-10, -10);
        var grid = inventoryGrid.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(70, 70);
        grid.spacing = new Vector2(6, 6);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 7;

        var inventoryInfo = CreateText(leftPanel.transform, "InventoryInfo", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 15), new Vector2(400, 22),
            "Items: 0  |  Value: 0g", 13, TextAlignmentOptions.Center,
            new Color(0.50f, 0.50f, 0.50f, 0.8f));

        // ==================== RIGHT PANEL — LOADOUT ====================
        var rightPanel = new GameObject("RightPanel");
        rightPanel.transform.SetParent(center.transform, false);
        rightPanel.layer = 5;
        var rightRect = rightPanel.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.57f, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.offsetMin = Vector2.zero;
        rightRect.offsetMax = Vector2.zero;
        var rightBg = rightPanel.AddComponent<Image>();
        rightBg.color = new Color(0.07f, 0.07f, 0.10f, 0.95f);
        rightBg.raycastTarget = false;

        CreateText(rightPanel.transform, "LoadoutTitle", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -15), new Vector2(300, 28),
            "LOADOUT", 20, TextAlignmentOptions.Center,
            new Color(0.85f, 0.80f, 0.65f, 1f));

        // Equipment slots fill nearly the entire right panel vertically —
        // no backpack section since the left panel is the run inventory.
        var equipArea = new GameObject("EquipmentArea");
        equipArea.transform.SetParent(rightPanel.transform, false);
        equipArea.layer = 5;
        var equipRect = equipArea.AddComponent<RectTransform>();
        equipRect.anchorMin = new Vector2(0.05f, 0.08f);
        equipRect.anchorMax = new Vector2(0.95f, 0.92f);
        equipRect.offsetMin = Vector2.zero;
        equipRect.offsetMax = Vector2.zero;
        var equipLayout = equipArea.AddComponent<VerticalLayoutGroup>();
        equipLayout.spacing = 10;
        equipLayout.childAlignment = TextAnchor.MiddleCenter;
        equipLayout.childControlWidth = true;
        equipLayout.childControlHeight = true;
        equipLayout.childForceExpandWidth = true;
        equipLayout.childForceExpandHeight = true; // flex to fill vertical space
        equipLayout.padding = new RectOffset(5, 5, 5, 5);

        var headSlot      = BuildEquipmentSlot(equipArea.transform, font, EquipSlot.Head);
        var chestSlot     = BuildEquipmentSlot(equipArea.transform, font, EquipSlot.Chest);
        var legsSlot      = BuildEquipmentSlot(equipArea.transform, font, EquipSlot.Legs);
        var primarySlot   = BuildEquipmentSlot(equipArea.transform, font, EquipSlot.PrimaryWeapon);
        var secondarySlot = BuildEquipmentSlot(equipArea.transform, font, EquipSlot.SecondaryWeapon);

        var loadoutInfo = CreateText(rightPanel.transform, "LoadoutInfo", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 15), new Vector2(400, 22),
            "Defense: 0", 13, TextAlignmentOptions.Center,
            new Color(0.50f, 0.50f, 0.50f, 0.8f));

        // ==================== CLOSE HINT ====================
        CreateText(center.transform, "CloseHint", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, -15), new Vector2(400, 22),
            "Press [Tab] to close", 13, TextAlignmentOptions.Center,
            new Color(0.45f, 0.45f, 0.45f, 0.6f));

        // ==================== TOOLTIP ====================
        var tooltip = BuildTooltipPanel(overlay.transform, font);

        // ==================== WIRE InventoryUI ====================
        var invUI = overlay.AddComponent<InventoryUI>();
        var invSo = new SerializedObject(invUI);

        invSo.FindProperty("_canvasGroup").objectReferenceValue          = canvasGroup;
        invSo.FindProperty("_slotContainer").objectReferenceValue        = inventoryGridRect;
        invSo.FindProperty("_slotPrefab").objectReferenceValue           = inventorySlotPrefab;
        invSo.FindProperty("_titleText").objectReferenceValue            = inventoryTitle.GetComponent<TMP_Text>();
        invSo.FindProperty("_infoText").objectReferenceValue             = inventoryInfo.GetComponent<TMP_Text>();
        invSo.FindProperty("_headSlot").objectReferenceValue             = headSlot;
        invSo.FindProperty("_chestSlot").objectReferenceValue            = chestSlot;
        invSo.FindProperty("_legsSlot").objectReferenceValue             = legsSlot;
        invSo.FindProperty("_primaryWeaponSlot").objectReferenceValue    = primarySlot;
        invSo.FindProperty("_secondaryWeaponSlot").objectReferenceValue  = secondarySlot;
        invSo.FindProperty("_loadoutInfoText").objectReferenceValue      = loadoutInfo.GetComponent<TMP_Text>();
        invSo.FindProperty("_tooltip").objectReferenceValue              = tooltip;
        invSo.ApplyModifiedPropertiesWithoutUndo();

        return overlay;
    }

    // -----------------------------------------------------------------
    // Equipment slot factory — matches MainMenuSceneSetup.BuildEquipmentSlot
    // but uses flexibleHeight=1 so slots stretch to fill the right panel.
    // -----------------------------------------------------------------
    private static EquipmentSlotUI BuildEquipmentSlot(Transform parent, TMP_FontAsset font,
        EquipSlot slotType)
    {
        string slotName = slotType.ToString();

        var go = new GameObject(slotName + "Slot");
        go.transform.SetParent(parent, false);
        go.layer = 5;

        var layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = 70;
        layout.flexibleHeight = 1;
        layout.flexibleWidth = 1;

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
        iconRect.anchoredPosition = new Vector2(12, 0);
        iconRect.sizeDelta = new Vector2(50, 0);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        iconImg.enabled = false;

        string displayName = slotType switch
        {
            EquipSlot.Head            => "HEAD",
            EquipSlot.Chest           => "CHEST",
            EquipSlot.Legs            => "LEGS",
            EquipSlot.PrimaryWeapon   => "PRIMARY WEAPON",
            EquipSlot.SecondaryWeapon => "SECONDARY WEAPON",
            _                          => "SLOT"
        };

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        labelGo.layer = 5;
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(75, 5);
        labelRect.offsetMax = new Vector2(-10, -5);
        var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.font = font;
        labelTmp.text = displayName;
        labelTmp.fontSize = 16;
        labelTmp.alignment = TextAlignmentOptions.Left;
        labelTmp.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        labelTmp.raycastTarget = false;

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

    // -----------------------------------------------------------------
    // Tooltip panel — includes weapon stats (damage/speed) so in-game
    // tooltips match the main-menu version exactly.
    // -----------------------------------------------------------------
    private static ItemTooltipUI BuildTooltipPanel(Transform parent, TMP_FontAsset font)
    {
        var tooltipGo = new GameObject("Tooltip");
        tooltipGo.transform.SetParent(parent, false);
        tooltipGo.layer = 5;

        var tooltipRect = tooltipGo.AddComponent<RectTransform>();
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
        ttNameRect.pivot = new Vector2(0, 1);
        ttNameRect.offsetMin = new Vector2(10, -30);
        ttNameRect.offsetMax = new Vector2(-10, -8);

        var ttRarity = CreateText(tooltipGo.transform, "Rarity", font,
            new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            "Common", 13, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
        var ttRarityRect = ttRarity.GetComponent<RectTransform>();
        ttRarityRect.pivot = new Vector2(0, 1);
        ttRarityRect.offsetMin = new Vector2(10, -50);
        ttRarityRect.offsetMax = new Vector2(-10, -32);

        var ttType = CreateText(tooltipGo.transform, "Type", font,
            new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            "Type", 13, TextAlignmentOptions.Right, new Color(0.5f, 0.5f, 0.5f));
        var ttTypeRect = ttType.GetComponent<RectTransform>();
        ttTypeRect.pivot = new Vector2(0, 1);
        ttTypeRect.offsetMin = new Vector2(10, -50);
        ttTypeRect.offsetMax = new Vector2(-10, -32);

        var ttDesc = CreateText(tooltipGo.transform, "Description", font,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            "", 13, TextAlignmentOptions.TopLeft, new Color(0.7f, 0.7f, 0.7f));
        var ttDescTmp = ttDesc.GetComponent<TextMeshProUGUI>();
        ttDescTmp.enableWordWrapping = true;
        var ttDescRect = ttDesc.GetComponent<RectTransform>();
        ttDescRect.offsetMin = new Vector2(10, 60);
        ttDescRect.offsetMax = new Vector2(-10, -55);

        var ttEquipSlot = CreateText(tooltipGo.transform, "EquipSlot", font,
            new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero,
            "", 13, TextAlignmentOptions.Left, new Color(0.3f, 0.8f, 1f));
        var ttEquipSlotRect = ttEquipSlot.GetComponent<RectTransform>();
        ttEquipSlotRect.pivot = new Vector2(0, 0);
        ttEquipSlotRect.offsetMin = new Vector2(10, 38);
        ttEquipSlotRect.offsetMax = new Vector2(-10, 55);

        var ttDefense = CreateText(tooltipGo.transform, "Defense", font,
            new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero,
            "", 13, TextAlignmentOptions.Right, new Color(0.4f, 0.9f, 0.4f));
        var ttDefenseRect = ttDefense.GetComponent<RectTransform>();
        ttDefenseRect.pivot = new Vector2(1, 0);
        ttDefenseRect.offsetMin = new Vector2(10, 38);
        ttDefenseRect.offsetMax = new Vector2(-10, 55);

        var ttDamage = CreateText(tooltipGo.transform, "Damage", font,
            new Vector2(0, 0), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero,
            "", 13, TextAlignmentOptions.Left, new Color(1f, 0.6f, 0.3f));
        var ttDamageRect = ttDamage.GetComponent<RectTransform>();
        ttDamageRect.pivot = new Vector2(0, 0);
        ttDamageRect.offsetMin = new Vector2(10, 58);
        ttDamageRect.offsetMax = new Vector2(-5, 75);

        var ttSpeed = CreateText(tooltipGo.transform, "Speed", font,
            new Vector2(0.5f, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero,
            "", 13, TextAlignmentOptions.Right, new Color(0.6f, 0.8f, 1f));
        var ttSpeedRect = ttSpeed.GetComponent<RectTransform>();
        ttSpeedRect.pivot = new Vector2(1, 0);
        ttSpeedRect.offsetMin = new Vector2(5, 58);
        ttSpeedRect.offsetMax = new Vector2(-10, 75);

        var ttValue = CreateText(tooltipGo.transform, "Value", font,
            new Vector2(0, 0), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero,
            "Value: 0g", 13, TextAlignmentOptions.Left, new Color(0.8f, 0.75f, 0.4f));
        var ttValueRect = ttValue.GetComponent<RectTransform>();
        ttValueRect.pivot = new Vector2(0, 0);
        ttValueRect.offsetMin = new Vector2(10, 10);
        ttValueRect.offsetMax = new Vector2(-5, 32);

        var ttWeight = CreateText(tooltipGo.transform, "Weight", font,
            new Vector2(0.5f, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero,
            "Weight: 0.0", 13, TextAlignmentOptions.Right, new Color(0.6f, 0.6f, 0.6f));
        var ttWeightRect = ttWeight.GetComponent<RectTransform>();
        ttWeightRect.pivot = new Vector2(1, 0);
        ttWeightRect.offsetMin = new Vector2(5, 10);
        ttWeightRect.offsetMax = new Vector2(-10, 32);

        var tooltip = tooltipGo.AddComponent<ItemTooltipUI>();
        var ttSo = new SerializedObject(tooltip);
        ttSo.FindProperty("_canvasGroup").objectReferenceValue     = tooltipCg;
        ttSo.FindProperty("_panelRect").objectReferenceValue       = tooltipRect;
        ttSo.FindProperty("_nameText").objectReferenceValue        = ttName.GetComponent<TMP_Text>();
        ttSo.FindProperty("_rarityText").objectReferenceValue      = ttRarity.GetComponent<TMP_Text>();
        ttSo.FindProperty("_typeText").objectReferenceValue        = ttType.GetComponent<TMP_Text>();
        ttSo.FindProperty("_descriptionText").objectReferenceValue = ttDesc.GetComponent<TMP_Text>();
        ttSo.FindProperty("_valueText").objectReferenceValue       = ttValue.GetComponent<TMP_Text>();
        ttSo.FindProperty("_weightText").objectReferenceValue      = ttWeight.GetComponent<TMP_Text>();
        ttSo.FindProperty("_defenseText").objectReferenceValue     = ttDefense.GetComponent<TMP_Text>();
        ttSo.FindProperty("_equipSlotText").objectReferenceValue   = ttEquipSlot.GetComponent<TMP_Text>();
        ttSo.FindProperty("_damageText").objectReferenceValue      = ttDamage.GetComponent<TMP_Text>();
        ttSo.FindProperty("_speedText").objectReferenceValue       = ttSpeed.GetComponent<TMP_Text>();
        ttSo.ApplyModifiedPropertiesWithoutUndo();

        return tooltip;
    }
}
#endif
