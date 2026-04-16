using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.Extraction;
using EverRealm.Exiles.Items;
using EverRealm.Exiles.Player;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Main in-game HUD. Displays health, stamina, crosshair, interaction
    /// prompts, kill counter, run timer, extraction countdown, and loot
    /// pickup notifications.
    /// Instantiated by <see cref="RunManager"/> at run start, destroyed at run end.
    /// </summary>
    public sealed class GameHUD : MonoBehaviour
    {
        [Header("Health & Stamina")]
        [SerializeField] private Image _healthFill;
        [SerializeField] private Image _staminaFill;

        [Header("Interaction")]
        [SerializeField] private TMP_Text _interactPrompt;

        [Header("Run Stats")]
        [SerializeField] private TMP_Text _killCountText;
        [SerializeField] private TMP_Text _runTimerText;

        [Header("Extraction")]
        [SerializeField] private CanvasGroup _extractionGroup;
        [SerializeField] private Image _extractionFill;
        [SerializeField] private TMP_Text _extractionText;

        [Header("Loot Notification")]
        [SerializeField] private TMP_Text _lootNotification;
        [SerializeField] private float _lootDisplayDuration = 2f;
        [SerializeField] private float _lootFadeDuration = 0.5f;

        private PlayerCombat _combat;
        private PlayerController _controller;
        private PlayerInventory _inventory;
        private ExtractionZone _activeZone;

        private float _lootTimer;

        // ---------------------------------------------------------------------

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[GameHUD] Player not found.");
                return;
            }

            _combat = player.GetComponent<PlayerCombat>();
            _controller = player.GetComponent<PlayerController>();
            _inventory = player.GetComponent<PlayerInventory>();

            if (_healthFill == null) Debug.LogWarning("[GameHUD] _healthFill is not wired.");
            if (_staminaFill == null) Debug.LogWarning("[GameHUD] _staminaFill is not wired — regenerate the GameHUD prefab via Tools > EverRealm > Generate HUD Prefabs.");

            if (_controller != null)
                _controller.OnInteractPromptChanged += UpdateInteractPrompt;

            if (_inventory != null)
                _inventory.OnPickedUp += ShowLootNotification;

            // Initialize prompt hidden.
            if (_interactPrompt != null)
                _interactPrompt.gameObject.SetActive(false);

            // Initialize loot notification hidden.
            if (_lootNotification != null)
                SetTextAlpha(_lootNotification, 0f);

            // Initialize extraction hidden.
            if (_extractionGroup != null)
                _extractionGroup.alpha = 0f;
        }

        private void Update()
        {
            var rm = RunManager.Instance;
            if (rm == null) return;

            // --- Health & Stamina (polled for robustness) ---
            if (_combat != null)
            {
                SetFillByAnchor(_healthFill, _combat.Health, _combat.MaxHealth);
                SetFillByAnchor(_staminaFill, _combat.Stamina, _combat.MaxStamina);
            }

            // --- Run timer ---
            if (_runTimerText != null)
            {
                float elapsed = rm.ElapsedTime;
                int minutes = Mathf.FloorToInt(elapsed / 60f);
                int seconds = Mathf.FloorToInt(elapsed % 60f);
                _runTimerText.text = $"{minutes:00}:{seconds:00}";
            }

            // --- Kill counter ---
            if (_killCountText != null)
                _killCountText.text = $"Kills: {rm.KillCount}";

            // --- Extraction countdown ---
            UpdateExtraction();

            // --- Loot notification fade ---
            if (_lootTimer > 0f)
            {
                _lootTimer -= Time.deltaTime;

                if (_lootTimer <= 0f)
                {
                    SetTextAlpha(_lootNotification, 0f);
                }
                else if (_lootTimer < _lootFadeDuration)
                {
                    float alpha = _lootTimer / _lootFadeDuration;
                    SetTextAlpha(_lootNotification, alpha);
                }
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
                _controller.OnInteractPromptChanged -= UpdateInteractPrompt;

            if (_inventory != null)
                _inventory.OnPickedUp -= ShowLootNotification;
        }

        // ---------------------------------------------------------------------
        // Extraction

        private void UpdateExtraction()
        {
            // Auto-detect when an ExtractionZone appears.
            if (_activeZone == null)
            {
                _activeZone = FindObjectOfType<ExtractionZone>();
                if (_activeZone == null)
                {
                    if (_extractionGroup != null && _extractionGroup.alpha > 0f)
                        _extractionGroup.alpha = 0f;
                    return;
                }
            }

            // Zone was destroyed (run ended).
            if (_activeZone == null) return;

            if (_extractionGroup != null)
                _extractionGroup.alpha = 1f;

            float progress = _activeZone.Progress;

            SetFillByAnchor(_extractionFill, progress, 1f);

            if (_extractionText != null)
            {
                if (progress >= 1f)
                {
                    _extractionText.text = "Extracted!";
                }
                else
                {
                    float remaining = (1f - progress) * 8f; // 8s total duration
                    _extractionText.text = _activeZone.PlayerInRange
                        ? $"Extracting... {remaining:F1}s"
                        : "Return to extraction zone!";
                }
            }
        }

        // ---------------------------------------------------------------------
        // Event handlers

        private void UpdateInteractPrompt(string prompt)
        {
            if (_interactPrompt == null) return;

            bool hasPrompt = !string.IsNullOrEmpty(prompt);
            _interactPrompt.gameObject.SetActive(hasPrompt);

            if (hasPrompt)
                _interactPrompt.text = $"[E] {prompt}";
        }

        private void ShowLootNotification(ItemStack stack)
        {
            if (_lootNotification == null) return;

            string name = stack.Definition != null ? stack.Definition.DisplayName : "???";
            string countStr = stack.Count > 1 ? $" x{stack.Count}" : "";
            _lootNotification.text = $"+ {name}{countStr}";
            SetTextAlpha(_lootNotification, 1f);
            _lootTimer = _lootDisplayDuration + _lootFadeDuration;
        }

        // ---------------------------------------------------------------------

        /// <summary>
        /// Controls fill width by adjusting the Image's right anchor.
        /// More reliable than Image.fillAmount which requires Image.Type.Filled
        /// to serialize correctly through prefab generation.
        /// </summary>
        private static void SetFillByAnchor(Image img, float current, float max)
        {
            if (img == null) return;
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            var rt = img.rectTransform;
            rt.anchorMax = new Vector2(ratio, rt.anchorMax.y);
        }

        private static void SetTextAlpha(TMP_Text text, float alpha)
        {
            if (text == null) return;
            Color c = text.color;
            c.a = alpha;
            text.color = c;
        }
    }
}
