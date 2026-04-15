using UnityEngine;
using UnityEngine.InputSystem;
using EverRealm.Exiles.Combat;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.Player
{
    /// <summary>
    /// Reads combat input (attack, dodge) and delegates to
    /// <see cref="WeaponController"/> and <see cref="PlayerMover"/>.
    ///
    /// Also owns stamina state and the <see cref="IDamageable"/> implementation
    /// for the player.
    ///
    /// Setup: add to the Player GameObject alongside PlayerController.
    /// Assign _weapon (WeaponDefinition asset) and _stats (PlayerStats asset).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(WeaponController))]
    public sealed class PlayerCombat : MonoBehaviour, IDamageable
    {
        [SerializeField] private PlayerStats      _stats;
        [SerializeField] private WeaponDefinition _weapon;

        [Header("Health")]
        [SerializeField] private float _maxHealth = 100f;

        private PlayerController _controller;
        private WeaponController _weaponCtrl;
        private PlayerMover      _mover;

        private float _health;
        private float _stamina;
        private float _staminaRegenCooldown;

        public float Health       => _health;
        public float MaxHealth    => _maxHealth;
        public float Stamina      => _stamina;
        public float MaxStamina   => _stats.MaxStamina;

        // -------------------------------------------------------------------------

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _weaponCtrl = GetComponent<WeaponController>();
        }

        private void Start()
        {
            _health  = _maxHealth;
            _stamina = _stats.MaxStamina;

            if (_weapon != null)
                _weaponCtrl.Equip(_weapon);
        }

        private void Update()
        {
            // Stamina regen
            if (_staminaRegenCooldown > 0f)
                _staminaRegenCooldown -= Time.deltaTime;
            else
                _stamina = Mathf.Min(_stamina + _stats.StaminaRegen * Time.deltaTime, _stats.MaxStamina);
        }

        // -------------------------------------------------------------------------
        // IDamageable

        public void TakeDamage(DamageInfo info)
        {
            // Dodge i-frames
            if (_mover != null && _mover.HasIFrames) return;

            _health -= info.Amount;
            Debug.Log($"[Player] Took {info.Amount} damage, health: {_health}/{_maxHealth}");

            if (_health <= 0f)
            {
                _health = 0f;
                Debug.Log("[Player] Died!");
                RunManager.Instance?.EndRun(false);
            }
        }

        // -------------------------------------------------------------------------
        // Input System callbacks

        public void OnAttack(InputValue value)
        {
            if (!value.isPressed) return;
            if (_weaponCtrl.IsBusy) return;

            float cost = _weapon.LightStamina;
            if (_stamina < cost) return;

            if (_weaponCtrl.StartSwing(false))
                SpendStamina(cost);
        }

        public void OnDodge(InputValue value)
        {
            if (!value.isPressed) return;
            if (_mover == null) return;
            if (_mover.IsDodging) return;
            if (_stamina < _stats.DodgeStaminaCost) return;

            // Dodge in the direction the player is pressing, or forward if idle.
            Vector2 input = _controller.MoveInput;
            Vector3 dir;

            if (input.sqrMagnitude > 0.01f)
            {
                float yaw = _controller.CameraPivot != null
                    ? _controller.CameraPivot.eulerAngles.y
                    : transform.eulerAngles.y;
                dir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
            }
            else
            {
                dir = transform.forward;
            }
            dir.y = 0f;

            if (_mover.RequestDodge(dir))
                SpendStamina(_stats.DodgeStaminaCost);
        }

        // -------------------------------------------------------------------------

        /// <summary>
        /// Called by <see cref="PlayerController"/> after it creates the mover
        /// so combat can access dodge state.
        /// </summary>
        public void SetMover(PlayerMover mover) => _mover = mover;

        private void SpendStamina(float amount)
        {
            _stamina = Mathf.Max(0f, _stamina - amount);
            _staminaRegenCooldown = _stats.StaminaRegenDelay;
        }
    }
}
