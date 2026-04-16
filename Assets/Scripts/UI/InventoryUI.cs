using System.Collections.Generic;
using UnityEngine;
using TMPro;
using EverRealm.Exiles.Items;
using EverRealm.Exiles.Player;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Toggleable in-run inventory panel. Uses <see cref="InventoryPresenter"/>
    /// to convert runtime inventory state into <see cref="ItemViewData"/> and
    /// renders it via pooled <see cref="InventorySlotUI"/> instances.
    /// </summary>
    public sealed class InventoryUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private TMP_Text _titleText;

        [Header("Panel")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Tooltip")]
        [SerializeField] private ItemTooltipUI _tooltip;

        [Header("Display")]
        [SerializeField] private int _displaySlotCount = 20;

        private PlayerInventory _playerInventory;
        private PlayerCamera _playerCamera;
        private InventoryPresenter _presenter;
        private readonly List<InventorySlotUI> _slotInstances = new();
        private bool _isOpen;
        private int _selectedSlotIndex = -1;

        // -----------------------------------------------------------------

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerInventory = player.GetComponent<PlayerInventory>();
                _playerCamera = player.GetComponentInChildren<PlayerCamera>();
            }

            if (_playerInventory != null)
            {
                _presenter = new InventoryPresenter(
                    _playerInventory.Inventory, _displaySlotCount);
                _presenter.OnRefreshed += OnPresenterRefreshed;
            }

            SetOpen(false);
        }

        private void OnDestroy()
        {
            if (_presenter != null)
            {
                _presenter.OnRefreshed -= OnPresenterRefreshed;
                _presenter.Dispose();
            }
        }

        // -----------------------------------------------------------------

        /// <summary>Toggle the inventory panel open / closed.</summary>
        public void Toggle()
        {
            SetOpen(!_isOpen);
        }

        public bool IsOpen => _isOpen;

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
            }
            else
            {
                _selectedSlotIndex = -1;
                _tooltip?.Hide();
            }
        }

        // -----------------------------------------------------------------

        private void OnPresenterRefreshed()
        {
            if (_isOpen)
                Rebuild();
        }

        private void Rebuild()
        {
            if (_slotContainer == null || _slotPrefab == null || _presenter == null)
                return;

            var viewData = _presenter.ViewData;

            // Title with occupied / capacity.
            if (_titleText != null)
                _titleText.text = $"Inventory ({_presenter.OccupiedSlots}/{_displaySlotCount})";

            // Pool-friendly slot creation.
            EnsureSlotCount(viewData.Count);

            for (int i = 0; i < viewData.Count; i++)
            {
                _slotInstances[i].Populate(viewData[i], i);
                _slotInstances[i].SetSelected(i == _selectedSlotIndex);
                _slotInstances[i].gameObject.SetActive(true);
            }

            // Hide any excess pool entries.
            for (int i = viewData.Count; i < _slotInstances.Count; i++)
                _slotInstances[i].gameObject.SetActive(false);
        }

        private void EnsureSlotCount(int needed)
        {
            while (_slotInstances.Count < needed)
            {
                var go = Instantiate(_slotPrefab, _slotContainer);
                var slot = go.GetComponent<InventorySlotUI>();
                if (slot != null)
                {
                    slot.OnSlotClicked += OnSlotClicked;
                    slot.OnSlotHoverEnter += OnSlotHoverEnter;
                    slot.OnSlotHoverExit += OnSlotHoverExit;
                    _slotInstances.Add(slot);
                }
            }
        }

        // -----------------------------------------------------------------
        // Slot interaction

        private void OnSlotClicked(int index, ItemViewData data)
        {
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
    }
}
