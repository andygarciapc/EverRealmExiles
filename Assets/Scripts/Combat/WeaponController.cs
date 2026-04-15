using System.Collections.Generic;
using UnityEngine;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.Combat
{
    /// <summary>
    /// Drives the swing state machine for a weapon:
    ///   Idle → Windup → Active → Recovery → Idle
    ///
    /// During the Active window a box overlap test is performed each frame.
    /// Anything with <see cref="IDamageable"/> is hit once per swing.
    /// </summary>
    public sealed class WeaponController : MonoBehaviour
    {
        public enum SwingState { Idle, Windup, Active, Recovery }

        public SwingState State { get; private set; } = SwingState.Idle;
        public bool IsBusy => State != SwingState.Idle;

        private WeaponDefinition _weapon;
        private float _stateTimer;
        private float _currentDamage;
        private float _currentWindup;
        private float _currentActive;
        private float _currentRecovery;

        // Track what we've already hit this swing to avoid multi-hits.
        private readonly HashSet<IDamageable> _hitThisSwing = new();

        // Reusable buffer for Physics.OverlapBoxNonAlloc.
        private readonly Collider[] _overlapBuffer = new Collider[16];

        public void Equip(WeaponDefinition weapon) => _weapon = weapon;

        /// <summary>Start a light or heavy swing. Returns false if busy or no weapon.</summary>
        public bool StartSwing(bool heavy)
        {
            if (IsBusy || _weapon == null) return false;

            _currentDamage   = heavy ? _weapon.HeavyDamage   : _weapon.LightDamage;
            _currentWindup   = heavy ? _weapon.HeavyWindup   : _weapon.LightWindup;
            _currentActive   = heavy ? _weapon.HeavyActive   : _weapon.LightActive;
            _currentRecovery = heavy ? _weapon.HeavyRecovery : _weapon.LightRecovery;

            State       = SwingState.Windup;
            _stateTimer = _currentWindup;
            _hitThisSwing.Clear();
            return true;
        }

        private void Update()
        {
            if (State == SwingState.Idle) return;

            _stateTimer -= Time.deltaTime;

            if (_stateTimer <= 0f)
            {
                switch (State)
                {
                    case SwingState.Windup:
                        State       = SwingState.Active;
                        _stateTimer = _currentActive;
                        Core.AudioManager.Instance?.PlaySwordSwing();
                        break;
                    case SwingState.Active:
                        State       = SwingState.Recovery;
                        _stateTimer = _currentRecovery;
                        break;
                    case SwingState.Recovery:
                        State = SwingState.Idle;
                        break;
                }
            }

            if (State == SwingState.Active)
                PerformOverlap();
        }

        private void PerformOverlap()
        {
            Vector3 center = transform.position
                           + Vector3.up * 1f // chest height
                           + transform.forward * _weapon.HitboxForwardOffset;

            int count = Physics.OverlapBoxNonAlloc(
                center,
                _weapon.HitboxHalfExtents,
                _overlapBuffer,
                transform.rotation
            );

            for (int i = 0; i < count; i++)
            {
                // Skip self.
                if (_overlapBuffer[i].transform.root == transform) continue;

                var damageable = _overlapBuffer[i].GetComponentInParent<IDamageable>();
                if (damageable == null || !_hitThisSwing.Add(damageable)) continue;

                Vector3 hitPoint = _overlapBuffer[i].ClosestPoint(center);
                Vector3 knockDir = (_overlapBuffer[i].transform.position - transform.position).normalized;

                var info = new DamageInfo(
                    _currentDamage,
                    hitPoint,
                    knockDir,
                    _weapon.KnockbackForce,
                    gameObject
                );
                damageable.TakeDamage(info);
                CombatFeedback.Instance?.OnPlayerDealtDamage(info);
                Core.AudioManager.Instance?.PlayHitImpact(hitPoint);
            }
        }
    }
}
