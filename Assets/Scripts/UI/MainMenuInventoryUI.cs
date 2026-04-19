using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Tab-toggled overlay in the main menu with a Tarkov/Arc Raiders split layout:
    /// left panel = persistent stash, right panel = equipment slots + backpack.
    /// Click items in stash to equip/move to backpack; click loadout items to return them.
    /// </summary>
    public sealed class MainMenuInventoryUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Stash (Left)")]
        [SerializeField] private Transform _stashSlotContainer;
        [SerializeField] private GameObject _stashSlotPrefab;
        [SerializeField] private TMP_Text _stashTitle;

        [Header("Loadout — Equipment (Right)")]
        [SerializeField] private EquipmentSlotUI _headSlot;
        [SerializeField] private EquipmentSlotUI _chestSlot;
        [SerializeField] private EquipmentSlotUI _legsSlot;
        [SerializeField] private EquipmentSlotUI _primaryWeaponSlot;
        [SerializeField] private EquipmentSlotUI _secondaryWeaponSlot;

        [Header("Loadout — Backpack (Right)")]
        [SerializeField] private Transform _backpackSlotContainer;
        [SerializeField] private TMP_Text _backpackTitle;

        [Header("Info")]
        [SerializeField] private TMP_Text _stashInfoText;
        [SerializeField] private TMP_Text _loadoutInfoText;

        [Header("Tooltip")]
        [SerializeField] private ItemTooltipUI _tooltip;

        private StashManager _stash;
        private StashPresenter _stashPresenter;
        private LoadoutPresenter _loadoutPresenter;
        private readonly List<InventorySlotUI> _stashSlots = new();
        private readonly List<InventorySlotUI> _backpackSlots = new();
        private EquipmentSlotUI[] _equipSlots;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        // ---------------------------------------------------------------------

        /// <summary>Wire the overlay to the stash. Call once from MainMenuUI.Show().</summary>
        public void Initialize(StashManager stash)
        {
            _stash = stash;

            if (_stash != null)
            {
                _stashPresenter = new StashPresenter(_stash);
                _stashPresenter.OnRefreshed += OnStashRefreshed;

                _loadoutPresenter = new LoadoutPresenter(_stash);
                _loadoutPresenter.OnRefreshed += OnLoadoutRefreshed;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UIDesyncDebugOverlay.EnsureInstance()
                    .RegisterStashPresenter(_stashPresenter, _stash);
#endif
            }

            _equipSlots = new[]
            {
                _headSlot, _chestSlot, _legsSlot,
                _primaryWeaponSlot, _secondaryWeaponSlot
            };

            WireEquipmentSlots();
            SetOpen(false);
        }

        private void OnDestroy()
        {
            if (_stashPresenter != null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (UIDesyncDebugOverlay.EnsureInstance() != null)
                    UIDesyncDebugOverlay.EnsureInstance().UnregisterStashPresenter(_stashPresenter);
#endif
                _stashPresenter.OnRefreshed -= OnStashRefreshed;
                _stashPresenter.Dispose();
            }

            if (_loadoutPresenter != null)
            {
                _loadoutPresenter.OnRefreshed -= OnLoadoutRefreshed;
                _loadoutPresenter.Dispose();
            }
        }

        // ---------------------------------------------------------------------

        /// <summary>Toggle the overlay open / closed.</summary>
        public void Toggle()
        {
            SetOpen(!_isOpen);
        }

        private void SetOpen(bool open)
        {
            _isOpen = open;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = open ? 1f : 0f;
                _canvasGroup.blocksRaycasts = open;
                _canvasGroup.interactable = open;
            }

            if (open)
            {
                _stashPresenter?.Refresh();
                _loadoutPresenter?.Refresh();
            }
            else
            {
                _tooltip?.Hide();
            }
        }

        // ---------------------------------------------------------------------
        // Stash (left panel)

        private void OnStashRefreshed()
        {
            if (_isOpen) RebuildStashSlots();
        }

        private void RebuildStashSlots()
        {
            if (_stashSlotContainer == null || _stashSlotPrefab == null || _stashPresenter == null)
                return;

            var viewData = _stashPresenter.ViewData;

            if (_stashTitle != null)
                _stashTitle.text = $"STASH ({_stashPresenter.OccupiedSlots})";

            EnsureSlotCount(_stashSlots, _stashSlotContainer, viewData.Count);

            for (int i = 0; i < viewData.Count; i++)
            {
                _stashSlots[i].Populate(viewData[i], i);
                _stashSlots[i].gameObject.SetActive(true);
            }

            for (int i = viewData.Count; i < _stashSlots.Count; i++)
                _stashSlots[i].gameObject.SetActive(false);

            UpdateStashInfo();
        }

        private void UpdateStashInfo()
        {
            if (_stashInfoText == null || _stashPresenter == null) return;

            _stashInfoText.text =
                $"Items: {_stashPresenter.TotalItemCount}  |  " +
                $"Value: {_stashPresenter.TotalValue}g";
        }

        // Click stash slot → move to loadout
        private void OnStashSlotClicked(int index, ItemViewData data)
        {
            if (data.IsEmpty || _stash == null) return;
            _stash.EquipFromStash(data.ItemId);
        }

        // ---------------------------------------------------------------------
        // Equipment slots (right panel, top)

        private void WireEquipmentSlots()
        {
            if (_equipSlots == null) return;

            foreach (var slot in _equipSlots)
            {
                if (slot == null) continue;
                slot.OnSlotClicked += OnEquipSlotClicked;
                slot.OnSlotHoverEnter += OnEquipSlotHoverEnter;
                slot.OnSlotHoverExit += OnEquipSlotHoverExit;
            }
        }

        private void OnEquipSlotClicked(EquipSlot slotType, ItemViewData data)
        {
            if (data.IsEmpty || _stash == null) return;
            _stash.UnequipToStash(slotType);
        }

        private void OnEquipSlotHoverEnter(EquipmentSlotUI slot)
        {
            _tooltip?.Show(slot.Data, slot.GetComponent<RectTransform>());
        }

        private void OnEquipSlotHoverExit(EquipmentSlotUI slot)
        {
            _tooltip?.Hide();
        }

        // ---------------------------------------------------------------------
        // Backpack (right panel, bottom)

        private void RebuildBackpackSlots()
        {
            if (_backpackSlotContainer == null || _stashSlotPrefab == null || _loadoutPresenter == null)
                return;

            var viewData = _loadoutPresenter.BackpackData;

            if (_backpackTitle != null)
                _backpackTitle.text = $"BACKPACK ({_loadoutPresenter.BackpackUsed}/{_loadoutPresenter.BackpackCapacity})";

            EnsureSlotCount(_backpackSlots, _backpackSlotContainer, viewData.Count);

            for (int i = 0; i < viewData.Count; i++)
            {
                _backpackSlots[i].Populate(viewData[i], i);
                _backpackSlots[i].gameObject.SetActive(true);
            }

            for (int i = viewData.Count; i < _backpackSlots.Count; i++)
                _backpackSlots[i].gameObject.SetActive(false);
        }

        // Click backpack slot → return to stash
        private void OnBackpackSlotClicked(int index, ItemViewData data)
        {
            if (data.IsEmpty || _stash == null) return;
            _stash.RemoveFromBackpack(data.ItemId);
        }

        // ---------------------------------------------------------------------
        // Loadout refresh (equipment + backpack)

        private void OnLoadoutRefreshed()
        {
            if (!_isOpen) return;

            RebuildEquipmentDisplay();
            RebuildBackpackSlots();
            UpdateLoadoutInfo();
        }

        private void RebuildEquipmentDisplay()
        {
            if (_loadoutPresenter == null) return;

            var equipData = _loadoutPresenter.EquipmentData;

            foreach (var slot in _equipSlots)
            {
                if (slot == null) continue;

                if (equipData.TryGetValue(slot.SlotType, out var data))
                    slot.Populate(data);
                else
                    slot.ShowEmpty();
            }
        }

        private void UpdateLoadoutInfo()
        {
            if (_loadoutInfoText == null || _loadoutPresenter == null) return;

            _loadoutInfoText.text =
                $"Defense: {_loadoutPresenter.TotalDefense:F0}  |  " +
                $"Backpack: {_loadoutPresenter.BackpackUsed}/{_loadoutPresenter.BackpackCapacity}";
        }

        // ---------------------------------------------------------------------
        // Tooltip (shared)

        private void OnSlotHoverEnter(InventorySlotUI slot)
        {
            _tooltip?.Show(slot.Data, slot.GetComponent<RectTransform>());
        }

        private void OnSlotHoverExit(InventorySlotUI slot)
        {
            _tooltip?.Hide();
        }

        // ---------------------------------------------------------------------
        // Slot pooling (shared between stash and backpack grids)

        private void EnsureSlotCount(List<InventorySlotUI> pool, Transform container, int needed)
        {
            if (container == null || _stashSlotPrefab == null) return;

            while (pool.Count < needed)
            {
                var go = Instantiate(_stashSlotPrefab, container);
                var slot = go.GetComponent<InventorySlotUI>();
                if (slot == null)
                {
                    // Guard: a silent `continue` here would spin forever since
                    // the pool count never advances.
                    Debug.LogError(
                        "[MainMenuInventoryUI] _stashSlotPrefab is missing InventorySlotUI — aborting pool expansion.");
                    Destroy(go);
                    break;
                }

                slot.OnSlotHoverEnter += OnSlotHoverEnter;
                slot.OnSlotHoverExit  += OnSlotHoverExit;

                // Wire click to the correct handler based on which pool this is.
                if (pool == _stashSlots)
                    slot.OnSlotClicked += OnStashSlotClicked;
                else
                    slot.OnSlotClicked += OnBackpackSlotClicked;

                pool.Add(slot);
            }
        }
    }
}
