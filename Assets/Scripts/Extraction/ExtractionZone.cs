using UnityEngine;
using EverRealm.Exiles.Core;

namespace EverRealm.Exiles.Extraction
{
    /// <summary>
    /// Runtime MonoBehaviour spawned when a player activates an
    /// <see cref="ExtractionBlockEntity"/>. Manages a countdown timer
    /// and proximity check — the player must stay within range until
    /// the timer completes, Arc Raiders style.
    ///
    /// If the player leaves the radius the timer pauses (does NOT reset)
    /// so they can fight and return.
    /// </summary>
    public sealed class ExtractionZone : MonoBehaviour
    {
        /// <summary>Total seconds the player must remain in proximity.</summary>
        private const float ExtractionDuration = 8f;

        /// <summary>Radius (world units) the player must stay within.</summary>
        private const float Radius = 6f;

        private ExtractionBlockEntity _entity;
        private Transform _player;
        private float _timer;
        private bool _completed;

        /// <summary>Normalised progress (0 → 1).</summary>
        public float Progress => Mathf.Clamp01(_timer / ExtractionDuration);

        /// <summary>True when the player is within extraction radius.</summary>
        public bool PlayerInRange { get; private set; }

        /// <summary>Fires every frame with normalised progress.</summary>
        public event System.Action<float> OnProgressChanged;

        /// <summary>Fires once when extraction completes.</summary>
        public event System.Action OnExtractionComplete;

        // -----------------------------------------------------------------

        public void Init(ExtractionBlockEntity entity)
        {
            _entity = entity;

            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
                _player = playerGo.transform;

            GameBootstrap.Instance?.SetState(GameState.Extracting);
            AudioManager.Instance?.PlayExtractionActivate(transform.position);
        }

        private void Update()
        {
            if (_completed) return;

            // Bail if run already ended (e.g., player died).
            if (GameBootstrap.Instance != null &&
                GameBootstrap.Instance.CurrentState == GameState.RunEnd)
            {
                Destroy(gameObject);
                return;
            }

            PlayerInRange = _player != null &&
                Vector3.Distance(transform.position, _player.position) <= Radius;

            if (PlayerInRange)
            {
                _timer += Time.deltaTime;
                OnProgressChanged?.Invoke(Progress);

                if (_timer >= ExtractionDuration)
                {
                    _completed = true;
                    OnExtractionComplete?.Invoke();
                    AudioManager.Instance?.PlayExtractionComplete();
                    Debug.Log("[ExtractionZone] Extraction complete!");
                    RunManager.Instance?.EndRun(true);
                }
            }
            else
            {
                // Timer pauses but does not reset — player can fight and return.
                OnProgressChanged?.Invoke(Progress);
            }
        }
    }
}
