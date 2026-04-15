using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EverRealm.Exiles.Extraction;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Screen-space progress bar shown while an <see cref="ExtractionZone"/>
    /// is active. Subscribes to the zone's progress events.
    /// </summary>
    public sealed class ExtractionProgressUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _fillBar;
        [SerializeField] private TMP_Text _label;

        private ExtractionZone _activeZone;

        private void Start()
        {
            Hide();
        }

        /// <summary>Begin tracking an extraction zone's progress.</summary>
        public void Bind(ExtractionZone zone)
        {
            Unbind();

            _activeZone = zone;
            _activeZone.OnProgressChanged += SetProgress;
            _activeZone.OnExtractionComplete += OnComplete;
            Show();
            SetProgress(0f);
        }

        /// <summary>Stop tracking the current extraction zone.</summary>
        public void Unbind()
        {
            if (_activeZone != null)
            {
                _activeZone.OnProgressChanged -= SetProgress;
                _activeZone.OnExtractionComplete -= OnComplete;
                _activeZone = null;
            }
            Hide();
        }

        public void Show()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_label != null)
                _label.text = "Extracting...";
        }

        public void Hide()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        public void SetProgress(float normalised)
        {
            if (_fillBar != null)
                _fillBar.fillAmount = normalised;

            if (_label != null && _activeZone != null)
            {
                _label.text = _activeZone.PlayerInRange
                    ? "Extracting..."
                    : "Return to extraction zone!";
            }
        }

        private void OnComplete()
        {
            if (_label != null)
                _label.text = "Extracted!";
        }
    }
}
