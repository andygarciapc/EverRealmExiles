using UnityEngine;
using TMPro;
using EverRealm.Exiles.Items;
using EverRealm.Exiles.Player;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Toggleable inventory panel. Rebuilds the slot grid from the player's
    /// inventory each time it opens and whenever inventory contents change.
    /// </summary>
    public sealed class InventoryUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private TMP_Text _titleText;

        [Header("Panel")]
        [SerializeField] private CanvasGroup _canvasGroup;

        private PlayerInventory _inventory;
        private PlayerCamera _playerCamera;
        private bool _isOpen;

        // -----------------------------------------------------------------

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _inventory = player.GetComponent<PlayerInventory>();
                _playerCamera = player.GetComponentInChildren<PlayerCamera>();
            }

            if (_inventory != null)
                _inventory.Inventory.OnChanged += OnInventoryChanged;

            // Start closed.
            SetOpen(false);
        }

        private void OnDestroy()
        {
            if (_inventory != null)
                _inventory.Inventory.OnChanged -= OnInventoryChanged;
        }

        // -----------------------------------------------------------------

        /// <summary>Toggle the inventory panel open/closed.</summary>
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

            // Cursor and camera control.
            if (_playerCamera != null)
                _playerCamera.SetCursorLocked(!open);

            if (open)
                Rebuild();
        }

        public bool IsOpen => _isOpen;

        // -----------------------------------------------------------------

        private void OnInventoryChanged()
        {
            if (_isOpen)
                Rebuild();
        }

        private void Rebuild()
        {
            if (_slotContainer == null || _slotPrefab == null) return;

            // Clear existing slots.
            for (int i = _slotContainer.childCount - 1; i >= 0; i--)
                Destroy(_slotContainer.GetChild(i).gameObject);

            if (_inventory == null) return;

            var slots = _inventory.Inventory.Slots;

            if (_titleText != null)
                _titleText.text = $"Inventory ({slots.Count})";

            for (int i = 0; i < slots.Count; i++)
            {
                var go = Instantiate(_slotPrefab, _slotContainer);
                var slotUI = go.GetComponent<InventorySlotUI>();
                if (slotUI != null)
                    slotUI.Populate(slots[i]);
            }
        }
    }
}
