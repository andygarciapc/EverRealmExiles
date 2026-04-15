namespace EverRealm.Exiles.AI
{
    /// <summary>
    /// Common interface for enemy attack components so
    /// <see cref="EnemyController"/> can work with both
    /// melee (<see cref="EnemyAttack"/>) and ranged
    /// (<see cref="EnemyRangedAttack"/>) enemies polymorphically.
    /// </summary>
    public interface IEnemyAttack
    {
        bool IsBusy { get; }
        bool StartAttack();
        void Cancel();
    }
}
