using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NexusArena.Environment
{
    public class ParticleEffectsManager : MonoBehaviour
    {
        public static ParticleEffectsManager Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private int defaultPoolSize = 20;

        [Header("Default Effect Prefabs")]
        [SerializeField] private ParticleSystem explosionPrefab;
        [SerializeField] private ParticleSystem dustPrefab;
        [SerializeField] private ParticleSystem sparksPrefab;
        [SerializeField] private ParticleSystem glowPrefab;

        private readonly Dictionary<int, Queue<ParticleSystem>> _pools = new();
        private Transform _poolParent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _poolParent = new GameObject("ParticleEffectsPool").transform;
            _poolParent.SetParent(transform);
        }

        public ParticleSystem SpawnEffect(ParticleSystem prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                prefab = CreateDefaultEffect();
            }

            int key = prefab.GetInstanceID();
            var instance = GetFromPool(key, prefab);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.gameObject.SetActive(true);
            instance.Play(true);

            StartCoroutine(ReturnToPoolWhenComplete(instance, key));

            return instance;
        }

        public ParticleSystem SpawnExplosion(Vector3 position) =>
            SpawnEffect(explosionPrefab, position, Quaternion.identity);

        public ParticleSystem SpawnDust(Vector3 position) =>
            SpawnEffect(dustPrefab, position, Quaternion.identity);

        public ParticleSystem SpawnSparks(Vector3 position, Quaternion rotation) =>
            SpawnEffect(sparksPrefab, position, rotation);

        public ParticleSystem SpawnGlow(Vector3 position) =>
            SpawnEffect(glowPrefab, position, Quaternion.identity);

        private ParticleSystem GetFromPool(int key, ParticleSystem prefab)
        {
            if (_pools.TryGetValue(key, out var pool) && pool.Count > 0)
            {
                return pool.Dequeue();
            }

            var instance = Instantiate(prefab, _poolParent);
            instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void ReturnToPool(ParticleSystem instance, int key)
        {
            if (instance == null) return;

            instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_poolParent);

            if (!_pools.TryGetValue(key, out var pool))
            {
                pool = new Queue<ParticleSystem>();
                _pools[key] = pool;
            }

            if (pool.Count < defaultPoolSize)
            {
                pool.Enqueue(instance);
            }
            else
            {
                Destroy(instance.gameObject);
            }
        }

        private IEnumerator ReturnToPoolWhenComplete(ParticleSystem instance, int key)
        {
            while (instance != null && instance.isPlaying)
            {
                yield return null;
            }
            ReturnToPool(instance, key);
        }

        public ParticleSystem CreateDefaultEffect()
        {
            var go = new GameObject("DefaultEffect");
            go.transform.SetParent(_poolParent);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 0.6f;
            main.startLifetime = 0.5f;
            main.startSpeed = 4f;
            main.startSize = 0.18f;
            main.startColor = new Color(1f, 0.5f, 0.15f, 1f);
            main.maxParticles = 60;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.3f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 35) });
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.4f),
                    new GradientColorKey(new Color(0.8f, 0.15f, 0f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            go.SetActive(false);

            return ps;
        }

        public void PrewarmPool(ParticleSystem prefab, int count)
        {
            if (prefab == null) return;
            int key = prefab.GetInstanceID();

            if (!_pools.ContainsKey(key))
                _pools[key] = new Queue<ParticleSystem>();

            for (int i = 0; i < count; i++)
            {
                var instance = Instantiate(prefab, _poolParent);
                instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                instance.gameObject.SetActive(false);
                _pools[key].Enqueue(instance);
            }
        }
    }
}
