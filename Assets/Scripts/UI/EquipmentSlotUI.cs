using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// A named equipment slot (Head, Chest, etc.) on the loadout panel.
    /// Displays the currently equipped item or a placeholder label.
    /// </summary>
    public sealed class EquipmentSlotUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Slot Identity")]
        [SerializeField] private EquipSlot _slotType;

        [Header("Visuals")]
        [SerializeField] private Image _icon;
        [SerializeField] private Image _border;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _slotLabel;

        [Header("Colours")]
        [SerializeField] private Color _emptyBorderColor = new(0.25f, 0.25f, 0.25f, 0.5f);
        [SerializeField] private Color _emptyBgColor = new(0.10f, 0.10f, 0.14f, 0.9f);
        [SerializeField] private Color _hoveredBgColor = new(0.18f, 0.18f, 0.24f, 0.95f);
        [SerializeField] private Color _occupiedBorderColor = new(0.3f, 0.8f, 1f, 0.8f);

        private ItemViewData _data;
        private bool _isHovered;

        public EquipSlot SlotType => _slotType;
        public ItemViewData Data => _data;

        /// <summary>Fired when the slot is clicked. Args: (slotType, data).</summary>
        public event Action<EquipSlot, ItemViewData> OnSlotClicked;

        /// <summary>Fired on pointer enter for tooltip display.</summary>
        public event Action<EquipmentSlotUI> OnSlotHoverEnter;

        /// <summary>Fired on pointer exit to hide tooltip.</summary>
        public event Action<EquipmentSlotUI> OnSlotHoverExit;

        // -----------------------------------------------------------------

        /// <summary>Show an equipped item in this slot.</summary>
        public void Populate(ItemViewData data)
        {
            _data = data;

            if (data.IsEmpty)
            {
                ShowEmpty();
                return;
            }

            if (_icon != null)
            {
                if (data.HasIcon)
                {
                    _icon.sprite = data.Icon;
                    _icon.enabled = true;
                    _icon.color = Color.white;
                }
                else
                {
                    _icon.sprite = null;
                    _icon.enabled = true;
                    _icon.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
                }
            }

            if (_slotLabel != null)
                _slotLabel.enabled = false; // hide label when item is shown

            UpdateVisuals();
        }

        /// <summary>Show the slot as empty with its type label.</summary>
        public void ShowEmpty()
        {
            _data = ItemViewData.Empty;

            if (_icon != null)
            {
                _icon.sprite = null;
                _icon.enabled = false;
            }

            if (_slotLabel != null)
            {
                _slotLabel.text = GetSlotDisplayName(_slotType);
                _slotLabel.enabled = true;
            }

            UpdateVisuals();
        }

        // -----------------------------------------------------------------

        private void UpdateVisuals()
        {
            if (_border != null)
            {
                if (!_data.IsEmpty)
                    _border.color = _data.RarityColor;
                else
                    _border.color = _emptyBorderColor;
            }

            if (_background != null)
                _background.color = _isHovered && !_data.IsEmpty ? _hoveredBgColor : _emptyBgColor;
        }

        // -----------------------------------------------------------------

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            UpdateVisuals();

            if (!_data.IsEmpty)
                OnSlotHoverEnter?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            UpdateVisuals();
            OnSlotHoverExit?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnSlotClicked?.Invoke(_slotType, _data);
        }

        // -----------------------------------------------------------------

        private static string GetSlotDisplayName(EquipSlot slot)
        {
            return slot switch
            {
                EquipSlot.Head            => "HEAD",
                EquipSlot.Chest           => "CHEST",
                EquipSlot.Legs            => "LEGS",
                EquipSlot.PrimaryWeapon   => "PRIMARY",
                EquipSlot.SecondaryWeapon => "SECONDARY",
                _                         => "SLOT"
            };
        }
    }
}
