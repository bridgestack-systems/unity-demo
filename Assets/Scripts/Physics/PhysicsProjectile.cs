using UnityEngine;

namespace NexusArena.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsProjectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float damage = 25f;

        [Header("Impact")]
        [SerializeField] private ParticleSystem impactEffect;
        [SerializeField] private bool explodeOnImpact;
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private float explosionForce = 500f;
        [SerializeField] private float explosionUpwardModifier = 1f;

        [Header("Trail")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private bool autoConfigureTrail = true;

        private Rigidbody _rb;
        private bool _hasImpacted;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = true;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (trailRenderer == null)
                trailRenderer = GetComponent<TrailRenderer>();

            if (trailRenderer != null && autoConfigureTrail)
            {
                trailRenderer.time = 0.3f;
                trailRenderer.startWidth = 0.1f;
                trailRenderer.endWidth = 0f;
            }
        }

        public void Launch(Vector3 direction, float speed)
        {
            _rb.linearVelocity = direction.normalized * speed;
            Destroy(gameObject, lifetime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasImpacted) return;
            _hasImpacted = true;

            var contact = collision.GetContact(0);

            if (explodeOnImpact)
            {
                ExplosionForce.Explode(contact.point, explosionRadius, explosionForce, explosionUpwardModifier);
            }
            else
            {
                var destructible = collision.gameObject.GetComponent<Destructible>();
                destructible?.TakeDamage(damage, contact.point, _rb.linearVelocity.normalized);
            }

            if (impactEffect != null)
            {
                var effect = Instantiate(impactEffect, contact.point, Quaternion.LookRotation(contact.normal));
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
            }

            Destroy(gameObject);
        }
    }
}
