using UnityEngine;
using UnityEngine.InputSystem;
using EverRealm.Exiles.World;

namespace EverRealm.Exiles.Tools
{
    /// <summary>
    /// Testing utility — spawns enemies around the player at runtime.
    /// Add to any GameObject in the Game scene and assign the enemy prefab.
    /// Press F9 (default) to spawn a wave.
    /// </summary>
    public sealed class DebugEnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private int _count = 5;
        [SerializeField] private float _minRadius = 8f;
        [SerializeField] private float _maxRadius = 20f;
        [SerializeField] private Key _spawnKey = Key.F9;

        private Transform _player;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[_spawnKey].wasPressedThisFrame)
            {
                if (_player == null)
                {
                    var playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null) _player = playerObj.transform;
                }

                if (_player != null)
                    SpawnWave();
                else
                    Debug.LogWarning("[DebugSpawner] No Player found.");
            }
        }

        private void SpawnWave()
        {
            if (_enemyPrefab == null)
            {
                Debug.LogWarning("[DebugSpawner] No enemy prefab assigned.");
                return;
            }

            int spawned = 0;
            for (int i = 0; i < _count; i++)
            {
                if (TryGetSpawnPosition(out Vector3 pos))
                {
                    Instantiate(_enemyPrefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    spawned++;
                }
            }

            Debug.Log($"[DebugSpawner] Spawned {spawned}/{_count} enemies around player.");
        }

        private bool TryGetSpawnPosition(out Vector3 pos)
        {
            pos = Vector3.zero;

            // Pick a random point in a ring around the player.
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(_minRadius, _maxRadius);
            float x = _player.position.x + Mathf.Cos(angle) * dist;
            float z = _player.position.z + Mathf.Sin(angle) * dist;

            // Raycast down to find terrain surface.
            if (Physics.Raycast(new Vector3(x, 500f, z), Vector3.down, out RaycastHit hit, 1000f))
            {
                pos = hit.point + Vector3.up * 0.5f;
                return true;
            }

            return false;
        }
    }
}
