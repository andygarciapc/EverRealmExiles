namespace EverRealm.Exiles.Combat
{
    /// <summary>
    /// Implement on any GameObject that can receive damage
    /// (player, enemies, destructibles).
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(DamageInfo info);
    }
}
