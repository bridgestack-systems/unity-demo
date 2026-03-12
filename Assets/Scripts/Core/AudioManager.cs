using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NexusArena.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private int sfxPoolSize = 16;

        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float bgmVolume = 1f;

        private AudioSource bgmSourceA;
        private AudioSource bgmSourceB;
        private AudioSource activeBgmSource;
        private readonly Queue<AudioSource> sfxPool = new();
        private readonly List<AudioSource> allSfxSources = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeBgmSources();
            InitializeSfxPool();
        }

        private void InitializeBgmSources()
        {
            bgmSourceA = CreateAudioSource("BGM_A");
            bgmSourceA.loop = true;

            bgmSourceB = CreateAudioSource("BGM_B");
            bgmSourceB.loop = true;

            activeBgmSource = bgmSourceA;
        }

        private void InitializeSfxPool()
        {
            for (int i = 0; i < sfxPoolSize; i++)
            {
                AudioSource source = CreateAudioSource($"SFX_{i}");
                source.playOnAwake = false;
                sfxPool.Enqueue(source);
                allSfxSources.Add(source);
            }
        }

        private AudioSource CreateAudioSource(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(transform);
            return child.AddComponent<AudioSource>();
        }

        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableSfxSource();
            source.transform.position = position;
            source.spatialBlend = 1f;
            source.clip = clip;
            source.volume = volume * sfxVolume * masterVolume;
            source.Play();
        }

        public void PlaySFX2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableSfxSource();
            source.spatialBlend = 0f;
            source.clip = clip;
            source.volume = volume * sfxVolume * masterVolume;
            source.Play();
        }

        private AudioSource GetAvailableSfxSource()
        {
            AudioSource source = sfxPool.Dequeue();
            sfxPool.Enqueue(source);

            if (source.isPlaying)
            {
                source.Stop();
            }

            return source;
        }

        public void PlayBGM(AudioClip clip, float fadeTime = 1f)
        {
            if (clip == null) return;

            AudioSource incomingSource = activeBgmSource == bgmSourceA ? bgmSourceB : bgmSourceA;
            incomingSource.clip = clip;
            incomingSource.volume = 0f;
            incomingSource.Play();

            StartCoroutine(CrossfadeBgm(activeBgmSource, incomingSource, fadeTime));
            activeBgmSource = incomingSource;
        }

        public void StopBGM(float fadeTime = 1f)
        {
            StartCoroutine(FadeOutBgm(activeBgmSource, fadeTime));
        }

        private IEnumerator CrossfadeBgm(AudioSource outgoing, AudioSource incoming, float duration)
        {
            float elapsed = 0f;
            float outStartVolume = outgoing.volume;
            float targetVolume = bgmVolume * masterVolume;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                outgoing.volume = Mathf.Lerp(outStartVolume, 0f, t);
                incoming.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            outgoing.Stop();
            outgoing.volume = 0f;
            incoming.volume = targetVolume;
        }

        private IEnumerator FadeOutBgm(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            source.Stop();
            source.volume = 0f;
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            UpdateBgmVolume();
        }

        private void UpdateAllVolumes()
        {
            UpdateBgmVolume();
        }

        private void UpdateBgmVolume()
        {
            if (activeBgmSource != null && activeBgmSource.isPlaying)
            {
                activeBgmSource.volume = bgmVolume * masterVolume;
            }
        }
    }
}
