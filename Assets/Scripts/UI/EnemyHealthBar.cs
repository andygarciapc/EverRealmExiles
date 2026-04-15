using UnityEngine;
using UnityEngine.UI;
using EverRealm.Exiles.AI;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// World-space health bar that floats above an enemy.
    /// Hidden at full health, fades in on damage, fades out after a delay.
    /// </summary>
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Behavior")]
        [SerializeField] private float _showDuration = 3f;
        [SerializeField] private float _fadeDuration = 0.5f;
        [SerializeField] private float _heightOffset = 2.2f;

        private EnemyHealth _enemyHealth;
        private Transform _enemyTransform;
        private float _showTimer;

        // -----------------------------------------------------------------

        /// <summary>Bind to an enemy. Call once after instantiation.</summary>
        public void Init(EnemyHealth health, Transform enemyTransform)
        {
            _enemyHealth = health;
            _enemyTransform = enemyTransform;
            _enemyHealth.OnHealthChanged += OnHealthChanged;

            // Start hidden (full health).
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        private void LateUpdate()
        {
            // Follow enemy position.
            if (_enemyTransform != null)
                transform.position = _enemyTransform.position + Vector3.up * _heightOffset;

            // Billboard: face camera.
            var cam = Camera.main;
            if (cam != null)
                transform.rotation = cam.transform.rotation;

            // Fade timer.
            if (_showTimer > 0f)
            {
                _showTimer -= Time.deltaTime;

                if (_showTimer <= 0f)
                {
                    if (_canvasGroup != null)
                        _canvasGroup.alpha = 0f;
                }
                else if (_showTimer < _fadeDuration)
                {
                    if (_canvasGroup != null)
                        _canvasGroup.alpha = _showTimer / _fadeDuration;
                }
            }
        }

        private void OnDestroy()
        {
            if (_enemyHealth != null)
                _enemyHealth.OnHealthChanged -= OnHealthChanged;
        }

        // -----------------------------------------------------------------

        private void OnHealthChanged(float current, float max)
        {
            if (_fill != null)
            {
                float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
                var rt = _fill.rectTransform;
                rt.anchorMax = new Vector2(ratio, rt.anchorMax.y);
            }

            if (current <= 0f)
            {
                // Enemy died — fade out and self-destruct.
                _showTimer = _fadeDuration;
                return;
            }

            // Show the bar on damage.
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            _showTimer = _showDuration + _fadeDuration;
        }
    }
}
