using UnityEngine;

namespace EverRealm.Exiles.Combat
{
    /// <summary>
    /// Simple projectile that moves forward, checks for <see cref="IDamageable"/>
    /// targets on collision, and self-destructs on impact or after a timeout.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class Projectile : MonoBehaviour
    {
        private float _speed;
        private float _damage;
        private float _knockbackForce;
        private GameObject _source;
        private float _lifetime = 3f;
        private bool _hit;

        /// <summary>Initialize the projectile after instantiation.</summary>
        public void Init(float speed, float damage, float knockbackForce, GameObject source)
        {
            _speed = speed;
            _damage = damage;
            _knockbackForce = knockbackForce;
            _source = source;
        }

        private void Update()
        {
            if (_hit) return;

            transform.Translate(Vector3.forward * (_speed * Time.deltaTime));

            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hit) return;

            // Ignore the shooter.
            if (_source != null && other.transform.root == _source.transform.root) return;

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 knockDir = transform.forward;

                damageable.TakeDamage(new DamageInfo(
                    _damage, hitPoint, knockDir, _knockbackForce, _source));
            }

            _hit = true;
            Destroy(gameObject);
        }
    }
}
