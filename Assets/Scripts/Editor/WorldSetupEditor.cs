using UnityEngine;
using UnityEditor;
using EverRealmExiles.World;

namespace EverRealmExiles.Editor
{
    public class WorldSetupEditor : EditorWindow
    {
        private int seed = 0;
        private int renderDistance = 8;

        [MenuItem("EverRealm Exiles/Setup Voxel World")]
        public static void ShowWindow()
        {
            GetWindow<WorldSetupEditor>("Voxel World Setup");
        }

        [MenuItem("EverRealm Exiles/Quick Setup World Scene")]
        public static void QuickSetup()
        {
            SetupWorldScene(0, 8);
        }

        private void OnGUI()
        {
            GUILayout.Label("Voxel World Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            seed = EditorGUILayout.IntField("World Seed (0 = random)", seed);
            renderDistance = EditorGUILayout.IntSlider("Render Distance", renderDistance, 2, 16);

            GUILayout.Space(20);

            if (GUILayout.Button("Setup World Scene", GUILayout.Height(40)))
            {
                SetupWorldScene(seed, renderDistance);
            }

            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "This will set up the current scene with:\n" +
                "- World Initializer component\n" +
                "- Proper lighting for voxel world\n" +
                "- Player with first-person controller\n\n" +
                "Make sure you have the World scene open!",
                MessageType.Info
            );
        }

        private static void SetupWorldScene(int seed, int renderDistance)
        {
            WorldInitializer existingInitializer = Object.FindFirstObjectByType<WorldInitializer>();
            if (existingInitializer != null)
            {
                Debug.Log("WorldInitializer already exists in scene. Updating settings...");
                existingInitializer.worldSeed = seed;
                existingInitializer.renderDistance = renderDistance;
                EditorUtility.SetDirty(existingInitializer);
                return;
            }

            GameObject worldManager = new GameObject("WorldManager");
            WorldInitializer initializer = worldManager.AddComponent<WorldInitializer>();
            initializer.worldSeed = seed;
            initializer.renderDistance = renderDistance;
            initializer.spawnPlayer = true;

            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.transform.rotation = Quaternion.Euler(50, -30, 0);
                    light.intensity = 1.2f;
                    light.color = new Color(1f, 0.95f, 0.85f);
                    light.shadows = LightShadows.Soft;
                    break;
                }
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.gameObject.SetActive(false);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.5f, 0.6f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.25f, 0.2f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.7f, 0.8f, 0.9f);
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 200f;

            EditorUtility.SetDirty(worldManager);

            Debug.Log("Voxel World setup complete! Press Play to see your world.");
        }
    }
}
