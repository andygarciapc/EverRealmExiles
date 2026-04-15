using UnityEngine;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.Items;
using EverRealm.Exiles.UI;

namespace EverRealm.Exiles.AI
{
    /// <summary>
    /// Top-level enemy state machine:
    ///   Patrol → Chase → Attack → (Stagger) → Dead
    ///
    /// Uses voxel A* pathfinding (no NavMesh required).
    ///
    /// Setup:
    ///   1. Create a capsule GameObject, add CharacterController.
    ///   2. Add this component, assign _definition (EnemyDefinition asset).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyAttack))]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyController : MonoBehaviour
    {
        public enum State { Patrol, Chase, Attack, Stagger, Dead }

        [SerializeField] private EnemyDefinition _definition;

        [Header("Loot")]
        [SerializeField] private GameObject _lootPickupPrefab;

        [Header("UI")]
        [SerializeField] private GameObject _healthBarPrefab;

        public State CurrentState { get; private set; } = State.Patrol;

        private EnemyMover  _mover;
        private EnemyAttack _attack;
        private EnemyHealth _health;
        private Transform   _player;

        private float _patrolWaitTimer;
        private float _staggerTimer;
        private float _repathTimer;
        private float _playerRetryTimer;
        private Vector3 _spawnPoint;

        private const float RepathInterval = 0.5f; // Re-pathfind every N seconds while chasing.

        // -------------------------------------------------------------------------

        private void Awake()
        {
            var cc = GetComponent<CharacterController>();
            _mover  = new EnemyMover(transform, cc);
            _attack = GetComponent<EnemyAttack>();
            _health = GetComponent<EnemyHealth>();

            _attack.Init(_definition);
            _health.Init(_definition, this);

            // Spawn world-space health bar.
            if (_healthBarPrefab != null)
            {
                var barGo = Instantiate(_healthBarPrefab);
                var bar = barGo.GetComponent<EnemyHealthBar>();
                if (bar != null)
                    bar.Init(_health, transform);
            }

            _mover.SetSpeed(_definition.MoveSpeed);
            _spawnPoint = transform.position;
        }

        private void Start()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                _player = playerObj.transform;
        }

        private void Update()
        {
            if (CurrentState == State.Dead) return;

            // Retry player lookup if the player wasn't found yet (spawn order, late join).
            if (_player == null)
            {
                _playerRetryTimer -= Time.deltaTime;
                if (_playerRetryTimer <= 0f)
                {
                    _playerRetryTimer = 1.5f;
                    var playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null)
                        _player = playerObj.transform;
                }
            }

            // Always tick the mover so gravity applies.
            _mover.Tick(Time.deltaTime);

            switch (CurrentState)
            {
                case State.Patrol:  UpdatePatrol();  break;
                case State.Chase:   UpdateChase();   break;
                case State.Attack:  UpdateAttack();  break;
                case State.Stagger: UpdateStagger(); break;
            }
        }

        // -------------------------------------------------------------------------
        // States

        private void UpdatePatrol()
        {
            if (PlayerInRange(_definition.DetectionRadius))
            {
                TransitionTo(State.Chase);
                return;
            }

            if (_mover.HasReachedDestination)
            {
                _patrolWaitTimer -= Time.deltaTime;
                if (_patrolWaitTimer <= 0f)
                {
                    if (_mover.TryGetRandomPoint(_spawnPoint, _definition.PatrolRadius, out Vector3 dest))
                        _mover.MoveTo(dest);

                    _patrolWaitTimer = Random.Range(_definition.PatrolWaitMin, _definition.PatrolWaitMax);
                }
            }
        }

        private void UpdateChase()
        {
            if (_player == null || !PlayerInRange(_definition.LoseRadius))
            {
                TransitionTo(State.Patrol);
                return;
            }

            if (PlayerInRange(_definition.AttackRange))
            {
                TransitionTo(State.Attack);
                return;
            }

            // Re-pathfind periodically so the enemy tracks the player.
            _repathTimer -= Time.deltaTime;
            if (_repathTimer <= 0f)
            {
                _mover.MoveTo(_player.position);
                _repathTimer = RepathInterval;
            }

            _mover.FaceTarget(_player.position, 8f);
        }

        private void UpdateAttack()
        {
            if (_player != null)
                _mover.FaceTarget(_player.position, 12f);

            if (!_attack.IsBusy)
            {
                if (!PlayerInRange(_definition.AttackRange * 1.2f))
                {
                    TransitionTo(State.Chase);
                    return;
                }
                _attack.StartAttack();
            }
        }

        private void UpdateStagger()
        {
            _staggerTimer -= Time.deltaTime;
            if (_staggerTimer <= 0f)
                TransitionTo(PlayerInRange(_definition.DetectionRadius) ? State.Chase : State.Patrol);
        }

        // -------------------------------------------------------------------------
        // Transitions called by EnemyHealth

        public void OnStagger()
        {
            if (CurrentState == State.Dead) return;
            _attack.Cancel();
            _mover.Stop();
            _staggerTimer = _definition.StaggerDuration;
            CurrentState = State.Stagger;
        }

        public void OnDeath()
        {
            CurrentState = State.Dead;
            _attack.Cancel();
            _mover.Stop();

            Debug.Log($"[{_definition.DisplayName}] Died!");
            RunManager.Instance?.RegisterKill();

            GetComponent<CharacterController>().enabled = false;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Drop loot from the enemy's loot table.
            if (_definition.LootTable != null && _lootPickupPrefab != null)
            {
                var item = _definition.LootTable.Roll(out int count);
                if (item != null)
                    LootPickup.Spawn(item, count, transform.position, _lootPickupPrefab);
            }

            Destroy(gameObject, 3f);
        }

        // -------------------------------------------------------------------------

        private void TransitionTo(State next)
        {
            if (CurrentState == State.Chase || CurrentState == State.Attack)
                _mover.Stop();

            CurrentState = next;

            if (next == State.Patrol)
                _patrolWaitTimer = 0f;
            if (next == State.Chase)
                _repathTimer = 0f; // Path immediately.
        }

        private bool PlayerInRange(float range)
        {
            if (_player == null) return false;
            return Vector3.Distance(transform.position, _player.position) <= range;
        }
    }
}
