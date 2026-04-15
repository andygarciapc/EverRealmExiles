using UnityEngine;

namespace EverRealm.Exiles.Data
{
    /// <summary>
    /// Tunable movement parameters for the player. Create via
    /// Assets → Create → EverRealm → Player Stats.
    /// </summary>
    [CreateAssetMenu(menuName = "EverRealm/Player Stats", fileName = "PlayerStats")]
    public sealed class PlayerStats : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Base horizontal speed (m/s).")]
        public float MoveSpeed      = 6f;

        [Tooltip("Multiplier applied on top of MoveSpeed while sprinting.")]
        public float SprintMultiplier = 1.65f;

        [Header("Jumping")]
        [Tooltip("How high the player jumps (metres, approximately).")]
        public float JumpHeight     = 1.4f;

        [Tooltip("Window after walking off a ledge where jump is still allowed (seconds).")]
        public float CoyoteTime     = 0.15f;

        [Header("Stamina")]
        [Tooltip("Maximum stamina pool.")]
        public float MaxStamina      = 100f;

        [Tooltip("Stamina recovered per second while not acting.")]
        public float StaminaRegen    = 20f;

        [Tooltip("Seconds after spending stamina before regen starts.")]
        public float StaminaRegenDelay = 1f;

        [Header("Dodge")]
        [Tooltip("Horizontal burst speed during a dodge roll (m/s).")]
        public float DodgeSpeed      = 14f;

        [Tooltip("Total duration of the dodge roll (seconds).")]
        public float DodgeDuration   = 0.4f;

        [Tooltip("Window within the dodge that grants invincibility (seconds).")]
        public float DodgeIFrames    = 0.3f;

        [Tooltip("Stamina cost per dodge.")]
        public float DodgeStaminaCost = 25f;

        [Header("Physics")]
        [Tooltip("Gravity applied to the player (m/s²). Positive = downward.")]
        public float Gravity        = 20f;

        [Tooltip("Maximum downward fall speed (m/s).")]
        public float MaxFallSpeed   = 40f;
    }
}
