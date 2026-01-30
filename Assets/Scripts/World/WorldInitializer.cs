using UnityEngine;
using EverRealmExiles.Voxel;
using EverRealmExiles.Player;

namespace EverRealmExiles.World
{
    public class WorldInitializer : MonoBehaviour
    {
        [Header("World Settings")]
        public int worldSeed = 0;
        public int renderDistance = 8;

        [Header("Player Settings")]
        public bool spawnPlayer = true;

        private VoxelWorld voxelWorld;
        private GameObject player;

        private void Awake()
        {
            SetupWorld();

            if (spawnPlayer)
            {
                SetupPlayer();
            }
        }

        private void SetupWorld()
        {
            GameObject worldObject = new GameObject("VoxelWorld");
            voxelWorld = worldObject.AddComponent<VoxelWorld>();

            voxelWorld.seed = worldSeed == 0 ? Random.Range(1, 999999) : worldSeed;
            voxelWorld.renderDistance = renderDistance;
            voxelWorld.voxelMaterial = TextureAtlasGenerator.CreateVoxelMaterial();
            voxelWorld.generateOnStart = false;

            Debug.Log($"World initialized with seed: {voxelWorld.seed}");
        }

        private void SetupPlayer()
        {
            player = new GameObject("Player");

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);

            VoxelPlayerController playerController = player.AddComponent<VoxelPlayerController>();

            GameObject cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.parent = player.transform;
            cameraObject.transform.localPosition = new Vector3(0, 1.6f, 0);

            Camera cam = cameraObject.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.fieldOfView = 70f;

            cameraObject.AddComponent<AudioListener>();
            playerController.playerCamera = cam;

            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera != cam)
            {
                mainCamera.gameObject.SetActive(false);
            }

            voxelWorld.playerTransform = player.transform;

            player.transform.position = new Vector3(0, 100, 0);
        }

        private void Start()
        {
            voxelWorld.GenerateInitialWorld();

            if (spawnPlayer)
            {
                StartCoroutine(SpawnPlayerWhenReady());
            }
        }

        private System.Collections.IEnumerator SpawnPlayerWhenReady()
        {
            yield return new WaitForSeconds(0.5f);

            while (voxelWorld.GetChunk(Vector2Int.zero) == null)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);

            Vector3 spawnPos = voxelWorld.GetSpawnPosition();
            VoxelPlayerController controller = player.GetComponent<VoxelPlayerController>();
            controller.SetPosition(spawnPos);

            Debug.Log($"Player spawned at: {spawnPos}");
        }

        public void RegenerateWorld()
        {
            int newSeed = Random.Range(1, 999999);
            voxelWorld.RegenerateWorld(newSeed);

            if (player != null)
            {
                StartCoroutine(SpawnPlayerWhenReady());
            }
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;

            GUI.Label(new Rect(10, 10, 300, 25), $"Seed: {voxelWorld.seed}", style);
            GUI.Label(new Rect(10, 30, 300, 25), $"Position: {player.transform.position:F1}", style);
            GUI.Label(new Rect(10, 50, 300, 25), "WASD - Move | Mouse - Look | Space - Jump", style);
            GUI.Label(new Rect(10, 70, 300, 25), "Shift - Run | Escape - Toggle Cursor", style);

            if (GUI.Button(new Rect(10, 100, 150, 30), "New World (R)") || Input.GetKeyDown(KeyCode.R))
            {
                RegenerateWorld();
            }
        }
    }
}
