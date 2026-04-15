using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.Items;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Displays a single inventory slot: item icon, stack count, and rarity-coloured border.
    /// </summary>
    public sealed class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private Image _border;
        [SerializeField] private Image _background;

        /// <summary>Populate this slot with an item stack.</summary>
        public void Populate(ItemStack stack)
        {
            if (stack.IsEmpty)
            {
                Clear();
                return;
            }

            if (_icon != null)
            {
                _icon.sprite = stack.Definition.Icon;
                _icon.enabled = stack.Definition.Icon != null;
                _icon.color = Color.white;
            }

            if (_countText != null)
            {
                _countText.text = stack.Count > 1 ? stack.Count.ToString() : "";
                _countText.enabled = true;
            }

            if (_border != null)
                _border.color = GetRarityColor(stack.Definition.Rarity);
        }

        /// <summary>Show an empty slot.</summary>
        public void Clear()
        {
            if (_icon != null)
            {
                _icon.sprite = null;
                _icon.enabled = false;
            }

            if (_countText != null)
                _countText.enabled = false;

            if (_border != null)
                _border.color = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        }

        private static Color GetRarityColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => new Color(0.6f, 0.6f, 0.6f, 0.8f),
                ItemRarity.Rare   => new Color(0.2f, 0.5f, 1f, 0.9f),
                ItemRarity.Epic   => new Color(0.7f, 0.3f, 0.9f, 0.9f),
                _                 => Color.white
            };
        }
    }
}
