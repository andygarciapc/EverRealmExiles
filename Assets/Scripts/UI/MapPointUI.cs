using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// A clickable point on the world map representing a deployment zone.
    /// Positioned using the biome's MapPosition (normalized 0-1 coordinates).
    /// </summary>
    public sealed class MapPointUI : MonoBehaviour
    {
        [SerializeField] private Image _marker;
        [SerializeField] private Image _glow;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Button _button;

        private BiomeDefinition _biome;

        public BiomeDefinition Biome => _biome;
        public Button Button => _button;

        /// <summary>Fires when this point is clicked.</summary>
        public event Action<BiomeDefinition> OnClicked;

        private static readonly Color SelectedColor = new(1f, 0.85f, 0.3f, 1f);
        private static readonly Color NormalColor   = new(0.9f, 0.9f, 0.9f, 1f);

        public void Populate(BiomeDefinition biome, bool isSelected)
        {
            _biome = biome;

            if (_marker != null)
                _marker.color = biome.CardColor;

            if (_label != null)
                _label.text = biome.BiomeName;

            SetSelected(isSelected);

            if (_button != null)
                _button.onClick.AddListener(() => OnClicked?.Invoke(_biome));
        }

        /// <summary>
        /// Place this point on the map using the biome's normalized MapPosition.
        /// </summary>
        public void PlaceOnMap(RectTransform mapRect)
        {
            if (_biome == null) return;
            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            rt.anchorMin = _biome.MapPosition;
            rt.anchorMax = _biome.MapPosition;
            rt.anchoredPosition = Vector2.zero;
        }

        public void SetSelected(bool selected)
        {
            if (_glow != null)
                _glow.enabled = selected;

            if (_marker != null)
            {
                // Pulse the marker slightly when selected.
                _marker.transform.localScale = selected ? Vector3.one * 1.2f : Vector3.one;
            }

            if (_label != null)
                _label.color = selected ? SelectedColor : NormalColor;
        }
    }
}
