using System;
using UnityEngine;
using UnityEngine.InputSystem;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.Items;
using EverRealm.Exiles.UI;
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
        private float   _snapSettleTimer; // Seconds to settle after snap before allowing player movement.
        private float   _lastTerrainY;    // Y of the terrain at snap — used for fall-through recovery.
        private float   _footstepTimer;   // Counts down between footstep sounds.

        // Interaction detection
        private string _currentInteractPrompt = string.Empty;
        public string CurrentInteractPrompt => _currentInteractPrompt;

        /// <summary>Fires when the interaction prompt changes (including to empty).</summary>
        public event Action<string> OnInteractPromptChanged;

        // Inventory UI (cached on first toggle)
        private InventoryUI _inventoryUI;

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

            // After snapping, hold the player on the surface for a short time
            // while the CC establishes ground contact via tiny downward moves.
            if (_snapSettleTimer > 0f)
            {
                _snapSettleTimer -= Time.deltaTime;
                // Push the CC gently into the ground each frame so isGrounded becomes true.
                _cc.Move(Vector3.down * 0.01f);
                _mover.ResetVerticalVelocity();
                return;
            }

            // Safety net: if the player falls well below the terrain they spawned on,
            // they clipped through a mesh collider. Re-snap.
            if (transform.position.y < _lastTerrainY - 5f)
            {
                Debug.LogWarning($"[PlayerController] Fell through terrain (y={transform.position.y:F1}, terrain={_lastTerrainY:F1}). Re-snapping.");
                _cc.enabled = false;
                _snapped = false;
                transform.position = new Vector3(transform.position.x, 500f, transform.position.z);
                return;
            }

            // After a scene transition the first few frames can have huge
            // deltaTime spikes. Clamp so the CharacterController never
            // moves far enough in one tick to clip through terrain.
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            // Use camera yaw so WASD is relative to where the camera faces.
            float yaw = CameraPivot != null ? CameraPivot.eulerAngles.y : transform.eulerAngles.y;

            Vector3 displacement = _mover.ComputeMove(
                _moveInput,
                yaw,
                _sprintHeld,
                _cc.isGrounded,
                dt
            );

            _cc.Move(displacement);

            // Footstep audio — play while grounded and moving.
            if (_cc.isGrounded && _moveInput.sqrMagnitude > 0.01f)
            {
                _footstepTimer -= dt;
                if (_footstepTimer <= 0f)
                {
                    _footstepTimer = _sprintHeld ? 0.28f : 0.40f;
                    Core.AudioManager.Instance?.PlayFootstep(transform.position);
                }
            }
            else
            {
                _footstepTimer = 0f; // Reset so next step plays immediately.
            }

            DetectInteractable();
        }

        // -------------------------------------------------------------------------
        // Interaction detection

        /// <summary>
        /// Per-frame raycast to detect what the player is aiming at,
        /// updating the interaction prompt for the HUD.
        /// </summary>
        private void DetectInteractable()
        {
            string prompt = string.Empty;

            if (TryRaycastInteractable(out RaycastHit hit))
            {
                // Check block entity first (chest, extraction point, etc.).
                var bem = BlockEntityManager.Instance;
                if (bem != null)
                {
                    Vector3Int blockPos = WorldManager.HitToBlockPos(hit);
                    if (bem.TryGet(blockPos, out var blockEntity))
                    {
                        prompt = blockEntity.InteractPrompt;
                    }
                }

                // Fall back to GameObject-based interactables.
                if (string.IsNullOrEmpty(prompt))
                {
                    var interactable = hit.collider.GetComponentInParent<IInteractable>();
                    if (interactable != null)
                        prompt = interactable.InteractPrompt;
                }
            }

            if (_currentInteractPrompt != prompt)
            {
                _currentInteractPrompt = prompt;
                OnInteractPromptChanged?.Invoke(prompt);
            }
        }

        /// <summary>Shared raycast used by both interaction detection and input handling.</summary>
        private bool TryRaycastInteractable(out RaycastHit hit)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                hit = default;
                return false;
            }

            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 dir = cam.transform.forward;
            return Physics.Raycast(origin, dir, out hit, _interactRange, ~0, QueryTriggerInteraction.Collide);
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
                // Place the CC so its bottom sits exactly on the surface.
                // ccBottom = local Y from transform.position to the CC's feet.
                float ccBottom = _cc.center.y - _cc.height * 0.5f;
                Vector3 spawnPos = hit.point - Vector3.up * ccBottom;

                transform.position = spawnPos;
                _lastTerrainY = hit.point.y;

                Physics.SyncTransforms();

                _mover.ResetVerticalVelocity();

                _cc.enabled = true;
                _snapped = true;

                // Settle period: the CC will receive tiny downward moves each
                // frame to establish isGrounded before real movement begins.
                _snapSettleTimer = 0.25f;

                Debug.Log($"[PlayerController] Snapped to {spawnPos} (terrain hit y={hit.point.y:F1})");
            }
        }

        // -------------------------------------------------------------------------
        // Input System callbacks (wired via Player Input component or manually).

        public void OnMove(InputValue value)    => _moveInput  = value.Get<Vector2>();
        public void OnSprint(InputValue value)  => _sprintHeld = value.isPressed;
        public void OnJump(InputValue value)    { if (value.isPressed) _mover.RequestJump(); }
        public void OnLook(InputValue value)
        {
            // Suppress look input while inventory is open.
            if (_inventoryUI != null && _inventoryUI.IsOpen) return;
            if (_playerCamera != null) _playerCamera.ApplyLookDelta(value.Get<Vector2>());
        }

        public void OnInteract(InputValue value)
        {
            if (!value.isPressed) return;

            if (!TryRaycastInteractable(out RaycastHit hit))
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

        public void OnInventory(InputValue value)
        {
            if (!value.isPressed) return;

            if (_inventoryUI == null)
                _inventoryUI = FindFirstObjectByType<InventoryUI>();

            if (_inventoryUI != null)
                _inventoryUI.Toggle();
        }
    }
}
