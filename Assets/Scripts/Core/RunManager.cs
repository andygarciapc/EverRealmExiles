using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EverRealm.Exiles.Extraction;
using EverRealm.Exiles.Items;
using EverRealm.Exiles.UI;

namespace EverRealm.Exiles.Core
{
    /// <summary>
    /// Manages the lifecycle of a single extraction run:
    /// start → track kills/time → end (success or death) → show summary → restart.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [SerializeField] private GameObject _runSummaryPrefab;

        private float _runStartTime;
        private int _killCount;

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
            _killCount = 0;
            GameBootstrap.Instance?.SetState(GameState.InRun);
            Debug.Log("[RunManager] Run started.");
        }

        /// <summary>Call when an enemy dies to increment the run kill counter.</summary>
        public void RegisterKill()
        {
            _killCount++;
        }

        /// <param name="success">True if player extracted, false if they died.</param>
        public void EndRun(bool success)
        {
            // Guard against double-call (e.g., dying inside extraction zone).
            if (GameBootstrap.Instance != null &&
                GameBootstrap.Instance.CurrentState == GameState.RunEnd)
                return;

            float elapsed = Time.time - _runStartTime;

            // --- Snapshot inventory ---
            var player = GameObject.FindWithTag("Player");
            var items = new List<ItemStack>();

            if (player != null)
            {
                // Lock player input.
                var playerInput = player.GetComponent<PlayerInput>();
                if (playerInput != null)
                    playerInput.enabled = false;

                // Copy inventory contents before potentially clearing.
                var inv = player.GetComponent<PlayerInventory>();
                if (inv != null)
                {
                    foreach (var slot in inv.Inventory.Slots)
                        items.Add(slot);

                    // On failure the player loses all carried items.
                    if (!success)
                        inv.Inventory.Clear();
                }
            }

            // --- Build result ---
            var result = new RunResult(success, elapsed, _killCount, items);

            GameBootstrap.Instance?.SetState(GameState.RunEnd);
            Debug.Log($"[RunManager] Run ended — success: {success}, time: {elapsed:F1}s, kills: {_killCount}");

            // --- Show summary UI ---
            if (_runSummaryPrefab != null)
            {
                var go = Instantiate(_runSummaryPrefab);
                var ui = go.GetComponent<RunSummaryUI>();
                if (ui != null)
                    ui.Show(result);
            }

            // Unlock cursor so the player can interact with the summary screen.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

    }
}
