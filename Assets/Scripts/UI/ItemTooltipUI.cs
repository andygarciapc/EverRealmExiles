using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Tooltip panel that displays detailed item information when hovering
    /// inventory or stash slots. One instance per canvas that contains slots.
    /// </summary>
    public sealed class ItemTooltipUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelRect;

        [Header("Content")]
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _rarityText;
        [SerializeField] private TMP_Text _typeText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private TMP_Text _weightText;

        [Header("Equipment (optional)")]
        [SerializeField] private TMP_Text _defenseText;
        [SerializeField] private TMP_Text _equipSlotText;

        [Header("Settings")]
        [SerializeField] private Vector2 _offset = new(12f, -12f);

        private RectTransform _canvasRect;

        private void Awake()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                _canvasRect = canvas.GetComponent<RectTransform>();

            Hide();
        }

        /// <summary>Show the tooltip with item data, positioned near the given slot.</summary>
        public void Show(ItemViewData data, RectTransform slotRect)
        {
            if (data.IsEmpty)
            {
                Hide();
                return;
            }

            if (_nameText != null)
            {
                _nameText.text = data.DisplayName;
                _nameText.color = data.RarityColor;
            }

            if (_rarityText != null)
                _rarityText.text = data.RarityName;

            if (_typeText != null)
                _typeText.text = data.TypeName;

            if (_descriptionText != null)
            {
                bool hasDesc = !string.IsNullOrEmpty(data.Description);
                _descriptionText.text = data.Description;
                _descriptionText.gameObject.SetActive(hasDesc);
            }

            if (_valueText != null)
                _valueText.text = $"Value: {data.Value}g";

            if (_weightText != null)
                _weightText.text = $"Weight: {data.Weight:F1}";

            if (_defenseText != null)
            {
                bool hasDef = data.DefenseValue > 0f;
                _defenseText.text = hasDef ? $"Defense: +{data.DefenseValue:F0}" : "";
                _defenseText.gameObject.SetActive(hasDef);
            }

            if (_equipSlotText != null)
            {
                bool equippable = data.EquipSlot != EquipSlot.None;
                _equipSlotText.text = equippable ? $"Slot: {data.EquipSlot}" : "";
                _equipSlotText.gameObject.SetActive(equippable);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = false;
            }

            PositionNearSlot(slotRect);
        }

        /// <summary>Hide the tooltip.</summary>
        public void Hide()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        private void PositionNearSlot(RectTransform slotRect)
        {
            if (_panelRect == null || slotRect == null) return;

            // Get the top-right corner of the slot in world space.
            Vector3[] corners = new Vector3[4];
            slotRect.GetWorldCorners(corners);
            Vector3 worldPos = corners[2]; // top-right

            // Determine camera based on canvas render mode.
            Camera cam = null;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            // Convert to local position within the tooltip's parent.
            RectTransform parentRect = _panelRect.parent as RectTransform;
            if (parentRect == null) return;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, screenPoint, cam, out Vector2 localPoint);

            _panelRect.anchoredPosition = localPoint + _offset;

            ClampToCanvas();
        }

        private void ClampToCanvas()
        {
            if (_panelRect == null || _canvasRect == null) return;

            Vector2 pos = _panelRect.anchoredPosition;
            Vector2 panelSize = _panelRect.sizeDelta;
            Vector2 canvasSize = _canvasRect.sizeDelta;

            float halfW = canvasSize.x * 0.5f;
            float halfH = canvasSize.y * 0.5f;

            // Keep tooltip within canvas bounds.
            if (pos.x + panelSize.x > halfW)
                pos.x = halfW - panelSize.x - 10f;
            if (pos.x < -halfW)
                pos.x = -halfW + 10f;
            if (pos.y > halfH)
                pos.y = halfH - 10f;
            if (pos.y - panelSize.y < -halfH)
                pos.y = -halfH + panelSize.y + 10f;

            _panelRect.anchoredPosition = pos;
        }
    }
}
