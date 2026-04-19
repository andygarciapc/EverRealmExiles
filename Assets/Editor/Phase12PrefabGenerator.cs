#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using EverRealm.Exiles.UI;

/// <summary>
/// Phase 12 editor utility — regenerates UI prefabs for the inventory/stash
/// rewrite. Creates tooltip panels, updates InventorySlot with pointer events,
/// and wires all new serialized references.
/// Run via Tools > EverRealm > Generate Phase 12 UI.
/// </summary>
public static class Phase12PrefabGenerator
{
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/EverRealm/Generate Phase 12 UI")]
    public static void Generate()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError("[Phase12Gen] Could not load TMP font at " + FontPath);
            return;
        }

        EnsureFolder(PrefabFolder);

        // Step 1: Regenerate InventorySlot.prefab (pointer events, new fields).
        var slotPrefab = RegenerateInventorySlot(font);

        // Step 2: Add tooltip + wire new fields on GameHUD.prefab.
        UpdateGameHUD(font, slotPrefab);

        // Step 3: Add tooltip + wire new fields on HideoutUI.prefab.
        UpdateHideoutUI(font, slotPrefab);

        // Step 4: Add total value text to RunSummaryUI.prefab.
        UpdateRunSummaryUI(font);

        // Step 5: Add rarity bar to RunSummaryItemRow.prefab.
        UpdateRunSummaryItemRow();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Phase12Gen] Phase 12 UI generation complete.");
    }

    // =====================================================================
    // InventorySlot Prefab
    // =====================================================================

    private static GameObject RegenerateInventorySlot(TMP_FontAsset font)
    {
        string path = $"{PrefabFolder}/InventorySlot.prefab";

        // Root: slot container.
        var root = new GameObject("InventorySlot");
        root.layer = 5;
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(70, 70);

        // Border — raycastTarget=true enables pointer events for tooltip/hover.
        var border = root.AddComponent<Image>();
        border.color = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        border.raycastTarget = true;

        // Background (inner dark area).
        var bg = new GameObject("Background");
        bg.transform.SetParent(root.transform, false);
        bg.layer = 5;
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(2, 2);
        bgRect.offsetMax = new Vector2(-2, -2);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.16f, 0.9f);
        bgImg.raycastTarget = false;

        // Icon.
        var icon = new GameObject("Icon");
        icon.transform.SetParent(root.transform, false);
        icon.layer = 5;
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

        // Count text (bottom-right corner).
        var countGo = CreateText(root.transform, "Count", font,
            new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-5, 5), new Vector2(40, 22),
            "", 16, TextAlignmentOptions.BottomRight, Color.white);
        var countRect = countGo.GetComponent<RectTransform>();
        countRect.pivot = new Vector2(1, 0);
        countGo.GetComponent<TMP_Text>().enabled = false;

        // Attach InventorySlotUI and wire.
        var slotUI = root.AddComponent<InventorySlotUI>();
        var so = new SerializedObject(slotUI);
        so.FindProperty("_icon").objectReferenceValue = iconImg;
        so.FindProperty("_countText").objectReferenceValue = countGo.GetComponent<TMP_Text>();
        so.FindProperty("_border").objectReferenceValue = border;
        so.FindProperty("_background").objectReferenceValue = bgImg;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Debug.Log($"[Phase12Gen] Regenerated {path} (pointer events enabled).");
        return prefab;
    }

    // =====================================================================
    // GameHUD — Add Tooltip + Wire InventoryUI
    // =====================================================================

    private static void UpdateGameHUD(TMP_FontAsset font, GameObject slotPrefab)
    {
        string path = $"{PrefabFolder}/GameHUD.prefab";
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefabAsset == null)
        {
            Debug.LogWarning("[Phase12Gen] GameHUD.prefab not found — skipping.");
            return;
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(path);

        // Find InventoryUI component (may be on root "InventoryPanel" or nested).
        InventoryUI invUI = null;
        var invPanel = prefabRoot.transform.Find("InventoryPanel");
        if (invPanel != null)
            invUI = invPanel.GetComponent<InventoryUI>();

        if (invUI == null)
            invUI = prefabRoot.GetComponentInChildren<InventoryUI>();

        if (invUI == null)
        {
            Debug.LogWarning("[Phase12Gen] InventoryUI not found on GameHUD — tooltip not wired.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        // If the new HUDPrefabGenerator has already wired a tooltip, don't clobber it.
        var invSoCheck = new SerializedObject(invUI);
        if (invSoCheck.FindProperty("_tooltip").objectReferenceValue != null)
        {
            Debug.Log("[Phase12Gen] GameHUD InventoryUI already has tooltip wired — skipping.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        // Legacy path: older HUDs were built without a tooltip — add one.
        var existingTooltip = prefabRoot.transform.Find("ItemTooltip");
        if (existingTooltip != null)
            Object.DestroyImmediate(existingTooltip.gameObject);

        var tooltip = CreateTooltipPanel(prefabRoot.transform, font);

        var so = new SerializedObject(invUI);
        so.FindProperty("_tooltip").objectReferenceValue = tooltip;
        so.FindProperty("_displaySlotCount").intValue = 20;
        if (slotPrefab != null)
            so.FindProperty("_slotPrefab").objectReferenceValue = slotPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log("[Phase12Gen] Added tooltip to legacy GameHUD.prefab.");
    }

    // =====================================================================
    // HideoutUI — Add Tooltip + Wire References
    // =====================================================================

    private static void UpdateHideoutUI(TMP_FontAsset font, GameObject slotPrefab)
    {
        string path = $"{PrefabFolder}/HideoutUI.prefab";
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefabAsset == null)
        {
            Debug.LogWarning("[Phase12Gen] HideoutUI.prefab not found — skipping.");
            return;
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(path);

        // Remove existing tooltip (idempotency).
        var existingTooltip = prefabRoot.transform.Find("ItemTooltip");
        if (existingTooltip != null)
            Object.DestroyImmediate(existingTooltip.gameObject);

        // Also check inside Background child (previous generators place content there).
        var bgTransform = prefabRoot.transform.Find("Background");
        if (bgTransform != null)
        {
            var existingInBg = bgTransform.Find("ItemTooltip");
            if (existingInBg != null)
                Object.DestroyImmediate(existingInBg.gameObject);
        }

        // Create tooltip at the root level so it renders on top.
        var tooltip = CreateTooltipPanel(prefabRoot.transform, font);

        // Wire HideoutUI component.
        var hideout = prefabRoot.GetComponent<HideoutUI>();
        if (hideout != null)
        {
            var so = new SerializedObject(hideout);
            so.FindProperty("_tooltip").objectReferenceValue = tooltip;

            // Update slot prefab reference.
            if (slotPrefab != null)
                so.FindProperty("_stashSlotPrefab").objectReferenceValue = slotPrefab;

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Phase12Gen] Wired HideoutUI tooltip + updated slot prefab.");
        }
        else
        {
            Debug.LogWarning("[Phase12Gen] HideoutUI component not found on prefab.");
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log("[Phase12Gen] Updated HideoutUI.prefab with tooltip panel.");
    }

    // =====================================================================
    // RunSummaryUI — Add Total Value Text
    // =====================================================================

    private static void UpdateRunSummaryUI(TMP_FontAsset font)
    {
        string path = $"{PrefabFolder}/RunSummaryUI.prefab";
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefabAsset == null)
        {
            Debug.LogWarning("[Phase12Gen] RunSummaryUI.prefab not found — skipping.");
            return;
        }

        var summaryUI = prefabAsset.GetComponent<RunSummaryUI>();
        if (summaryUI == null)
        {
            Debug.LogWarning("[Phase12Gen] RunSummaryUI component not found.");
            return;
        }

        // Check if totalValueText is already wired.
        var checkSo = new SerializedObject(summaryUI);
        var existingProp = checkSo.FindProperty("_totalValueText");
        if (existingProp != null && existingProp.objectReferenceValue != null)
        {
            Debug.Log("[Phase12Gen] RunSummaryUI already has total value text — skipping.");
            return;
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(path);

        // Find the background/container to parent the new text.
        var bgTransform = prefabRoot.transform.Find("Background");
        if (bgTransform == null)
            bgTransform = prefabRoot.transform;

        // Create total value text near the stats area.
        var valueGo = CreateText(bgTransform, "TotalValueText", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 115), new Vector2(300, 30),
            "Total Value: 0g", 20, TextAlignmentOptions.Center,
            new Color(0.85f, 0.75f, 0.3f, 1f));

        // Wire to the RunSummaryUI component.
        var prefabSummaryUI = prefabRoot.GetComponent<RunSummaryUI>();
        if (prefabSummaryUI != null)
        {
            var so = new SerializedObject(prefabSummaryUI);
            so.FindProperty("_totalValueText").objectReferenceValue = valueGo.GetComponent<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log("[Phase12Gen] Added total value text to RunSummaryUI.prefab.");
    }

    // =====================================================================
    // RunSummaryItemRow — Add Rarity Bar
    // =====================================================================

    private static void UpdateRunSummaryItemRow()
    {
        string path = $"{PrefabFolder}/RunSummaryItemRow.prefab";
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefabAsset == null)
        {
            Debug.LogWarning("[Phase12Gen] RunSummaryItemRow.prefab not found — skipping.");
            return;
        }

        var rowUI = prefabAsset.GetComponent<RunSummaryItemRow>();
        if (rowUI == null)
        {
            Debug.LogWarning("[Phase12Gen] RunSummaryItemRow component not found.");
            return;
        }

        // Check if rarity bar already wired.
        var checkSo = new SerializedObject(rowUI);
        var existingProp = checkSo.FindProperty("_rarityBar");
        if (existingProp != null && existingProp.objectReferenceValue != null)
        {
            Debug.Log("[Phase12Gen] RunSummaryItemRow already has rarity bar — skipping.");
            return;
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(path);

        // Add a thin vertical rarity bar on the left side.
        var barGo = new GameObject("RarityBar");
        barGo.transform.SetParent(prefabRoot.transform, false);
        barGo.layer = 5;
        barGo.transform.SetAsFirstSibling();

        var barRect = barGo.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 0);
        barRect.anchorMax = new Vector2(0, 1);
        barRect.pivot = new Vector2(0, 0.5f);
        barRect.offsetMin = new Vector2(0, 2);
        barRect.offsetMax = new Vector2(4, -2);

        var barImg = barGo.AddComponent<Image>();
        barImg.color = new Color(0.6f, 0.6f, 0.6f, 0.8f); // default common
        barImg.raycastTarget = false;

        // Wire to the RunSummaryItemRow component.
        var prefabRowUI = prefabRoot.GetComponent<RunSummaryItemRow>();
        if (prefabRowUI != null)
        {
            var so = new SerializedObject(prefabRowUI);
            so.FindProperty("_rarityBar").objectReferenceValue = barImg;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log("[Phase12Gen] Added rarity bar to RunSummaryItemRow.prefab.");
    }

    // =====================================================================
    // Tooltip Panel Factory
    // =====================================================================

    private static ItemTooltipUI CreateTooltipPanel(Transform parent, TMP_FontAsset font)
    {
        var root = new GameObject("ItemTooltip");
        root.transform.SetParent(parent, false);
        root.layer = 5;

        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0f, 1f); // top-left pivot for positioning
        rootRect.sizeDelta = new Vector2(260, 180);
        rootRect.anchoredPosition = Vector2.zero;

        var canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // Dark background panel.
        var bgImg = root.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.06f, 0.1f, 0.95f);
        bgImg.raycastTarget = false;

        // --- Name text (top) ---
        var nameGo = CreateText(root.transform, "NameText", font,
            new Vector2(0, 1), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            "Item Name", 20, TextAlignmentOptions.Left,
            Color.white);
        var nameRect = nameGo.GetComponent<RectTransform>();
        nameRect.offsetMin = new Vector2(12, -36);
        nameRect.offsetMax = new Vector2(-12, -8);

        // --- Separator line ---
        var sep = new GameObject("Separator");
        sep.transform.SetParent(root.transform, false);
        sep.layer = 5;
        var sepRect = sep.AddComponent<RectTransform>();
        sepRect.anchorMin = new Vector2(0, 1);
        sepRect.anchorMax = new Vector2(1, 1);
        sepRect.offsetMin = new Vector2(10, -42);
        sepRect.offsetMax = new Vector2(-10, -40);
        var sepImg = sep.AddComponent<Image>();
        sepImg.color = new Color(0.3f, 0.3f, 0.4f, 0.6f);
        sepImg.raycastTarget = false;

        // --- Rarity label (left) ---
        var rarityGo = CreateText(root.transform, "RarityText", font,
            new Vector2(0, 1), new Vector2(0.5f, 1),
            Vector2.zero, Vector2.zero,
            "Rare", 14, TextAlignmentOptions.Left,
            new Color(0.7f, 0.7f, 0.7f, 0.9f));
        var rarityRect = rarityGo.GetComponent<RectTransform>();
        rarityRect.offsetMin = new Vector2(12, -62);
        rarityRect.offsetMax = new Vector2(-5, -46);

        // --- Type label (right) ---
        var typeGo = CreateText(root.transform, "TypeText", font,
            new Vector2(0.5f, 1), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            "Material", 14, TextAlignmentOptions.Right,
            new Color(0.7f, 0.7f, 0.7f, 0.9f));
        var typeRect = typeGo.GetComponent<RectTransform>();
        typeRect.offsetMin = new Vector2(5, -62);
        typeRect.offsetMax = new Vector2(-12, -46);

        // --- Description (middle area, word-wrapped) ---
        var descGo = CreateText(root.transform, "DescriptionText", font,
            new Vector2(0, 1), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            "", 13, TextAlignmentOptions.TopLeft,
            new Color(0.6f, 0.6f, 0.6f, 0.9f));
        var descRect = descGo.GetComponent<RectTransform>();
        descRect.offsetMin = new Vector2(12, -120);
        descRect.offsetMax = new Vector2(-12, -66);
        var descTMP = descGo.GetComponent<TextMeshProUGUI>();
        descTMP.enableWordWrapping = true;
        descTMP.overflowMode = TextOverflowModes.Ellipsis;

        // --- Value (bottom-left) ---
        var valueGo = CreateText(root.transform, "ValueText", font,
            new Vector2(0, 0), new Vector2(0.5f, 0),
            Vector2.zero, Vector2.zero,
            "Value: 10g", 14, TextAlignmentOptions.BottomLeft,
            new Color(0.85f, 0.75f, 0.3f, 1f));
        var valueRect = valueGo.GetComponent<RectTransform>();
        valueRect.offsetMin = new Vector2(12, 8);
        valueRect.offsetMax = new Vector2(-5, 32);

        // --- Weight (bottom-right) ---
        var weightGo = CreateText(root.transform, "WeightText", font,
            new Vector2(0.5f, 0), new Vector2(1, 0),
            Vector2.zero, Vector2.zero,
            "Weight: 1.0", 14, TextAlignmentOptions.BottomRight,
            new Color(0.65f, 0.65f, 0.65f, 0.9f));
        var weightRect = weightGo.GetComponent<RectTransform>();
        weightRect.offsetMin = new Vector2(5, 8);
        weightRect.offsetMax = new Vector2(-12, 32);

        // --- Attach and wire ItemTooltipUI ---
        var tooltip = root.AddComponent<ItemTooltipUI>();
        var so = new SerializedObject(tooltip);
        so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("_panelRect").objectReferenceValue = rootRect;
        so.FindProperty("_nameText").objectReferenceValue = nameGo.GetComponent<TMP_Text>();
        so.FindProperty("_rarityText").objectReferenceValue = rarityGo.GetComponent<TMP_Text>();
        so.FindProperty("_typeText").objectReferenceValue = typeGo.GetComponent<TMP_Text>();
        so.FindProperty("_descriptionText").objectReferenceValue = descGo.GetComponent<TMP_Text>();
        so.FindProperty("_valueText").objectReferenceValue = valueGo.GetComponent<TMP_Text>();
        so.FindProperty("_weightText").objectReferenceValue = weightGo.GetComponent<TMP_Text>();
        so.ApplyModifiedPropertiesWithoutUndo();

        return tooltip;
    }

    // =====================================================================
    // Helpers
    // =====================================================================

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
