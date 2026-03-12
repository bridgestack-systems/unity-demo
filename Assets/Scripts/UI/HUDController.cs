using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NexusArena.Core;

namespace NexusArena.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Gradient healthColorGradient;

        [Header("Score")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private string scoreFormat = "Score: {0:N0}";

        [Header("Timer")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private string timerFormat = "{0:00}:{1:00}";

        [Header("Crosshair")]
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Color crosshairDefault = Color.white;
        [SerializeField] private Color crosshairInteractable = Color.green;
        [SerializeField] private float crosshairRayDistance = 50f;

        [Header("Minimap")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private RenderTexture minimapRenderTexture;

        [Header("FPS Counter")]
        [SerializeField] private TMP_Text fpsText;
        [SerializeField] private float fpsUpdateInterval = 0.5f;

        [Header("Notifications")]
        [SerializeField] private TMP_Text notificationText;
        [SerializeField] private CanvasGroup notificationCanvasGroup;
        [SerializeField] private float notificationFadeDuration = 0.3f;

        [Header("Canvas Group")]
        [SerializeField] private CanvasGroup hudCanvasGroup;

        private float _currentHealth = 1f;
        private int _currentScore;
        private float _matchTimer;
        private bool _timerRunning;
        private float _fpsTimer;
        private int _frameCount;
        private readonly Queue<NotificationEntry> _notificationQueue = new();
        private bool _isShowingNotification;
        private Camera _cam;

        private struct NotificationEntry
        {
            public string Message;
            public float Duration;
        }

        private void Start()
        {
            _cam = Camera.main;

            if (minimapImage != null && minimapRenderTexture != null)
                minimapImage.texture = minimapRenderTexture;

            if (notificationCanvasGroup != null)
                notificationCanvasGroup.alpha = 0f;

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;

            UpdateHealthBar(_currentHealth);
            UpdateScore(0);
        }

        private void Update()
        {
            UpdateCrosshair();
            UpdateFPSCounter();

            if (_timerRunning)
            {
                _matchTimer += Time.deltaTime;
                UpdateTimerDisplay();
            }
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (hudCanvasGroup == null) return;

            hudCanvasGroup.alpha = newState == GameState.Playing ? 1f : 0f;
            hudCanvasGroup.interactable = newState == GameState.Playing;
            hudCanvasGroup.blocksRaycasts = newState == GameState.Playing;

            _timerRunning = newState == GameState.Playing;
        }

        public void UpdateHealthBar(float normalizedHealth)
        {
            _currentHealth = Mathf.Clamp01(normalizedHealth);
            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = _currentHealth;
                if (healthColorGradient != null)
                    healthBarFill.color = healthColorGradient.Evaluate(_currentHealth);
            }
        }

        public void UpdateScore(int score)
        {
            _currentScore = score;
            if (scoreText != null)
                scoreText.text = string.Format(scoreFormat, _currentScore);
        }

        public void AddScore(int amount)
        {
            UpdateScore(_currentScore + amount);
        }

        private void UpdateTimerDisplay()
        {
            if (timerText == null) return;
            int minutes = Mathf.FloorToInt(_matchTimer / 60f);
            int seconds = Mathf.FloorToInt(_matchTimer % 60f);
            timerText.text = string.Format(timerFormat, minutes, seconds);
        }

        private void UpdateCrosshair()
        {
            if (crosshairImage == null || _cam == null) return;

            var ray = new Ray(_cam.transform.position, _cam.transform.forward);
            bool isInteractable = UnityEngine.Physics.Raycast(ray, out var hit, crosshairRayDistance)
                                  && hit.collider.CompareTag("Interactable");

            crosshairImage.color = Color.Lerp(
                crosshairImage.color,
                isInteractable ? crosshairInteractable : crosshairDefault,
                Time.deltaTime * 10f
            );
        }

        private void UpdateFPSCounter()
        {
            if (fpsText == null) return;

            _frameCount++;
            _fpsTimer += Time.unscaledDeltaTime;

            if (_fpsTimer >= fpsUpdateInterval)
            {
                float fps = _frameCount / _fpsTimer;
                fpsText.text = $"{fps:F0} FPS";
                _frameCount = 0;
                _fpsTimer = 0f;
            }
        }

        public void ShowNotification(string message, float duration = 3f)
        {
            _notificationQueue.Enqueue(new NotificationEntry { Message = message, Duration = duration });
            if (!_isShowingNotification)
                StartCoroutine(ProcessNotificationQueue());
        }

        private IEnumerator ProcessNotificationQueue()
        {
            _isShowingNotification = true;

            while (_notificationQueue.Count > 0)
            {
                var entry = _notificationQueue.Dequeue();
                yield return StartCoroutine(ShowNotificationCoroutine(entry.Message, entry.Duration));
            }

            _isShowingNotification = false;
        }

        private IEnumerator ShowNotificationCoroutine(string message, float duration)
        {
            if (notificationText == null || notificationCanvasGroup == null) yield break;

            notificationText.text = message;

            float elapsed = 0f;
            while (elapsed < notificationFadeDuration)
            {
                elapsed += Time.deltaTime;
                notificationCanvasGroup.alpha = elapsed / notificationFadeDuration;
                yield return null;
            }
            notificationCanvasGroup.alpha = 1f;

            yield return new WaitForSeconds(duration);

            elapsed = 0f;
            while (elapsed < notificationFadeDuration)
            {
                elapsed += Time.deltaTime;
                notificationCanvasGroup.alpha = 1f - elapsed / notificationFadeDuration;
                yield return null;
            }
            notificationCanvasGroup.alpha = 0f;
        }

        public void ResetTimer()
        {
            _matchTimer = 0f;
        }

        public void SetTimerRunning(bool running)
        {
            _timerRunning = running;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }
}
