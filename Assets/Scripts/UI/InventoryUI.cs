using System.Collections.Generic;
using UnityEngine;
using TMPro;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.Items;
using EverRealm.Exiles.Player;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// In-run inventory overlay. Tarkov/Arc Raiders split layout mirrored from
    /// the main-menu loadout screen: left = run inventory, right = equipment
    /// slots. Equipment can be unequipped mid-run — the item falls into the
    /// run inventory and follows normal extraction rules from there.
    /// </summary>
    public sealed class InventoryUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Run Inventory (Left)")]
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _infoText;

        [Header("Loadout — Equipment (Right)")]
        [SerializeField] private EquipmentSlotUI _headSlot;
        [SerializeField] private EquipmentSlotUI _chestSlot;
        [SerializeField] private EquipmentSlotUI _legsSlot;
        [SerializeField] private EquipmentSlotUI _primaryWeaponSlot;
        [SerializeField] private EquipmentSlotUI _secondaryWeaponSlot;
        [SerializeField] private TMP_Text _loadoutInfoText;

        [Header("Tooltip")]
        [SerializeField] private ItemTooltipUI _tooltip;

        [Header("Display")]
        [SerializeField] private int _displaySlotCount = 28;

        private PlayerInventory _playerInventory;
        private PlayerCamera _playerCamera;
        private PlayerCombat _playerCombat;
        private InventoryPresenter _presenter;
        private LoadoutPresenter _loadoutPresenter;
        private readonly List<InventorySlotUI> _slotInstances = new();
        private EquipmentSlotUI[] _equipSlots;
        private bool _isOpen;
        private int _selectedSlotIndex = -1;

        public bool IsOpen => _isOpen;

        // -----------------------------------------------------------------

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerInventory = player.GetComponent<PlayerInventory>();
                _playerCamera = player.GetComponentInChildren<PlayerCamera>();
                _playerCombat = player.GetComponent<PlayerCombat>();
            }

            if (_playerInventory != null)
            {
                _presenter = new InventoryPresenter(
                    _playerInventory.Inventory, _displaySlotCount);
                _presenter.OnRefreshed += OnInventoryRefreshed;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UIDesyncDebugOverlay.EnsureInstance()
                    .RegisterInventoryPresenter(_presenter, _playerInventory.Inventory);
#endif
            }

            var stash = GameBootstrap.Instance?.Stash;
            if (stash != null)
            {
                _loadoutPresenter = new LoadoutPresenter(stash);
                _loadoutPresenter.OnRefreshed += OnLoadoutRefreshed;
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
            if (_presenter != null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (UIDesyncDebugOverlay.EnsureInstance() != null)
                    UIDesyncDebugOverlay.EnsureInstance().UnregisterInventoryPresenter(_presenter);
#endif
                _presenter.OnRefreshed -= OnInventoryRefreshed;
                _presenter.Dispose();
            }

            if (_loadoutPresenter != null)
            {
                _loadoutPresenter.OnRefreshed -= OnLoadoutRefreshed;
                _loadoutPresenter.Dispose();
            }
        }

        // -----------------------------------------------------------------

        public void Toggle() => SetOpen(!_isOpen);

        private void SetOpen(bool open)
        {
            _isOpen = open;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = open ? 1f : 0f;
                _canvasGroup.blocksRaycasts = open;
                _canvasGroup.interactable = open;
            }

            if (_playerCamera != null)
                _playerCamera.SetCursorLocked(!open);

            if (open)
            {
                _presenter?.Refresh();
                _loadoutPresenter?.Refresh();
            }
            else
            {
                _selectedSlotIndex = -1;
                _tooltip?.Hide();
            }
        }

        // -----------------------------------------------------------------
        // Run inventory (left panel)

        private void OnInventoryRefreshed()
        {
            if (_isOpen) RebuildInventory();
        }

        private void RebuildInventory()
        {
            if (_slotContainer == null || _slotPrefab == null || _presenter == null)
                return;

            var viewData = _presenter.ViewData;

            if (_titleText != null)
                _titleText.text = $"INVENTORY ({_presenter.OccupiedSlots})";

            EnsureSlotCount(viewData.Count);

            for (int i = 0; i < viewData.Count; i++)
            {
                _slotInstances[i].Populate(viewData[i], i);
                _slotInstances[i].SetSelected(i == _selectedSlotIndex);
                _slotInstances[i].gameObject.SetActive(true);
            }

            for (int i = viewData.Count; i < _slotInstances.Count; i++)
                _slotInstances[i].gameObject.SetActive(false);

            if (_infoText != null)
                _infoText.text =
                    $"Items: {_presenter.OccupiedSlots}  |  " +
                    $"Value: {_presenter.TotalValue}g";
        }

        private void EnsureSlotCount(int needed)
        {
            if (_slotContainer == null || _slotPrefab == null) return;

            while (_slotInstances.Count < needed)
            {
                var go = Instantiate(_slotPrefab, _slotContainer);
                var slot = go.GetComponent<InventorySlotUI>();
                if (slot == null)
                {
                    // Guard: a silently-dropped continue here would leak one
                    // GameObject per iteration and loop forever.
                    Debug.LogError(
                        "[InventoryUI] _slotPrefab is missing InventorySlotUI — aborting pool expansion.");
                    Destroy(go);
                    break;
                }

                slot.OnSlotClicked   += OnSlotClicked;
                slot.OnSlotHoverEnter += OnSlotHoverEnter;
                slot.OnSlotHoverExit  += OnSlotHoverExit;
                _slotInstances.Add(slot);
            }
        }

        private void OnSlotClicked(int index, ItemViewData data)
        {
            // Equippable items equip on click (mirrors the main-menu stash flow).
            // Non-equippable items just toggle selection.
            if (!data.IsEmpty && data.EquipSlot != EquipSlot.None
                && _playerInventory != null)
            {
                var stash = GameBootstrap.Instance?.Stash;
                if (stash != null &&
                    stash.EquipFromRunInventory(data.ItemId, _playerInventory.Inventory))
                {
                    if (_playerCombat != null)
                    {
                        _playerCombat.SetWeapon(stash.GetSelectedWeapon());
                        _playerCombat.SetArmorDefense(stash.Loadout.TotalDefense);
                    }
                    return;
                }
            }

            _selectedSlotIndex = _selectedSlotIndex == index ? -1 : index;
            for (int i = 0; i < _slotInstances.Count; i++)
                _slotInstances[i].SetSelected(i == _selectedSlotIndex);
        }

        private void OnSlotHoverEnter(InventorySlotUI slot)
        {
            _tooltip?.Show(slot.Data, slot.GetComponent<RectTransform>());
        }

        private void OnSlotHoverExit(InventorySlotUI slot)
        {
            _tooltip?.Hide();
        }

        // -----------------------------------------------------------------
        // Equipment (right panel, read-only during run)

        private void WireEquipmentSlots()
        {
            if (_equipSlots == null) return;
            foreach (var slot in _equipSlots)
            {
                if (slot == null) continue;
                slot.OnSlotHoverEnter += OnEquipSlotHoverEnter;
                slot.OnSlotHoverExit += OnEquipSlotHoverExit;
                slot.OnSlotClicked += OnEquipSlotClicked;
            }
        }

        // Click an equipped slot to unequip into the run inventory. Weapons
        // also re-sync PlayerCombat so the swing model and armor totals
        // reflect the new loadout state immediately.
        private void OnEquipSlotClicked(EquipSlot slotType, ItemViewData data)
        {
            if (data.IsEmpty || _playerInventory == null) return;

            var stash = GameBootstrap.Instance?.Stash;
            if (stash == null) return;

            if (!stash.UnequipToRunInventory(slotType, _playerInventory.Inventory))
                return;

            if (_playerCombat != null)
            {
                _playerCombat.SetWeapon(stash.GetSelectedWeapon());
                _playerCombat.SetArmorDefense(stash.Loadout.TotalDefense);
            }
        }

        private void OnLoadoutRefreshed()
        {
            if (_isOpen) RebuildEquipment();
        }

        private void RebuildEquipment()
        {
            if (_equipSlots == null || _loadoutPresenter == null) return;

            var equipData = _loadoutPresenter.EquipmentData;

            foreach (var slot in _equipSlots)
            {
                if (slot == null) continue;

                if (equipData.TryGetValue(slot.SlotType, out var data))
                    slot.Populate(data);
                else
                    slot.ShowEmpty();
            }

            if (_loadoutInfoText != null)
                _loadoutInfoText.text = $"Defense: {_loadoutPresenter.TotalDefense:F0}";
        }

        private void OnEquipSlotHoverEnter(EquipmentSlotUI slot)
        {
            _tooltip?.Show(slot.Data, slot.GetComponent<RectTransform>());
        }

        private void OnEquipSlotHoverExit(EquipmentSlotUI slot)
        {
            _tooltip?.Hide();
        }
    }
}
