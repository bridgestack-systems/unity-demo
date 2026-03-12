using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NexusArena.Core
{
    public class SceneController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        private bool isTransitioning;

        public void LoadSceneAsync(string sceneName, Action<float> onProgress = null)
        {
            if (isTransitioning) return;
            StartCoroutine(LoadSceneCoroutine(sceneName, onProgress));
        }

        public void LoadSceneWithFade(string sceneName, Action<float> onProgress = null)
        {
            if (isTransitioning) return;
            StartCoroutine(TransitionCoroutine(sceneName, onProgress));
        }

        private IEnumerator LoadSceneCoroutine(string sceneName, Action<float> onProgress)
        {
            isTransitioning = true;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                onProgress?.Invoke(operation.progress / 0.9f);
                yield return null;
            }

            onProgress?.Invoke(1f);
            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                yield return null;
            }

            isTransitioning = false;
        }

        private IEnumerator TransitionCoroutine(string sceneName, Action<float> onProgress)
        {
            isTransitioning = true;

            yield return StartCoroutine(FadeCoroutine(1f));

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                onProgress?.Invoke(operation.progress / 0.9f);
                yield return null;
            }

            onProgress?.Invoke(1f);
            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return StartCoroutine(FadeCoroutine(0f));

            isTransitioning = false;
        }

        private IEnumerator FadeCoroutine(float targetAlpha)
        {
            if (fadeCanvasGroup == null) yield break;

            float startAlpha = fadeCanvasGroup.alpha;
            float elapsed = 0f;

            fadeCanvasGroup.blocksRaycasts = true;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = targetAlpha;
            fadeCanvasGroup.blocksRaycasts = targetAlpha > 0f;
        }
    }
}
