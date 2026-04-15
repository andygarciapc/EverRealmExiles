using UnityEngine;

namespace EverRealm.Exiles.Data
{
    /// <summary>
    /// Data asset describing a weapon's stats and timing.
    /// Create via Assets → Create → EverRealm → Weapon Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "EverRealm/Weapon Definition", fileName = "Weapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string WeaponName = "Sword";

        [Header("Light Attack")]
        [Tooltip("Damage dealt per light swing.")]
        public float LightDamage     = 15f;

        [Tooltip("Stamina cost per light swing.")]
        public float LightStamina    = 10f;

        [Tooltip("Seconds of windup before the hitbox activates (light).")]
        public float LightWindup     = 0.1f;

        [Tooltip("Seconds the hitbox stays active (light).")]
        public float LightActive     = 0.15f;

        [Tooltip("Recovery time after the active window before the next action (light).")]
        public float LightRecovery   = 0.25f;

        [Header("Heavy Attack")]
        public float HeavyDamage     = 35f;
        public float HeavyStamina    = 25f;
        public float HeavyWindup     = 0.35f;
        public float HeavyActive     = 0.2f;
        public float HeavyRecovery   = 0.4f;

        [Header("Reach")]
        [Tooltip("Half-extents of the hitbox volume in front of the player.")]
        public Vector3 HitboxHalfExtents = new(0.5f, 0.5f, 1f);

        [Tooltip("Forward offset of the hitbox centre from the player.")]
        public float HitboxForwardOffset = 1.2f;

        [Header("Knockback")]
        public float KnockbackForce  = 4f;
    }
}
