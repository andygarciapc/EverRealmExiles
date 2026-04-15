using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.Items;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Main menu hub (Arc Raiders–style). Shows the persistent stash, weapon loadout
    /// selection, lifetime stats, and game mode buttons (Solo / Multiplayer).
    /// This is the primary menu the player returns to between runs.
    /// </summary>
    public sealed class HideoutUI : MonoBehaviour
    {
        [Header("Stash")]
        [SerializeField] private Transform _stashSlotContainer;
        [SerializeField] private GameObject _stashSlotPrefab;
        [SerializeField] private TMP_Text _stashTitle;

        [Header("Loadout")]
        [SerializeField] private Transform _weaponListContainer;
        [SerializeField] private GameObject _weaponButtonPrefab;

        [Header("Stats")]
        [SerializeField] private TMP_Text _statsText;

        [Header("Play")]
        [SerializeField] private Button _soloButton;
        [SerializeField] private Button _multiplayerButton;

        private StashManager _stash;
        private readonly List<WeaponButtonUI> _weaponButtons = new();

        // ---------------------------------------------------------------------

        /// <summary>
        /// Populate the hub with stash contents, weapon choices, stats, and wire buttons.
        /// Accepts null stash gracefully — buttons still work, content is empty.
        /// </summary>
        public void Show(StashManager stash)
        {
            _stash = stash;
            gameObject.SetActive(true);

            if (_stash != null)
            {
                PopulateStash();
                PopulateWeapons();
                PopulateStats();
            }

            if (_soloButton != null)
            {
                _soloButton.onClick.AddListener(OnSoloClicked);
            }

            // Multiplayer is a future feature — button is visible but not interactable.
            if (_multiplayerButton != null)
                _multiplayerButton.interactable = false;
        }

        // ---------------------------------------------------------------------
        // Stash display

        private void PopulateStash()
        {
            if (_stashSlotContainer == null || _stashSlotPrefab == null) return;

            // Clear existing slots.
            for (int i = _stashSlotContainer.childCount - 1; i >= 0; i--)
                Destroy(_stashSlotContainer.GetChild(i).gameObject);

            int count = 0;
            foreach (var slot in _stash.Stash.Slots)
            {
                if (slot.IsEmpty) continue;
                var go = Instantiate(_stashSlotPrefab, _stashSlotContainer);
                var slotUI = go.GetComponent<InventorySlotUI>();
                if (slotUI != null)
                    slotUI.Populate(slot);
                count++;
            }

            if (_stashTitle != null)
                _stashTitle.text = $"Stash ({count})";
        }

        // ---------------------------------------------------------------------
        // Weapon loadout

        private void PopulateWeapons()
        {
            if (_weaponListContainer == null || _weaponButtonPrefab == null) return;

            // Clear existing buttons.
            for (int i = _weaponListContainer.childCount - 1; i >= 0; i--)
                Destroy(_weaponListContainer.GetChild(i).gameObject);
            _weaponButtons.Clear();

            var selectedWeapon = _stash.GetSelectedWeapon();
            var allWeapons = _stash.WeaponRegistry.All;

            foreach (var weapon in allWeapons)
            {
                if (weapon == null) continue;

                var go = Instantiate(_weaponButtonPrefab, _weaponListContainer);
                var btn = go.GetComponent<WeaponButtonUI>();
                if (btn == null) continue;

                bool isSelected = selectedWeapon != null && selectedWeapon.WeaponId == weapon.WeaponId;
                btn.Populate(weapon, isSelected);
                _weaponButtons.Add(btn);

                var captured = weapon;
                btn.Button.onClick.AddListener(() => OnWeaponSelected(captured));
            }
        }

        private void OnWeaponSelected(WeaponDefinition weapon)
        {
            _stash.SetSelectedWeapon(weapon.WeaponId);

            foreach (var btn in _weaponButtons)
                btn.SetSelected(btn.Weapon.WeaponId == weapon.WeaponId);
        }

        // ---------------------------------------------------------------------
        // Stats

        private void PopulateStats()
        {
            if (_statsText == null || _stash.Stats == null) return;

            var s = _stash.Stats;
            int minutes = Mathf.FloorToInt(s.TotalPlayTime / 60f);
            _statsText.text =
                $"Runs: {s.TotalRuns}  |  Extractions: {s.TotalExtractions}  |  " +
                $"Kills: {s.TotalKills}  |  Time: {minutes}m";
        }

        // ---------------------------------------------------------------------
        // Game mode buttons

        private void OnSoloClicked()
        {
            Debug.Log("[HideoutUI] Solo clicked — loading Game scene.");
            SceneManager.LoadScene("Game");
        }
    }
}
