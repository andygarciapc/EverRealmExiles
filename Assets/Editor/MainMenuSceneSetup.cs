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

/// <summary>
/// Editor utility that rebuilds the MainMenu scene with the correct hierarchy:
/// Bootstrap (GameBootstrap + StashManager), MainMenuManager (MainMenuController),
/// Camera, Light, and EventSystem.
/// Also provides a menu item to fix Game.unity by removing GameBootstrap.
/// Run via Tools > EverRealm > Setup MainMenu Scene.
/// </summary>
public static class MainMenuSceneSetup
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string HideoutPrefabPath = "Assets/Prefabs/UI/HideoutUI.prefab";
    private const string ItemRegistryPath = "Assets/ScriptableObjects/ItemRegistry.asset";
    private const string WeaponRegistryPath = "Assets/ScriptableObjects/WeaponRegistry.asset";
    private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

    // =====================================================================
    // Setup MainMenu Scene
    // =====================================================================

    [MenuItem("Tools/EverRealm/Setup MainMenu Scene")]
    public static void SetupMainMenu()
    {
        // Confirm with the user before wiping the scene.
        if (!EditorUtility.DisplayDialog(
                "Setup MainMenu Scene",
                "This will DELETE all existing objects in MainMenu.unity and rebuild the scene with:\n\n" +
                "- Bootstrap (GameBootstrap + StashManager)\n" +
                "- MainMenuManager (MainMenuController)\n" +
                "- Main Camera\n" +
                "- Directional Light\n" +
                "- EventSystem\n\n" +
                "Continue?",
                "Setup", "Cancel"))
        {
            return;
        }

        // 0. Regenerate HideoutUI prefab to ensure fields match current HideoutUI.cs.
        RepairHideoutPrefab();

        // 1. Open MainMenu scene.
        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

        // 2. Delete all existing root objects.
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
            Object.DestroyImmediate(root);

        // 3. Load required assets.
        var itemRegistry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(ItemRegistryPath);
        var weaponRegistry = AssetDatabase.LoadAssetAtPath<WeaponRegistry>(WeaponRegistryPath);
        var hideoutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HideoutPrefabPath);

        if (itemRegistry == null)
            Debug.LogError("[MainMenuSetup] ItemRegistry.asset not found at " + ItemRegistryPath);
        if (weaponRegistry == null)
            Debug.LogError("[MainMenuSetup] WeaponRegistry.asset not found at " + WeaponRegistryPath);
        if (hideoutPrefab == null)
            Debug.LogError("[MainMenuSetup] HideoutUI.prefab not found at " + HideoutPrefabPath);

        // 4. Create Bootstrap GameObject (GameBootstrap + StashManager).
        var bootstrapGo = new GameObject("Bootstrap");
        bootstrapGo.AddComponent<GameBootstrap>();
        var stash = bootstrapGo.AddComponent<StashManager>();

        if (itemRegistry != null && weaponRegistry != null)
        {
            var so = new SerializedObject(stash);
            so.FindProperty("_itemRegistry").objectReferenceValue = itemRegistry;
            so.FindProperty("_weaponRegistry").objectReferenceValue = weaponRegistry;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 5. Create MainMenuManager GameObject (MainMenuController).
        var menuManagerGo = new GameObject("MainMenuManager");
        var menuController = menuManagerGo.AddComponent<MainMenuController>();

        if (hideoutPrefab != null)
        {
            var so = new SerializedObject(menuController);
            so.FindProperty("_hubUiPrefab").objectReferenceValue = hideoutPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 6. Create Main Camera.
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 1f);
        cameraGo.AddComponent<AudioListener>();

        // 7. Create Directional Light.
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.5f;
        light.color = new Color(1f, 0.95f, 0.9f, 1f);
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 8. Create EventSystem with the project's input actions.
        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        var uiInputModule = eventSystemGo.AddComponent<InputSystemUIInputModule>();

        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        if (inputActions != null)
        {
            var so = new SerializedObject(uiInputModule);
            so.FindProperty("m_ActionsAsset").objectReferenceValue = inputActions;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[MainMenuSetup] Wired InputActionAsset to InputSystemUIInputModule.");
        }
        else
        {
            Debug.LogWarning("[MainMenuSetup] InputSystem_Actions.inputactions not found — " +
                             "UI input may not work. Assign manually.");
        }

        // 9. Save the scene.
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[MainMenuSetup] MainMenu scene rebuilt successfully.");
        Debug.Log("[MainMenuSetup] Hierarchy: Bootstrap (GameBootstrap + StashManager), " +
                  "MainMenuManager (MainMenuController → HideoutUI.prefab), " +
                  "Main Camera, Directional Light, EventSystem");
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

        // 1. Open Game scene.
        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        // 2. Find and remove GameBootstrap + StashManager from all objects.
        var roots = scene.GetRootGameObjects();
        bool removedAny = false;

        foreach (var root in roots)
        {
            var bootstraps = root.GetComponentsInChildren<GameBootstrap>(true);
            foreach (var bootstrap in bootstraps)
            {
                Debug.Log($"[MainMenuSetup] Removing GameBootstrap from '{bootstrap.gameObject.name}'");
                Object.DestroyImmediate(bootstrap);
                EditorUtility.SetDirty(root);
                removedAny = true;
            }

            var stashManagers = root.GetComponentsInChildren<StashManager>(true);
            foreach (var sm in stashManagers)
            {
                Debug.Log($"[MainMenuSetup] Removing StashManager from '{sm.gameObject.name}'");
                Object.DestroyImmediate(sm);
                EditorUtility.SetDirty(root);
                removedAny = true;
            }
        }

        if (!removedAny)
            Debug.Log("[MainMenuSetup] No GameBootstrap or StashManager found in Game scene — already clean.");

        // 3. Save the scene.
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[MainMenuSetup] Game scene fixed. GameManager now has only RunManager.");
    }

    // =====================================================================
    // Repair HideoutUI Prefab
    // =====================================================================

    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string InventorySlotPrefabPath = "Assets/Prefabs/UI/InventorySlot.prefab";
    private const string WeaponButtonPrefabPath = "Assets/Prefabs/UI/WeaponButton.prefab";

    /// <summary>
    /// Regenerates the HideoutUI prefab from scratch so all serialized fields
    /// match the current HideoutUI.cs (Solo/Multiplayer buttons, stash, loadout, stats).
    /// </summary>
    private static void RepairHideoutPrefab()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError("[MainMenuSetup] TMP font not found at " + FontPath);
            return;
        }

        var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InventorySlotPrefabPath);
        var weaponBtnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponButtonPrefabPath);

        // --- Build the prefab hierarchy ---

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
        bg.layer = 5;
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.04f, 0.06f, 1f);
        bgImg.raycastTarget = true;

        // Title.
        CreateText(bg.transform, "Title", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -40), new Vector2(600, 50),
            "EVERREALM: EXILES", 38, TextAlignmentOptions.Center,
            new Color(0.9f, 0.85f, 0.7f, 1f));

        // Stats.
        var stats = CreateText(bg.transform, "Stats", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -85), new Vector2(800, 25),
            "Runs: 0  |  Extractions: 0  |  Kills: 0  |  Time: 0m",
            16, TextAlignmentOptions.Center,
            new Color(0.55f, 0.55f, 0.55f, 0.9f));

        // Stash section.
        var stashTitle = CreateText(bg.transform, "StashTitle", font,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -125), new Vector2(300, 30),
            "STASH (0)", 20, TextAlignmentOptions.Center,
            new Color(0.75f, 0.7f, 0.6f, 1f));

        var stashContainer = new GameObject("StashContainer");
        stashContainer.transform.SetParent(bg.transform, false);
        stashContainer.layer = 5;
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
        slotGrid.layer = 5;
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

        // Loadout section.
        CreateText(bg.transform, "LoadoutTitle", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 250), new Vector2(300, 25),
            "LOADOUT", 18, TextAlignmentOptions.Center,
            new Color(0.75f, 0.7f, 0.6f, 1f));

        var weaponRow = new GameObject("WeaponRow");
        weaponRow.transform.SetParent(bg.transform, false);
        weaponRow.layer = 5;
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

        // Play section.
        CreateText(bg.transform, "PlayTitle", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 150), new Vector2(300, 25),
            "PLAY", 18, TextAlignmentOptions.Center,
            new Color(0.75f, 0.7f, 0.6f, 1f));

        var buttonRow = new GameObject("ButtonRow");
        buttonRow.transform.SetParent(bg.transform, false);
        buttonRow.layer = 5;
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

        var soloBtn = CreateMenuButton(buttonRow.transform, "SoloButton", font,
            "SOLO", new Color(0.15f, 0.55f, 0.25f, 1f), 250, 70);

        var multiBtn = CreateMenuButton(buttonRow.transform, "MultiplayerButton", font,
            "MULTIPLAYER", new Color(0.25f, 0.25f, 0.3f, 1f), 250, 70);

        CreateText(multiBtn.transform, "ComingSoon", font,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 8), new Vector2(200, 18),
            "COMING SOON", 11, TextAlignmentOptions.Center,
            new Color(0.6f, 0.6f, 0.6f, 0.8f));

        // --- Wire HideoutUI component fields ---
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

        // --- Save prefab ---
        PrefabUtility.SaveAsPrefabAsset(root, HideoutPrefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[MainMenuSetup] HideoutUI prefab regenerated with correct field wiring.");
    }

    // =====================================================================
    // UI Helpers
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

        btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);

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
}
#endif
