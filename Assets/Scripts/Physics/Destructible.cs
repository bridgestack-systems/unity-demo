using UnityEngine;
using UnityEngine.Events;
using NexusArena.Core;
using NexusArena.DataVisualization;
using NexusArena.Environment;

namespace NexusArena.Physics
{
    public class Destructible : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Destruction")]
        [SerializeField] private GameObject fracturedPrefab;
        [SerializeField] private int proceduralDebrisCount = 8;
        [SerializeField] private float debrisScale = 0.2f;
        [SerializeField] private float debrisExplosionForce = 5f;
        [SerializeField] private float debrisLifetime = 5f;
        [SerializeField] private Material debrisMaterial;

        [Header("Effects")]
        [SerializeField] private ParticleSystem breakEffect;
        [SerializeField] private AudioClip breakSound;
        [SerializeField] [Range(0f, 1f)] private float breakSoundVolume = 1f;

        [Header("Events")]
        public UnityEvent OnDestroyed;

        public float HealthNormalized => currentHealth / maxHealth;
        public bool IsDestroyed { get; private set; }

        private void Awake()
        {
            currentHealth = maxHealth;
            if (!gameObject.CompareTag("Destructible"))
                gameObject.tag = "Destructible";
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (IsDestroyed) return;

            currentHealth -= amount;

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Destroy(hitPoint, hitDirection);
            }
        }

        private void Destroy(Vector3 hitPoint, Vector3 hitDirection)
        {
            IsDestroyed = true;

            if (breakEffect != null)
            {
                ParticleEffectsManager.Instance?.SpawnEffect(breakEffect, transform.position, Quaternion.identity);
            }

            if (breakSound != null)
            {
                AudioManager.Instance?.PlaySFX(breakSound, transform.position, breakSoundVolume);
            }

            StatsTracker.Instance?.RecordEvent("Destruction", maxHealth);

            if (fracturedPrefab != null)
            {
                SpawnFracturedVersion(hitDirection);
            }
            else
            {
                SpawnProceduralDebris(hitPoint, hitDirection);
            }

            OnDestroyed?.Invoke();
            Destroy(gameObject);
        }

        private void SpawnFracturedVersion(Vector3 hitDirection)
        {
            var fractured = Instantiate(fracturedPrefab, transform.position, transform.rotation);
            var rigidbodies = fractured.GetComponentsInChildren<Rigidbody>();

            foreach (var rb in rigidbodies)
            {
                rb.AddForce(hitDirection.normalized * debrisExplosionForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * debrisExplosionForce, ForceMode.Impulse);
            }

            Destroy(fractured, debrisLifetime);
        }

        private void SpawnProceduralDebris(Vector3 hitPoint, Vector3 hitDirection)
        {
            for (int i = 0; i < proceduralDebrisCount; i++)
            {
                var debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
                float scale = debrisScale * Random.Range(0.5f, 1.5f);
                debris.transform.localScale = Vector3.one * scale;
                debris.transform.position = hitPoint + Random.insideUnitSphere * 0.3f;
                debris.transform.rotation = Random.rotation;

                if (debrisMaterial != null)
                {
                    debris.GetComponent<Renderer>().material = debrisMaterial;
                }
                else
                {
                    var renderer = GetComponent<Renderer>();
                    if (renderer != null)
                        debris.GetComponent<Renderer>().material = renderer.material;
                }

                var rb = debris.AddComponent<Rigidbody>();
                rb.mass = 0.1f;
                Vector3 force = (hitDirection.normalized + Random.insideUnitSphere).normalized * debrisExplosionForce;
                rb.AddForce(force, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * debrisExplosionForce, ForceMode.Impulse);

                Destroy(debris, debrisLifetime);
            }
        }
    }
}
