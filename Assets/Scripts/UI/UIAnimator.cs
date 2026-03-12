using System.Collections;
using UnityEngine;
using TMPro;

namespace NexusArena.UI
{
    public static class UIAnimator
    {
        public static readonly AnimationCurve EaseInOut = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public static readonly AnimationCurve EaseOut = new(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(1f, 1f, 0f, 0f)
        );
        public static readonly AnimationCurve EaseIn = new(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(1f, 1f, 2f, 0f)
        );
        public static readonly AnimationCurve Bounce = new(
            new Keyframe(0f, 0f),
            new Keyframe(0.6f, 1.1f),
            new Keyframe(0.8f, 0.95f),
            new Keyframe(1f, 1f)
        );

        public static IEnumerator FadeIn(CanvasGroup canvasGroup, float duration, AnimationCurve curve = null)
        {
            curve ??= EaseInOut;
            float elapsed = 0f;
            canvasGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = curve.Evaluate(t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        public static IEnumerator FadeOut(CanvasGroup canvasGroup, float duration, AnimationCurve curve = null)
        {
            curve ??= EaseInOut;
            float elapsed = 0f;
            canvasGroup.alpha = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = 1f - curve.Evaluate(t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        public static IEnumerator ScalePop(Transform target, float targetScale, float duration, AnimationCurve curve = null)
        {
            curve ??= Bounce;
            Vector3 originalScale = target.localScale;
            Vector3 finalScale = originalScale * targetScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.LerpUnclamped(originalScale, finalScale, curve.Evaluate(t));
                yield return null;
            }

            target.localScale = finalScale;
        }

        public static IEnumerator SlideIn(RectTransform target, Vector2 from, Vector2 to, float duration, AnimationCurve curve = null)
        {
            curve ??= EaseOut;
            target.anchoredPosition = from;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, curve.Evaluate(t));
                yield return null;
            }

            target.anchoredPosition = to;
        }

        public static IEnumerator SlideOut(RectTransform target, Vector2 from, Vector2 to, float duration, AnimationCurve curve = null)
        {
            curve ??= EaseIn;
            target.anchoredPosition = from;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, curve.Evaluate(t));
                yield return null;
            }

            target.anchoredPosition = to;
        }

        public static IEnumerator TypewriterText(TMP_Text textComponent, string text, float charsPerSecond = 30f)
        {
            textComponent.text = "";
            textComponent.maxVisibleCharacters = 0;
            textComponent.text = text;

            int totalChars = text.Length;
            float interval = 1f / charsPerSecond;
            float elapsed = 0f;

            while (textComponent.maxVisibleCharacters < totalChars)
            {
                elapsed += Time.unscaledDeltaTime;
                int visibleChars = Mathf.FloorToInt(elapsed * charsPerSecond);
                textComponent.maxVisibleCharacters = Mathf.Min(visibleChars, totalChars);
                yield return null;
            }

            textComponent.maxVisibleCharacters = totalChars;
        }

        public static IEnumerator PunchScale(Transform target, float punchAmount, float duration)
        {
            Vector3 originalScale = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float punch = Mathf.Sin(t * Mathf.PI) * punchAmount * (1f - t);
                target.localScale = originalScale * (1f + punch);
                yield return null;
            }

            target.localScale = originalScale;
        }

        public static IEnumerator ColorPulse(UnityEngine.UI.Graphic graphic, Color targetColor, float duration, int pulseCount = 1)
        {
            Color originalColor = graphic.color;
            float singlePulseDuration = duration / pulseCount;

            for (int i = 0; i < pulseCount; i++)
            {
                float elapsed = 0f;
                while (elapsed < singlePulseDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / singlePulseDuration);
                    float pingPong = 1f - Mathf.Abs(2f * t - 1f);
                    graphic.color = Color.Lerp(originalColor, targetColor, pingPong);
                    yield return null;
                }
            }

            graphic.color = originalColor;
        }
    }
}
