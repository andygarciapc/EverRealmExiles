using UnityEngine;

namespace EverRealm.Exiles.Combat
{
    /// <summary>
    /// Payload passed through <see cref="IDamageable.TakeDamage"/>.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly float   Amount;
        public readonly Vector3 HitPoint;
        public readonly Vector3 KnockbackDir;
        public readonly float   KnockbackForce;
        public readonly GameObject Source;

        public DamageInfo(float amount, Vector3 hitPoint, Vector3 knockbackDir, float knockbackForce, GameObject source)
        {
            Amount         = amount;
            HitPoint       = hitPoint;
            KnockbackDir   = knockbackDir;
            KnockbackForce = knockbackForce;
            Source         = source;
        }
    }
}
