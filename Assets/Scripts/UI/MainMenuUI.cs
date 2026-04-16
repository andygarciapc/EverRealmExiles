using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Top-level controller for the tabbed main menu.
    /// Manages tab switching, biome selection, and launching runs.
    /// Instantiated by <see cref="MainMenuController"/> or placed directly in the scene.
    /// </summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        // ----- Top Bar — Tabs -----
        [Header("Tab Buttons")]
        [SerializeField] private Button _playTabButton;
        [SerializeField] private Button _craftTabButton;
        [SerializeField] private Button _exileTabButton;
        [SerializeField] private Button _shopTabButton;

        // ----- Top Bar — Player Info -----
        [Header("Player Info")]
        [SerializeField] private TMP_Text _currencyText;
        [SerializeField] private TMP_Text _notificationText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _playerNameText;

        // ----- Content Panels -----
        [Header("Content Panels")]
        [SerializeField] private GameObject _playPanel;
        [SerializeField] private GameObject _craftPanel;
        [SerializeField] private GameObject _exilePanel;
        [SerializeField] private GameObject _shopPanel;

        // ----- Play Tab — World Map -----
        [Header("Play Tab — Map")]
        [SerializeField] private RectTransform _mapContainer;
        [SerializeField] private GameObject _mapPointPrefab;

        // ----- Play Tab — Selected Biome Detail -----
        [Header("Play Tab — Selection")]
        [SerializeField] private TMP_Text _selectedBiomeName;
        [SerializeField] private TMP_Text _selectedBiomeDesc;
        [SerializeField] private TMP_Text _selectedBiomeDifficulty;
        [SerializeField] private Image _selectedBiomePreview;
        [SerializeField] private Button _launchButton;

        // ----- Exile Tab — Character info -----
        [Header("Exile Tab")]
        [SerializeField] private ExileTabUI _exileTabUI;

        // ----- Inventory / Loadout Overlay (Tab key) -----
        [Header("Inventory Overlay")]
        [SerializeField] private MainMenuInventoryUI _inventoryOverlay;

        // ----- Runtime state -----
        private StashManager _stash;
        private BiomeDefinition _selectedBiome;
        private readonly List<MapPointUI> _mapPoints = new();

        private Button[] _tabButtons;
        private GameObject[] _panels;

        private static readonly Color TabActive   = new(0.90f, 0.85f, 0.70f, 1f);
        private static readonly Color TabInactive = new(0.50f, 0.50f, 0.50f, 1f);

        // ---------------------------------------------------------------------

        /// <summary>
        /// Populate the entire menu. Called by MainMenuController after instantiation.
        /// </summary>
        public void Show(StashManager stash)
        {
            _stash = stash;
            gameObject.SetActive(true);

            Debug.Log($"[MainMenuUI] Show() called. stash={(stash != null ? "OK" : "NULL")}");
            Debug.Log($"[MainMenuUI] Panels: play={_playPanel != null}, craft={_craftPanel != null}, exile={_exilePanel != null}, shop={_shopPanel != null}");
            Debug.Log($"[MainMenuUI] Tabs: play={_playTabButton != null}, craft={_craftTabButton != null}, exile={_exileTabButton != null}, shop={_shopTabButton != null}");

            _tabButtons = new[] { _playTabButton, _craftTabButton, _exileTabButton, _shopTabButton };
            _panels     = new[] { _playPanel, _craftPanel, _exilePanel, _shopPanel };

            WireTabButtons();
            PopulatePlayerInfo();
            PopulateMap();
            RestoreSelectedBiome();

            // Exile tab — character info.
            if (_exileTabUI != null)
                _exileTabUI.Show(stash);

            // Inventory overlay — initialized but hidden until Tab pressed.
            if (_inventoryOverlay != null)
                _inventoryOverlay.Initialize(stash);

            // Wire launch button.
            if (_launchButton != null)
                _launchButton.onClick.AddListener(OnLaunchClicked);

            // Default to Play tab.
            SwitchTab(0);
            Debug.Log("[MainMenuUI] Show() complete — Play tab active.");
        }

        // ---------------------------------------------------------------------

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[Key.Tab].wasPressedThisFrame)
            {
                if (_inventoryOverlay != null)
                    _inventoryOverlay.Toggle();
            }
        }

        // ---------------------------------------------------------------------
        // Tab system

        private void WireTabButtons()
        {
            if (_playTabButton  != null) _playTabButton.onClick.AddListener(()  => SwitchTab(0));
            if (_craftTabButton != null) _craftTabButton.onClick.AddListener(() => SwitchTab(1));
            if (_exileTabButton != null) _exileTabButton.onClick.AddListener(() => SwitchTab(2));
            if (_shopTabButton  != null) _shopTabButton.onClick.AddListener(()  => SwitchTab(3));
        }

        private void SwitchTab(int index)
        {
            for (int i = 0; i < _panels.Length; i++)
            {
                if (_panels[i] != null)
                    _panels[i].SetActive(i == index);

                if (_tabButtons[i] != null)
                {
                    var colors = _tabButtons[i].colors;
                    colors.normalColor = i == index ? TabActive : TabInactive;
                    _tabButtons[i].colors = colors;

                    var text = _tabButtons[i].GetComponentInChildren<TMP_Text>();
                    if (text != null)
                        text.color = i == index ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
                }
            }
        }

        // ---------------------------------------------------------------------
        // Player info bar

        private void PopulatePlayerInfo()
        {
            if (_stash == null) return;

            if (_currencyText != null)
                _currencyText.text = _stash.Currency.ToString("N0");

            if (_notificationText != null)
                _notificationText.text = "0"; // Placeholder — no notification system yet.

            if (_levelText != null)
                _levelText.text = $"Lv. {_stash.PlayerLevel}";

            if (_playerNameText != null)
                _playerNameText.text = _stash.PlayerName;
        }

        // ---------------------------------------------------------------------
        // World map (Play tab)

        private void PopulateMap()
        {
            if (_mapContainer == null || _mapPointPrefab == null) return;
            if (_stash == null || _stash.BiomeRegistry == null) return;

            // Clear existing points.
            for (int i = _mapContainer.childCount - 1; i >= 0; i--)
                Destroy(_mapContainer.GetChild(i).gameObject);
            _mapPoints.Clear();

            foreach (var biome in _stash.BiomeRegistry.All)
            {
                if (biome == null) continue;

                var go = Instantiate(_mapPointPrefab, _mapContainer);
                var point = go.GetComponent<MapPointUI>();
                if (point == null) continue;

                point.Populate(biome, false);
                point.PlaceOnMap(_mapContainer);
                point.OnClicked += OnBiomeSelected;
                _mapPoints.Add(point);
            }
        }

        private void RestoreSelectedBiome()
        {
            if (_stash == null) return;

            var saved = _stash.GetSelectedBiome();
            if (saved != null)
            {
                OnBiomeSelected(saved);
            }
            else if (_stash.BiomeRegistry != null && _stash.BiomeRegistry.All.Count > 0)
            {
                OnBiomeSelected(_stash.BiomeRegistry.All[0]);
            }
        }

        private void OnBiomeSelected(BiomeDefinition biome)
        {
            _selectedBiome = biome;

            // Update map point selection visuals.
            foreach (var point in _mapPoints)
                point.SetSelected(point.Biome.BiomeId == biome.BiomeId);

            // Update detail panel.
            if (_selectedBiomeName != null)
                _selectedBiomeName.text = biome.BiomeName;

            if (_selectedBiomeDesc != null)
                _selectedBiomeDesc.text = biome.Description;

            if (_selectedBiomeDifficulty != null)
            {
                string stars = new string('\u2605', biome.DifficultyTier)
                             + new string('\u2606', 5 - biome.DifficultyTier);
                _selectedBiomeDifficulty.text = $"Difficulty: {stars}";
            }

            if (_selectedBiomePreview != null)
                _selectedBiomePreview.color = biome.CardColor;

            // Persist selection.
            _stash?.SetSelectedBiome(biome.BiomeId);
        }

        // ---------------------------------------------------------------------
        // Launch

        private void OnLaunchClicked()
        {
            if (_selectedBiome == null)
            {
                Debug.LogWarning("[MainMenuUI] No biome selected.");
                return;
            }

            // Store biome on GameBootstrap so WorldManager can read it.
            GameBootstrap.Instance?.SetSelectedBiome(_selectedBiome);

            Debug.Log($"[MainMenuUI] Launching run — biome: {_selectedBiome.BiomeName}");
            SceneManager.LoadScene("Game");
        }
    }
}
