using UnityEngine;
using UnityEngine.InputSystem;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.Core
{
    /// <summary>
    /// Entry point that persists across scenes and initialises core systems.
    /// Place on a single GameObject in the MainMenu scene — it will DontDestroyOnLoad itself.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        [field: SerializeField]
        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        private StashManager _stash;
        public StashManager Stash => _stash ??= GetComponent<StashManager>();

        /// <summary>
        /// The biome selected for the next run. Set by the Play tab, read by WorldManager.
        /// Runtime-only — the BiomeId is persisted in SaveData via StashManager.
        /// </summary>
        public BiomeDefinition SelectedBiome { get; private set; }

        public void SetSelectedBiome(BiomeDefinition biome)
        {
            SelectedBiome = biome;
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Transitions to a new game state. Called by RunManager or UI.
        /// </summary>
        public void SetState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"[GameBootstrap] State → {newState}");
        }
    }
}
