using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EverRealm.Exiles.Items;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Displays a single item stack in the run summary list.
    /// Supports both <see cref="ItemViewData"/> and raw <see cref="ItemStack"/>.
    /// </summary>
    public sealed class RunSummaryItemRow : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private Image _rarityBar;

        /// <summary>Populate from presentation data (preferred path).</summary>
        public void Populate(ItemViewData data)
        {
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
                    _icon.enabled = false;
                }
            }

            if (_nameText != null)
            {
                _nameText.text = data.DisplayName;
                _nameText.color = data.RarityColor;
            }

            if (_countText != null)
                _countText.text = data.Count > 1 ? $"x{data.Count}" : string.Empty;

            if (_rarityBar != null)
                _rarityBar.color = data.RarityColor;
        }

        /// <summary>Populate from a raw ItemStack (backwards compatible).</summary>
        public void Populate(ItemStack stack)
        {
            if (stack.IsEmpty || stack.Definition == null)
            {
                Populate(ItemViewData.Invalid);
                return;
            }

            Populate(ItemViewData.FromStack(stack));
        }
    }
}
