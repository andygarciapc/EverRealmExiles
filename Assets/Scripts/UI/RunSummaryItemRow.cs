using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EverRealm.Exiles.Items;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Displays a single item stack in the run summary list.
    /// Attach to a prefab with Image (icon), TMP_Text (name), TMP_Text (count).
    /// </summary>
    public sealed class RunSummaryItemRow : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _countText;

        public void Populate(ItemStack stack)
        {
            if (_icon != null)
            {
                if (stack.Definition.Icon != null)
                {
                    _icon.sprite = stack.Definition.Icon;
                    _icon.enabled = true;
                }
                else
                {
                    _icon.enabled = false;
                }
            }

            if (_nameText != null)
                _nameText.text = stack.Definition.DisplayName;

            if (_countText != null)
                _countText.text = stack.Count > 1 ? $"x{stack.Count}" : string.Empty;
        }
    }
}
