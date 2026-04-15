using UnityEngine;
using EverRealm.Exiles.Combat;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.AI
{
    /// <summary>
    /// Drives a simple Windup → Active → Recovery attack cycle for an enemy.
    /// During the Active window, a sphere overlap checks for <see cref="IDamageable"/> targets.
    /// </summary>
    public sealed class EnemyAttack : MonoBehaviour
    {
        public enum AttackPhase { Idle, Windup, Active, Recovery }

        public AttackPhase Phase { get; private set; } = AttackPhase.Idle;
        public bool IsBusy => Phase != AttackPhase.Idle;

        private EnemyDefinition _def;
        private float _timer;
        private bool _hitThisSwing;

        private readonly Collider[] _overlapBuffer = new Collider[8];

        public void Init(EnemyDefinition def) => _def = def;

        public bool StartAttack()
        {
            if (IsBusy || _def == null) return false;
            Phase = AttackPhase.Windup;
            _timer = _def.AttackWindup;
            _hitThisSwing = false;
            return true;
        }

        private void Update()
        {
            if (Phase == AttackPhase.Idle) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                switch (Phase)
                {
                    case AttackPhase.Windup:
                        Phase  = AttackPhase.Active;
                        _timer = _def.AttackActive;
                        break;
                    case AttackPhase.Active:
                        Phase  = AttackPhase.Recovery;
                        _timer = _def.AttackRecovery;
                        break;
                    case AttackPhase.Recovery:
                        Phase = AttackPhase.Idle;
                        break;
                }
            }

            if (Phase == AttackPhase.Active && !_hitThisSwing)
                PerformOverlap();
        }

        private void PerformOverlap()
        {
            Vector3 center = transform.position + Vector3.up * 1f + transform.forward * _def.AttackRange * 0.6f;
            float radius = _def.AttackRange * 0.5f;

            int count = Physics.OverlapSphereNonAlloc(center, radius, _overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                if (_overlapBuffer[i].transform.root == transform) continue;

                var damageable = _overlapBuffer[i].GetComponentInParent<IDamageable>();
                if (damageable == null) continue;

                Vector3 hitPoint = _overlapBuffer[i].ClosestPoint(center);
                Vector3 knockDir = (_overlapBuffer[i].transform.position - transform.position).normalized;

                damageable.TakeDamage(new DamageInfo(
                    _def.AttackDamage,
                    hitPoint,
                    knockDir,
                    _def.KnockbackForce,
                    gameObject
                ));

                _hitThisSwing = true;
                break; // One target per swing.
            }
        }

        /// <summary>Immediately cancel the current attack (used by stagger).</summary>
        public void Cancel() => Phase = AttackPhase.Idle;
    }
}
