using UnityEngine;
using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.Core
{
    /// <summary>
    /// Singleton audio system with source pooling.
    /// Persists across scenes via DontDestroyOnLoad.
    /// All play methods null-check their clips so the game works
    /// silently until actual audio assets are imported.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private SFXLibrary _sfxLibrary;

        [Header("Volume")]
        [Range(0f, 1f)]
        [SerializeField] private float _masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float _sfxVolume = 1f;

        private const int PoolSize = 8;
        private AudioSource[] _pool;
        private int _poolIndex;
        private int _footstepIndex;

        public SFXLibrary Library => _sfxLibrary;

        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitPool();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void InitPool()
        {
            _pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var child = new GameObject($"SFX_Source_{i}");
                child.transform.SetParent(transform);
                var src = child.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // default 2D
                _pool[i] = src;
            }
        }

        private AudioSource GetNextSource()
        {
            var src = _pool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % PoolSize;
            return src;
        }

        // -------------------------------------------------------------------------
        // Generic play

        /// <summary>Play a 2D sound effect.</summary>
        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            var src = GetNextSource();
            src.spatialBlend = 0f;
            src.clip = clip;
            src.volume = volume * _sfxVolume * _masterVolume;
            src.Play();
        }

        /// <summary>Play a 3D sound at a world position.</summary>
        public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume * _sfxVolume * _masterVolume);
        }

        // -------------------------------------------------------------------------
        // Named convenience methods

        public void PlayFootstep(Vector3 position)
        {
            if (_sfxLibrary == null || _sfxLibrary.Footsteps == null || _sfxLibrary.Footsteps.Length == 0) return;
            var clip = _sfxLibrary.Footsteps[_footstepIndex % _sfxLibrary.Footsteps.Length];
            _footstepIndex++;
            PlaySFXAtPoint(clip, position, 0.4f);
        }

        public void PlaySwordSwing()
        {
            if (_sfxLibrary != null) PlaySFX(_sfxLibrary.SwordSwing, 0.6f);
        }

        public void PlayHitImpact(Vector3 position)
        {
            if (_sfxLibrary != null) PlaySFXAtPoint(_sfxLibrary.HitImpact, position, 0.7f);
        }

        public void PlayEnemyDeath(Vector3 position)
        {
            if (_sfxLibrary != null) PlaySFXAtPoint(_sfxLibrary.EnemyDeath, position, 0.8f);
        }

        public void PlayChestOpen(Vector3 position)
        {
            if (_sfxLibrary != null) PlaySFXAtPoint(_sfxLibrary.ChestOpen, position, 0.7f);
        }

        public void PlayLootPickup()
        {
            if (_sfxLibrary != null) PlaySFX(_sfxLibrary.LootPickup, 0.5f);
        }

        public void PlayExtractionActivate(Vector3 position)
        {
            if (_sfxLibrary != null) PlaySFXAtPoint(_sfxLibrary.ExtractionActivate, position, 0.8f);
        }

        public void PlayExtractionComplete()
        {
            if (_sfxLibrary != null) PlaySFX(_sfxLibrary.ExtractionComplete, 1f);
        }

        public void PlayPlayerHurt()
        {
            if (_sfxLibrary != null) PlaySFX(_sfxLibrary.PlayerHurt, 0.7f);
        }
    }
}
