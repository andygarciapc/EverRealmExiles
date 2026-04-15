using UnityEngine;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.Player
{
    /// <summary>
    /// Pure C# helper — no MonoBehaviour. Owns velocity state and computes the
    /// per-frame displacement fed into <see cref="CharacterController.Move"/>.
    ///
    /// Keeping movement math here makes it unit-testable and keeps
    /// <see cref="PlayerController"/> a thin wiring layer.
    /// </summary>
    public sealed class PlayerMover
    {
        private readonly PlayerStats _stats;

        // Accumulated vertical velocity (signed, negative = falling).
        private float _verticalVelocity;

        // Tracks time since the player was last grounded for coyote-time logic.
        private float _timeSinceGrounded;

        // True for the frame a jump was requested (consumed on use).
        private bool _jumpQueued;

        // Dodge state
        private float   _dodgeTimer;
        private Vector3 _dodgeDirection;

        /// <summary>True while in a dodge roll.</summary>
        public bool IsDodging => _dodgeTimer > 0f;

        /// <summary>True while invincibility frames are active during a dodge.</summary>
        public bool HasIFrames => _dodgeTimer > (_stats.DodgeDuration - _stats.DodgeIFrames);

        public PlayerMover(PlayerStats stats)
        {
            _stats = stats;
        }

        /// <summary>
        /// Call every frame. Returns the world-space displacement to pass to
        /// <see cref="CharacterController.Move"/> (already scaled by deltaTime).
        /// </summary>
        /// <param name="moveInput">Normalised XZ input from the player (horizontal plane).</param>
        /// <param name="yaw">Current player yaw (degrees) so input is relative to facing.</param>
        /// <param name="isSprinting">Whether sprint is held.</param>
        /// <param name="isGrounded">CharacterController.isGrounded from last frame.</param>
        /// <param name="deltaTime">Time.deltaTime.</param>
        public Vector3 ComputeMove(
            Vector2 moveInput,
            float   yaw,
            bool    isSprinting,
            bool    isGrounded,
            float   deltaTime)
        {
            // --- Coyote time ---
            if (isGrounded)
                _timeSinceGrounded = 0f;
            else
                _timeSinceGrounded += deltaTime;

            bool canJump = _timeSinceGrounded <= _stats.CoyoteTime;

            // --- Vertical / gravity ---
            if (isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f; // Small negative keeps CharacterController grounded.

            if (_jumpQueued && canJump)
            {
                // v² = 2·g·h  →  v = sqrt(2·g·h)
                _verticalVelocity = Mathf.Sqrt(2f * _stats.Gravity * _stats.JumpHeight);
                _timeSinceGrounded = _stats.CoyoteTime + 1f; // Consume coyote window.
            }
            _jumpQueued = false;

            _verticalVelocity -= _stats.Gravity * deltaTime;
            _verticalVelocity  = Mathf.Max(_verticalVelocity, -_stats.MaxFallSpeed);

            // --- Dodge ---
            if (_dodgeTimer > 0f)
            {
                _dodgeTimer -= deltaTime;
                Vector3 dodgeMove = _dodgeDirection * _stats.DodgeSpeed;
                return new Vector3(dodgeMove.x, _verticalVelocity, dodgeMove.z) * deltaTime;
            }

            // --- Horizontal ---
            float speed = _stats.MoveSpeed * (isSprinting ? _stats.SprintMultiplier : 1f);

            // Rotate input relative to where the player is facing.
            Vector3 horizontal = Quaternion.Euler(0f, yaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);

            // Clamp diagonal movement to the same max speed as cardinal.
            if (horizontal.sqrMagnitude > 1f)
                horizontal.Normalize();
            horizontal *= speed;

            return new Vector3(horizontal.x, _verticalVelocity, horizontal.z) * deltaTime;
        }

        /// <summary>Queue a jump; consumed on the next <see cref="ComputeMove"/> call.</summary>
        public void RequestJump() => _jumpQueued = true;

        /// <summary>Begin a dodge roll in the given world-space direction.</summary>
        public bool RequestDodge(Vector3 direction)
        {
            if (IsDodging) return false;
            _dodgeDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.forward;
            _dodgeTimer = _stats.DodgeDuration;
            return true;
        }
    }
}
