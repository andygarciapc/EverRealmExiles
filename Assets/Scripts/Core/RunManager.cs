using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EverRealm.Exiles.Extraction;
using EverRealm.Exiles.Items;
using EverRealm.Exiles.Player;
using EverRealm.Exiles.UI;

namespace EverRealm.Exiles.Core
{
    /// <summary>
    /// Manages the lifecycle of a single extraction run:
    /// start → track kills/time → end (success or death) → show summary.
    /// After the summary, the player returns to the MainMenu scene (hub).
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [SerializeField] private GameObject _runSummaryPrefab;
        [SerializeField] private GameObject _gameHudPrefab;

        private float _runStartTime;
        private int _killCount;
        private GameObject _gameHudInstance;

        public int   KillCount   => _killCount;
        public float ElapsedTime => Time.time - _runStartTime;

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

            if (_gameHudPrefab != null && _gameHudInstance == null)
                _gameHudInstance = Instantiate(_gameHudPrefab);

            // Inject loadout from persistent save.
            var stash = GameBootstrap.Instance?.Stash;
            if (stash != null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    // Apply weapon.
                    var weapon = stash.GetSelectedWeapon();
                    var combat = player.GetComponent<PlayerCombat>();
                    if (combat != null)
                    {
                        combat.SetWeapon(weapon);

                        if (weapon == null)
                            Debug.LogWarning("[RunManager] No weapon equipped — player will be unarmed this run.");

                        // Apply armor defense from equipped gear.
                        combat.SetArmorDefense(stash.Loadout.TotalDefense);
                    }

                    // Seed player inventory with backpack items.
                    var playerInv = player.GetComponent<PlayerInventory>();
                    if (playerInv != null)
                    {
                        foreach (var slot in stash.Loadout.Backpack.Slots)
                        {
                            if (!slot.IsEmpty)
                                playerInv.TryAdd(slot.Definition, slot.Count);
                        }
                    }
                }
            }

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

            // --- Persist to stash ---
            var stash = GameBootstrap.Instance?.Stash;
            if (stash != null)
            {
                stash.RecordRunEnd(result);
                if (success)
                    stash.TransferRunItems(result.Items);

                // Clear backpack — items were seeded into PlayerInventory at run start.
                // On success they're transferred via TransferRunItems; on failure they're lost.
                stash.Loadout.ClearBackpack();
                stash.Save();
            }

            // --- Destroy HUD ---
            if (_gameHudInstance != null)
            {
                Destroy(_gameHudInstance);
                _gameHudInstance = null;
            }

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
