using UnityEngine;
using UnityEngine.UI;
using NexusArena.Core;

namespace NexusArena.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private GameObject settingsPanel;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        [Header("Settings")]
        [SerializeField] private string mainMenuScene = "MainMenu";

        private bool _isPaused;
        private float _previousTimeScale;

        private void Start()
        {
            pausePanel?.SetActive(false);
            settingsPanel?.SetActive(false);

            if (overlayCanvasGroup != null)
                overlayCanvasGroup.alpha = 0f;

            resumeButton?.onClick.AddListener(Resume);
            settingsButton?.onClick.AddListener(ToggleSettings);
            mainMenuButton?.onClick.AddListener(ReturnToMainMenu);
            quitButton?.onClick.AddListener(QuitGame);

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    settingsPanel.SetActive(false);
                    return;
                }

                if (_isPaused)
                    Resume();
                else
                    Pause();
            }
        }

        public void Pause()
        {
            if (_isPaused) return;

            _isPaused = true;
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            pausePanel?.SetActive(true);
            if (overlayCanvasGroup != null)
                overlayCanvasGroup.alpha = 0.6f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GameManager.Instance?.SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (!_isPaused) return;

            _isPaused = false;
            Time.timeScale = _previousTimeScale;

            pausePanel?.SetActive(false);
            settingsPanel?.SetActive(false);
            if (overlayCanvasGroup != null)
                overlayCanvasGroup.alpha = 0f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            GameManager.Instance?.SetState(GameState.Playing);
        }

        private void ToggleSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            _isPaused = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameManager.Instance?.LoadScene(mainMenuScene);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.Playing && _isPaused)
                Resume();
        }

        private void OnDestroy()
        {
            resumeButton?.onClick.RemoveListener(Resume);
            settingsButton?.onClick.RemoveListener(ToggleSettings);
            mainMenuButton?.onClick.RemoveListener(ReturnToMainMenu);
            quitButton?.onClick.RemoveListener(QuitGame);

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }
}
