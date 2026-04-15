using UnityEngine;

namespace EverRealm.Exiles.Data
{
    /// <summary>
    /// Central registry of all sound effect clips.
    /// Clip fields are null until actual audio assets are imported and assigned.
    /// The <see cref="Core.AudioManager"/> null-checks all clips before playing.
    /// Create via Assets > Create > EverRealm > SFX Library.
    /// </summary>
    [CreateAssetMenu(menuName = "EverRealm/SFX Library", fileName = "SFXLibrary")]
    public sealed class SFXLibrary : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Random footstep sounds. Cycled through per step.")]
        public AudioClip[] Footsteps;
        public AudioClip DodgeRoll;

        [Header("Combat")]
        public AudioClip SwordSwing;
        public AudioClip HitImpact;
        public AudioClip PlayerHurt;

        [Header("Enemies")]
        public AudioClip EnemyDeath;

        [Header("Interaction")]
        public AudioClip ChestOpen;
        public AudioClip LootPickup;

        [Header("Extraction")]
        public AudioClip ExtractionActivate;
        public AudioClip ExtractionComplete;

        [Header("UI")]
        public AudioClip UIClick;
    }
}
