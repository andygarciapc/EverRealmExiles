using UnityEngine;

namespace EverRealm.Exiles.Core
{
    /// <summary>
    /// Manages the lifecycle of a single extraction run.
    /// Stub — will be expanded in Phase 7 (Extraction System).
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        private float _runStartTime;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            BeginRun();
        }

        public void BeginRun()
        {
            _runStartTime = Time.time;
            GameBootstrap.Instance?.SetState(GameState.InRun);
            Debug.Log("[RunManager] Run started.");
        }

        /// <param name="success">True if player extracted, false if they died.</param>
        public void EndRun(bool success)
        {
            float elapsed = Time.time - _runStartTime;
            GameBootstrap.Instance?.SetState(GameState.RunEnd);
            Debug.Log($"[RunManager] Run ended — success: {success}, time: {elapsed:F1}s");

            // TODO Phase 7: build RunResult, transition to RunSummary scene.
        }
    }
}
