using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Visual representation of a weapon choice in the loadout screen.
    /// </summary>
    public sealed class WeaponButtonUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image _border;
        [SerializeField] private Button _button;

        private static readonly Color SelectedColor = new(0.3f, 0.8f, 1f, 1f);
        private static readonly Color NormalColor = new(0.3f, 0.3f, 0.3f, 0.8f);

        public WeaponDefinition Weapon { get; private set; }
        public Button Button => _button;

        /// <summary>Set up the button with a weapon definition and selection state.</summary>
        public void Populate(WeaponDefinition weapon, bool isSelected)
        {
            Weapon = weapon;

            if (_nameText != null)
                _nameText.text = weapon.WeaponName;

            SetSelected(isSelected);
        }

        /// <summary>Toggle the visual selection highlight.</summary>
        public void SetSelected(bool selected)
        {
            if (_border != null)
                _border.color = selected ? SelectedColor : NormalColor;
        }
    }
}
