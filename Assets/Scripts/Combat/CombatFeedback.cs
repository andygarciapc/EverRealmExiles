using UnityEngine;

namespace EverRealm.Exiles.Combat
{
    /// <summary>
    /// Singleton managing combat feel effects: screen shake, hit pause (hitstop),
    /// and hit VFX. Attach to a persistent GameObject in the Game scene or
    /// instantiate via the Phase11AssetGenerator.
    /// </summary>
    public sealed class CombatFeedback : MonoBehaviour
    {
        public static CombatFeedback Instance { get; private set; }

        [Header("Screen Shake")]
        [SerializeField] private float _defaultShakeIntensity = 0.15f;
        [SerializeField] private float _defaultShakeDuration  = 0.12f;
        [SerializeField] private float _heavyShakeIntensity   = 0.30f;
        [SerializeField] private float _heavyShakeDuration    = 0.20f;

        [Header("Hit Pause")]
        [SerializeField] private float _hitPauseDuration = 0.05f;
        [SerializeField] private float _hitPauseScale    = 0.1f;

        [Header("VFX")]
        [SerializeField] private GameObject _hitVFXPrefab;

        /// <summary>
        /// Current screen shake offset. Read by PlayerCamera each frame.
        /// </summary>
        public Vector3 ShakeOffset { get; private set; }

        private float _shakeTimer;
        private float _shakeDuration;
        private float _shakeIntensity;

        private float _pauseTimer;
        private float _preTimeScale = 1f;

        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            UpdateShake();
            UpdateHitPause();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // -------------------------------------------------------------------------
        // Screen Shake

        /// <summary>Start a screen shake with configurable intensity and duration.</summary>
        public void TriggerShake(float intensity, float duration)
        {
            // Stronger shake overrides weaker in-progress shake.
            if (intensity > _shakeIntensity || _shakeTimer <= 0f)
            {
                _shakeIntensity = intensity;
                _shakeDuration  = duration;
                _shakeTimer     = duration;
            }
        }

        private void UpdateShake()
        {
            if (_shakeTimer <= 0f)
            {
                ShakeOffset = Vector3.zero;
                return;
            }

            // Use unscaled delta so shake works during hit pause.
            _shakeTimer -= Time.unscaledDeltaTime;
            float decay = Mathf.Clamp01(_shakeTimer / _shakeDuration);
            float currentIntensity = _shakeIntensity * decay;

            // Perlin noise for smooth random offset.
            float t = Time.unscaledTime * 25f;
            float offsetX = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * currentIntensity;
            float offsetY = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * currentIntensity;

            ShakeOffset = new Vector3(offsetX, offsetY, 0f);
        }

        // -------------------------------------------------------------------------
        // Hit Pause (Hitstop)

        /// <summary>Brief time-scale reduction for impact weight.</summary>
        public void TriggerHitPause(float duration, float scale)
        {
            if (_pauseTimer > 0f) return; // Don't stack pauses.

            _preTimeScale = Time.timeScale;
            Time.timeScale = scale;
            _pauseTimer = duration;
        }

        private void UpdateHitPause()
        {
            if (_pauseTimer <= 0f) return;

            _pauseTimer -= Time.unscaledDeltaTime;
            if (_pauseTimer <= 0f)
            {
                Time.timeScale = _preTimeScale;
                _pauseTimer = 0f;
            }
        }

        // -------------------------------------------------------------------------
        // Hit VFX

        /// <summary>Spawn impact particles at the given world position.</summary>
        public void SpawnHitVFX(Vector3 position, Vector3 normal)
        {
            if (_hitVFXPrefab == null) return;

            var rot = normal.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(normal)
                : Quaternion.identity;

            var go = Instantiate(_hitVFXPrefab, position, rot);
            Destroy(go, 1f);
        }

        // -------------------------------------------------------------------------
        // Convenience methods — called by combat systems

        /// <summary>Player weapon hit an enemy: shake + pause + VFX.</summary>
        public void OnPlayerDealtDamage(DamageInfo info)
        {
            TriggerShake(_defaultShakeIntensity, _defaultShakeDuration);
            TriggerHitPause(_hitPauseDuration, _hitPauseScale);
            SpawnHitVFX(info.HitPoint, info.KnockbackDir);
        }

        /// <summary>Player took damage: stronger shake, no pause.</summary>
        public void OnPlayerTookDamage(DamageInfo info)
        {
            TriggerShake(_heavyShakeIntensity, _heavyShakeDuration);
            SpawnHitVFX(info.HitPoint, -info.KnockbackDir);
        }
    }
}
