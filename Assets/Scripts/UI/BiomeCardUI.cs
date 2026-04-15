using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// UI component for a single biome card in the Play tab's map grid.
    /// Shows the biome's color/icon, name, and difficulty tier.
    /// </summary>
    public sealed class BiomeCardUI : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _selectionBorder;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _difficultyText;
        [SerializeField] private Button _button;

        private BiomeDefinition _biome;

        public BiomeDefinition Biome => _biome;
        public Button Button => _button;

        /// <summary>Fires when this card is clicked. Arg: the biome definition.</summary>
        public event Action<BiomeDefinition> OnClicked;

        public void Populate(BiomeDefinition biome, bool isSelected)
        {
            _biome = biome;

            if (_background != null)
                _background.color = biome.CardColor;

            if (_icon != null)
            {
                if (biome.Icon != null)
                {
                    _icon.sprite = biome.Icon;
                    _icon.color = Color.white;
                    _icon.enabled = true;
                }
                else
                {
                    _icon.enabled = false;
                }
            }

            if (_nameText != null)
                _nameText.text = biome.BiomeName;

            if (_difficultyText != null)
            {
                // Show difficulty as filled stars.
                string stars = new string('\u2605', biome.DifficultyTier)
                             + new string('\u2606', 5 - biome.DifficultyTier);
                _difficultyText.text = stars;
            }

            SetSelected(isSelected);

            if (_button != null)
                _button.onClick.AddListener(() => OnClicked?.Invoke(_biome));
        }

        public void SetSelected(bool selected)
        {
            if (_selectionBorder != null)
                _selectionBorder.enabled = selected;
        }
    }
}
