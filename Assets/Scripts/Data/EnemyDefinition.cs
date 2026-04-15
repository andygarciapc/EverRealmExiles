using UnityEngine;

namespace EverRealm.Exiles.Data
{
    /// <summary>
    /// Data asset for an enemy archetype.
    /// Create via Assets → Create → EverRealm → Enemy Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "EverRealm/Enemy Definition", fileName = "Enemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string DisplayName = "Grunt";

        [Header("Health")]
        public float MaxHealth = 60f;

        [Header("Movement")]
        public float MoveSpeed       = 3.5f;
        public float PatrolRadius    = 10f;
        public float PatrolWaitMin   = 1f;
        public float PatrolWaitMax   = 3f;

        [Header("Detection")]
        public float DetectionRadius = 12f;
        public float LoseRadius      = 18f;

        [Header("Attack")]
        public float AttackRange     = 2f;
        public float AttackDamage    = 12f;
        public float AttackWindup    = 0.3f;
        public float AttackActive    = 0.15f;
        public float AttackRecovery  = 0.5f;
        public float KnockbackForce  = 3f;

        [Header("Stagger")]
        [Tooltip("Damage threshold in a single hit to trigger stagger.")]
        public float StaggerThreshold = 25f;
        public float StaggerDuration  = 0.6f;

        [Header("Loot")]
        [Tooltip("Loot table rolled on death. Leave null for no drops.")]
        public LootTable LootTable;
    }
}
