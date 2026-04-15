using UnityEngine;
using UnityEngine.InputSystem;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.Items;
using EverRealm.Exiles.World;

namespace EverRealm.Exiles.Player
{
    /// <summary>
    /// Thin MonoBehaviour wiring layer.
    ///
    /// Reads Unity Input System callbacks, forwards to <see cref="PlayerMover"/>,
    /// and drives the <see cref="CharacterController"/>.
    ///
    /// Setup:
    ///   1. Add to the player GameObject alongside a CharacterController.
    ///   2. Assign _stats (PlayerStats asset) in the Inspector.
    ///   3. Assign _cameraTransform to the child camera pivot (set by PlayerCamera).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerStats _stats;

        [Header("Interaction")]
        [SerializeField] private float _interactRange = 4f;

        // Assigned by PlayerCamera on Awake so neither component needs a manual reference.
        [HideInInspector] public Transform CameraPivot;

        private PlayerCamera _playerCamera;

        private CharacterController _cc;
        private PlayerMover _mover;

        // Input state — written by Input System callbacks, read in Update.
        /// <summary>Current WASD input vector (raw, before yaw rotation).</summary>
        public Vector2 MoveInput => _moveInput;

        private Vector2 _moveInput;
        private bool    _sprintHeld;
        private bool    _snapped; // True once the player has been placed on terrain.

        // -------------------------------------------------------------------------

        private void Awake()
        {
            _cc           = GetComponent<CharacterController>();
            _mover        = new PlayerMover(_stats);
            _playerCamera = GetComponentInChildren<PlayerCamera>();

            // Share the mover with PlayerCombat so dodge state is accessible.
            var combat = GetComponent<PlayerCombat>();
            if (combat != null) combat.SetMover(_mover);

            // Park player high above the world so they don't interact with
            // terrain while chunks are still generating.
            _cc.enabled = false;
            transform.position = new Vector3(transform.position.x, 500f, transform.position.z);

            gameObject.tag = "Player";
        }

        private void Update()
        {
            if (!_snapped)
            {
                TrySnapToSurface();
                return;
            }

            // Use camera yaw so WASD is relative to where the camera faces.
            float yaw = CameraPivot != null ? CameraPivot.eulerAngles.y : transform.eulerAngles.y;

            Vector3 displacement = _mover.ComputeMove(
                _moveInput,
                yaw,
                _sprintHeld,
                _cc.isGrounded,
                Time.deltaTime
            );

            _cc.Move(displacement);
        }

        // -------------------------------------------------------------------------
        // Spawn helpers

        /// <summary>
        /// Raycast straight down from a high point to find the terrain surface.
        /// Retries each frame until a chunk collider exists beneath the player.
        /// </summary>
        private void TrySnapToSurface()
        {
            const float castHeight = 500f;

            Vector3 origin = new Vector3(transform.position.x, castHeight, transform.position.z);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, castHeight * 2f))
            {
                // Place player 3m above the surface — gravity will do the rest.
                float ccBottom = _cc.center.y - _cc.height * 0.5f;
                Vector3 spawnPos = hit.point + Vector3.up * (3f - ccBottom);

                transform.position = spawnPos;
                _cc.enabled = true;
                _snapped = true;
                Debug.Log($"[PlayerController] Snapped to {spawnPos} (terrain hit: {hit.point}, CC bottom offset: {ccBottom})");
            }
        }

        // -------------------------------------------------------------------------
        // Input System callbacks (wired via Player Input component or manually).

        public void OnMove(InputValue value)    => _moveInput  = value.Get<Vector2>();
        public void OnSprint(InputValue value)  => _sprintHeld = value.isPressed;
        public void OnJump(InputValue value)    { if (value.isPressed) _mover.RequestJump(); }
        public void OnLook(InputValue value)    { if (_playerCamera != null) _playerCamera.ApplyLookDelta(value.Get<Vector2>()); }

        public void OnInteract(InputValue value)
        {
            if (!value.isPressed) return;

            var cam = Camera.main;
            if (cam == null) return;

            // Start from player center, aim in camera direction.
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 dir = cam.transform.forward;

            if (!Physics.Raycast(origin, dir, out RaycastHit hit, _interactRange, ~0, QueryTriggerInteraction.Collide))
                return;

            // 1. Check for a block entity (chest, etc.) at the hit position.
            var bem = BlockEntityManager.Instance;
            if (bem != null)
            {
                Vector3Int blockPos = WorldManager.HitToBlockPos(hit);
                if (bem.TryGet(blockPos, out var blockEntity))
                {
                    blockEntity.OnInteract(this);
                    return;
                }
            }

            // 2. Fall back to GameObject-based interactables (loot pickups, etc.).
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            interactable?.Interact(this);
        }
    }
}
