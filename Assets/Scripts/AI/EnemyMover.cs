using System.Collections.Generic;
using UnityEngine;
using EverRealm.Exiles.World;

namespace EverRealm.Exiles.AI
{
    /// <summary>
    /// Moves an enemy along a voxel A* path.
    /// Replaces NavMeshAgent — works directly on the block grid.
    /// </summary>
    public sealed class EnemyMover
    {
        private readonly Transform _transform;
        private readonly CharacterController _cc;
        private float _speed;
        private float _gravity = 20f;
        private float _jumpSpeed = 8f; // Enough to clear ~2 blocks.
        private float _verticalVelocity;

        private List<Vector3> _path;
        private int _pathIndex;

        private const float WaypointThreshold = 0.3f;

        public bool HasReachedDestination => _path == null || _pathIndex >= _path.Count;

        public EnemyMover(Transform transform, CharacterController cc)
        {
            _transform = transform;
            _cc = cc;
            _cc.stepOffset = 1.05f; // Auto-step up 1-block voxel height changes.
        }

        public void SetSpeed(float speed) => _speed = speed;

        public void MoveTo(Vector3 worldTarget)
        {
            _path = VoxelPathfinder.FindPath(_transform.position, worldTarget);
            _pathIndex = 0;
        }

        public void Stop()
        {
            _path = null;
            _pathIndex = 0;
        }

        /// <summary>Call every frame. Moves the enemy along the current path.</summary>
        public void Tick(float deltaTime)
        {
            // Gravity
            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity -= _gravity * deltaTime;

            if (_path == null || _pathIndex >= _path.Count)
            {
                // Apply only gravity when idle.
                _cc.Move(new Vector3(0f, _verticalVelocity * deltaTime, 0f));
                return;
            }

            Vector3 target = _path[_pathIndex];
            Vector3 pos = _transform.position;
            Vector3 horizontal = target - pos;
            horizontal.y = 0f;

            if (horizontal.magnitude < WaypointThreshold)
            {
                _pathIndex++;
                if (_pathIndex >= _path.Count)
                {
                    _cc.Move(new Vector3(0f, _verticalVelocity * deltaTime, 0f));
                    return;
                }
                target = _path[_pathIndex];
                horizontal = target - pos;
                horizontal.y = 0f;
            }

            // Jump only for 2+ block climbs (1-block steps handled by stepOffset).
            float heightDiff = target.y - pos.y;
            if (heightDiff > 1.1f && _cc.isGrounded)
                _verticalVelocity = _jumpSpeed;

            // Dampen horizontal movement while mid-jump and still below the target
            // so the CC rises before slamming into the wall face.
            float hScale = 1f;
            if (!_cc.isGrounded && heightDiff > 0.3f)
                hScale = 0.15f;

            Vector3 move = horizontal.normalized * _speed * hScale;
            move.y = _verticalVelocity;
            _cc.Move(move * deltaTime);
        }

        /// <summary>
        /// Picks a random walkable surface position within radius of the origin.
        /// </summary>
        public bool TryGetRandomPoint(Vector3 origin, float radius, out Vector3 result)
        {
            var wm = WorldManager.Instance;
            if (wm != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 rnd = Random.insideUnitCircle * radius;
                    int wx = Mathf.FloorToInt(origin.x + rnd.x);
                    int wz = Mathf.FloorToInt(origin.z + rnd.y);
                    int wy = wm.GetSurfaceY(wx, wz);
                    if (wy >= 0)
                    {
                        result = new Vector3(wx + 0.5f, wy, wz + 0.5f);
                        return true;
                    }
                }
            }
            result = origin;
            return false;
        }

        public void FaceTarget(Vector3 target, float rotSpeed)
        {
            Vector3 dir = (target - _transform.position).normalized;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion look = Quaternion.LookRotation(dir);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, look, rotSpeed * Time.deltaTime);
        }
    }
}
