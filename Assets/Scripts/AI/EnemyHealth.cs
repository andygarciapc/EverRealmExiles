using UnityEngine;
using EverRealm.Exiles.Combat;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.AI
{
    /// <summary>
    /// Manages enemy health, hit reactions, and death.
    /// Notifies <see cref="EnemyController"/> when stagger or death should occur.
    /// </summary>
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        private EnemyDefinition _def;
        private EnemyController _controller;
        private float _health;
        private Renderer _renderer;
        private Color _originalColor;

        public float Health    => _health;
        public float MaxHealth => _def != null ? _def.MaxHealth : 0f;
        public bool  IsDead    => _health <= 0f;

        public void Init(EnemyDefinition def, EnemyController controller)
        {
            _def        = def;
            _controller = controller;
            _health     = def.MaxHealth;

            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null)
                _originalColor = _renderer.material.color;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;

            _health -= info.Amount;
            Debug.Log($"[{_def.DisplayName}] Took {info.Amount} damage, health: {_health}/{_def.MaxHealth}");

            // Flash red.
            if (_renderer != null)
            {
                _renderer.material.color = Color.red;
                Invoke(nameof(ResetColor), 0.12f);
            }

            if (_health <= 0f)
            {
                _health = 0f;
                _controller.OnDeath();
            }
            else if (info.Amount >= _def.StaggerThreshold)
            {
                _controller.OnStagger();
            }
        }

        private void ResetColor()
        {
            if (_renderer != null)
                _renderer.material.color = _originalColor;
        }
    }
}
