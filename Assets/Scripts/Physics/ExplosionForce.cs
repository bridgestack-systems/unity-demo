using UnityEngine;
using NexusArena.Environment;

namespace NexusArena.Physics
{
    public static class ExplosionForce
    {
        private static ParticleSystem _defaultExplosionEffect;

        public static void Explode(Vector3 position, float radius, float force, float upwardModifier = 1f)
        {
            Explode(position, radius, force, upwardModifier, null, 0f);
        }

        public static void Explode(
            Vector3 position,
            float radius,
            float force,
            float upwardModifier,
            ParticleSystem effectPrefab,
            float damage)
        {
            var colliders = UnityEngine.Physics.OverlapSphere(position, radius);

            foreach (var col in colliders)
            {
                var rb = col.attachedRigidbody;
                if (rb != null)
                {
                    rb.AddExplosionForce(force, position, radius, upwardModifier, ForceMode.Impulse);
                }

                if (damage > 0f)
                {
                    var destructible = col.GetComponent<Destructible>();
                    if (destructible != null)
                    {
                        Vector3 direction = (col.transform.position - position).normalized;
                        float distanceFactor = 1f - Mathf.Clamp01(Vector3.Distance(position, col.transform.position) / radius);
                        destructible.TakeDamage(damage * distanceFactor, col.ClosestPoint(position), direction);
                    }
                }
            }

            if (effectPrefab != null)
            {
                ParticleEffectsManager.Instance?.SpawnEffect(effectPrefab, position, Quaternion.identity);
            }

            ApplyCameraShake(position, radius);
        }

        private static void ApplyCameraShake(Vector3 explosionPos, float radius)
        {
            var cam = Camera.main;
            if (cam == null) return;

            float distance = Vector3.Distance(cam.transform.position, explosionPos);
            if (distance > radius * 2f) return;

            float intensity = 1f - Mathf.Clamp01(distance / (radius * 2f));

            var shaker = cam.GetComponent<CameraShake>();
            if (shaker == null)
                shaker = cam.gameObject.AddComponent<CameraShake>();

            shaker.Shake(intensity * 0.5f, 0.3f);
        }
    }

    public class CameraShake : MonoBehaviour
    {
        private float _remainingDuration;
        private float _intensity;
        private Vector3 _originalLocalPosition;
        private bool _isShaking;

        public void Shake(float intensity, float duration)
        {
            _intensity = Mathf.Max(_intensity, intensity);
            _remainingDuration = Mathf.Max(_remainingDuration, duration);

            if (!_isShaking)
            {
                _originalLocalPosition = transform.localPosition;
                _isShaking = true;
            }
        }

        private void Update()
        {
            if (!_isShaking) return;

            if (_remainingDuration > 0f)
            {
                transform.localPosition = _originalLocalPosition + Random.insideUnitSphere * _intensity;
                _remainingDuration -= Time.deltaTime;
                _intensity = Mathf.Lerp(_intensity, 0f, Time.deltaTime * 5f);
            }
            else
            {
                transform.localPosition = _originalLocalPosition;
                _isShaking = false;
                _intensity = 0f;
            }
        }
    }
}
