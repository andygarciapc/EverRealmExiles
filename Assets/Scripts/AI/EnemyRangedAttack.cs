using UnityEngine;
using EverRealm.Exiles.Combat;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.AI
{
    /// <summary>
    /// Ranged attack component for enemies. Same phase cycle as <see cref="EnemyAttack"/>
    /// (Idle → Windup → Active → Recovery) but spawns a <see cref="Projectile"/>
    /// during the Active window instead of performing a melee overlap.
    /// </summary>
    public sealed class EnemyRangedAttack : MonoBehaviour, IEnemyAttack
    {
        public enum AttackPhase { Idle, Windup, Active, Recovery }

        public AttackPhase Phase { get; private set; } = AttackPhase.Idle;
        public bool IsBusy => Phase != AttackPhase.Idle;

        [SerializeField] private GameObject _projectilePrefab;

        private EnemyDefinition _def;
        private float _timer;
        private bool _firedThisAttack;
        private Transform _player;

        public void Init(EnemyDefinition def, GameObject projectilePrefab)
        {
            _def = def;
            if (projectilePrefab != null)
                _projectilePrefab = projectilePrefab;
        }

        public bool StartAttack()
        {
            if (IsBusy || _def == null) return false;
            Phase = AttackPhase.Windup;
            _timer = _def.AttackWindup;
            _firedThisAttack = false;

            // Cache player reference for aiming.
            if (_player == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) _player = playerObj.transform;
            }

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
                        Phase = AttackPhase.Active;
                        _timer = _def.AttackActive;
                        break;
                    case AttackPhase.Active:
                        Phase = AttackPhase.Recovery;
                        _timer = _def.AttackRecovery;
                        break;
                    case AttackPhase.Recovery:
                        Phase = AttackPhase.Idle;
                        break;
                }
            }

            if (Phase == AttackPhase.Active && !_firedThisAttack)
                FireProjectile();
        }

        private void FireProjectile()
        {
            _firedThisAttack = true;

            if (_projectilePrefab == null || _player == null) return;

            // Spawn at chest height, aimed at the player's center mass.
            Vector3 spawnPos = transform.position + Vector3.up * 1.2f + transform.forward * 0.5f;
            Vector3 targetPos = _player.position + Vector3.up * 1f;
            Vector3 direction = (targetPos - spawnPos).normalized;

            var go = Instantiate(_projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
            var proj = go.GetComponent<Projectile>();
            if (proj != null)
                proj.Init(_def.ProjectileSpeed, _def.AttackDamage, _def.KnockbackForce, gameObject);
        }

        public void Cancel() => Phase = AttackPhase.Idle;
    }
}
