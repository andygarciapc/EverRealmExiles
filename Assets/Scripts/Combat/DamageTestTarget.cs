using UnityEngine;

namespace EverRealm.Exiles.Combat
{
    /// <summary>
    /// Drop-in test target for verifying combat.
    /// Create a Cube, add a BoxCollider, and add this component.
    /// Logs damage to the Console and changes colour on hit.
    /// </summary>
    public sealed class DamageTestTarget : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 100f;

        private float    _health;
        private Renderer _renderer;
        private Color    _originalColor;

        private void Awake()
        {
            _health   = _maxHealth;
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
                _originalColor = _renderer.material.color;
        }

        public void TakeDamage(DamageInfo info)
        {
            _health -= info.Amount;
            Debug.Log($"[TestTarget] Hit for {info.Amount}, health: {_health}/{_maxHealth}");

            if (_renderer != null)
                _renderer.material.color = Color.red;

            // Flash back after a short delay.
            Invoke(nameof(ResetColor), 0.15f);

            if (_health <= 0f)
            {
                Debug.Log("[TestTarget] Destroyed!");
                Destroy(gameObject);
            }
        }

        private void ResetColor()
        {
            if (_renderer != null)
                _renderer.material.color = _originalColor;
        }
    }
}
