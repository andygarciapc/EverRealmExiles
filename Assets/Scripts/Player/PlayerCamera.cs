using UnityEngine;
using UnityEngine.InputSystem;

namespace EverRealm.Exiles.Player
{
    /// <summary>
    /// Third-person follow camera with mouse-look.
    ///
    /// Hierarchy expected:
    ///   Player (PlayerController)
    ///   └── CameraPivot          ← this component lives here
    ///       └── Main Camera      ← offset along -Z
    ///
    /// The pivot sits at the player's eye level. Mouse X rotates the player
    /// body (yaw); mouse Y pitches only the pivot (so the body stays upright).
    ///
    /// Setup:
    ///   1. Create a child GameObject named CameraPivot on the player.
    ///   2. Move the Main Camera under CameraPivot, offset it (e.g., Position = 0, 0, -5).
    ///   3. Add this component to CameraPivot.
    ///   4. Assign _playerController in the Inspector.
    /// </summary>
    public sealed class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private PlayerController _playerController;

        [Header("Sensitivity")]
        [SerializeField] private float _sensitivityX = 0.15f; // degrees per pixel
        [SerializeField] private float _sensitivityY = 0.15f;

        [Header("Pitch Clamp")]
        [SerializeField] private float _minPitch = -80f;
        [SerializeField] private float _maxPitch =  80f;

        private float _yaw;
        private float _pitch;

        private Vector2 _lookDelta;

        // -------------------------------------------------------------------------

        private void Awake()
        {
            // Give PlayerController a reference to this pivot so it can read yaw.
            if (_playerController != null)
                _playerController.CameraPivot = transform;

            // Initialise from current rotation so the camera doesn't snap on start.
            _yaw   = _playerController != null
                ? _playerController.transform.eulerAngles.y
                : 0f;
            _pitch = transform.localEulerAngles.x;

            LockCursor(true);
        }

        private void LateUpdate()
        {
            if (_lookDelta == Vector2.zero) return;

            _yaw   += _lookDelta.x * _sensitivityX;
            _pitch -= _lookDelta.y * _sensitivityY; // subtract: moving mouse up should look up
            _pitch  = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            // Rotate the player body for yaw (so movement stays aligned with facing).
            if (_playerController != null)
                _playerController.transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            // Pitch only affects the camera pivot, not the whole body.
            transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            _lookDelta = Vector2.zero;
        }

        /// <summary>Called by PlayerController to forward look input from the root GameObject.</summary>
        public void ApplyLookDelta(Vector2 delta) => _lookDelta = delta;

        // -------------------------------------------------------------------------

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible   = !locked;
        }

        /// <summary>
        /// Call from UI or pause menu to release / recapture the cursor.
        /// </summary>
        public void SetCursorLocked(bool locked) => LockCursor(locked);
    }
}
