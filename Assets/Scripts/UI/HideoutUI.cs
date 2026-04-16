using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Main menu hub (Arc Raiders-style). Shows the persistent stash via
    /// <see cref="StashPresenter"/>, weapon loadout selection, lifetime stats,
    /// and game-mode buttons. This is the primary menu the player returns to
    /// between runs.
    /// </summary>
    public sealed class HideoutUI : MonoBehaviour
    {
        [Header("Stash")]
        [SerializeField] private Transform _stashSlotContainer;
        [SerializeField] private GameObject _stashSlotPrefab;
        [SerializeField] private TMP_Text _stashTitle;

        [Header("Tooltip")]
        [SerializeField] private ItemTooltipUI _tooltip;

        [Header("Loadout")]
        [SerializeField] private Transform _weaponListContainer;
        [SerializeField] private GameObject _weaponButtonPrefab;

        [Header("Stats")]
        [SerializeField] private TMP_Text _statsText;

        [Header("Play")]
        [SerializeField] private Button _soloButton;
        [SerializeField] private Button _multiplayerButton;

        private StashManager _stash;
        private StashPresenter _presenter;
        private readonly List<WeaponButtonUI> _weaponButtons = new();
        private readonly List<InventorySlotUI> _slotInstances = new();

        // ---------------------------------------------------------------------

        /// <summary>
        /// Populate the hub with stash contents, weapon choices, stats, and wire buttons.
        /// Accepts null stash gracefully.
        /// </summary>
        public void Show(StashManager stash)
        {
            _stash = stash;
            gameObject.SetActive(true);

            if (_stash != null)
            {
                _presenter = new StashPresenter(_stash);
                _presenter.OnRefreshed += OnStashRefreshed;

                PopulateWeapons();
                PopulateStats();
            }

            if (_soloButton != null)
                _soloButton.onClick.AddListener(OnSoloClicked);

            // Multiplayer is a future feature.
            if (_multiplayerButton != null)
                _multiplayerButton.interactable = false;
        }

        private void OnDestroy()
        {
            if (_presenter != null)
            {
                _presenter.OnRefreshed -= OnStashRefreshed;
                _presenter.Dispose();
            }
        }

        // ---------------------------------------------------------------------
        // Stash display

        private void OnStashRefreshed()
        {
            RebuildStashSlots();
            PopulateStats();
        }

        private void RebuildStashSlots()
        {
            if (_stashSlotContainer == null || _stashSlotPrefab == null || _presenter == null)
                return;

            var viewData = _presenter.ViewData;

            if (_stashTitle != null)
                _stashTitle.text = $"Stash ({_presenter.OccupiedSlots})";

            EnsureStashSlotCount(viewData.Count);

            for (int i = 0; i < viewData.Count; i++)
            {
                _slotInstances[i].Populate(viewData[i], i);
                _slotInstances[i].gameObject.SetActive(true);
            }

            for (int i = viewData.Count; i < _slotInstances.Count; i++)
                _slotInstances[i].gameObject.SetActive(false);
        }

        private void EnsureStashSlotCount(int needed)
        {
            while (_slotInstances.Count < needed)
            {
                var go = Instantiate(_stashSlotPrefab, _stashSlotContainer);
                var slot = go.GetComponent<InventorySlotUI>();
                if (slot != null)
                {
                    slot.OnSlotHoverEnter += OnSlotHoverEnter;
                    slot.OnSlotHoverExit += OnSlotHoverExit;
                    _slotInstances.Add(slot);
                }
            }
        }

        private void OnSlotHoverEnter(InventorySlotUI slot)
        {
            _tooltip?.Show(slot.Data, slot.GetComponent<RectTransform>());
        }

        private void OnSlotHoverExit(InventorySlotUI slot)
        {
            _tooltip?.Hide();
        }

        // ---------------------------------------------------------------------
        // Weapon loadout

        private void PopulateWeapons()
        {
            if (_weaponListContainer == null || _weaponButtonPrefab == null) return;

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

                bool isSelected = selectedWeapon != null
                    && selectedWeapon.WeaponId == weapon.WeaponId;
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
            if (_statsText == null || _stash?.Stats == null) return;

            var s = _stash.Stats;
            int minutes = Mathf.FloorToInt(s.TotalPlayTime / 60f);
            int stashValue = _presenter?.TotalValue ?? 0;

            _statsText.text =
                $"Runs: {s.TotalRuns}  |  Extractions: {s.TotalExtractions}  |  " +
                $"Kills: {s.TotalKills}  |  Time: {minutes}m  |  Stash: {stashValue}g";
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
