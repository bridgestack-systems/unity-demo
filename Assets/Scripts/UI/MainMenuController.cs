using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using NexusArena.Core;

namespace NexusArena.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button multiplayerButton;
        [SerializeField] private Button arModeButton;
        [SerializeField] private Button vrModeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Title")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private float titlePulseScale = 1.02f;
        [SerializeField] private float titlePulseSpeed = 0.6f;

        [Header("Info")]
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private string versionPrefix = "v";

        [Header("Fade")]
        [SerializeField] private CanvasGroup mainCanvasGroup;
        [SerializeField] private float fadeInDuration = 0.8f;

        [Header("Scenes")]
        [SerializeField] private string gameScene = "GameArena";
        [SerializeField] private string multiplayerScene = "Lobby";
        [SerializeField] private string arScene = "ARScene";
        [SerializeField] private string vrScene = "VRScene";
        [SerializeField] private string settingsScene = "Settings";

        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;

        private Vector3 _titleBaseScale;

        private void Awake()
        {
            if (titleText != null)
                _titleBaseScale = titleText.transform.localScale;

            if (versionText != null)
                versionText.text = $"{versionPrefix}{Application.version}";

            BindButtons();
        }

        private void Start()
        {
            GameManager.Instance?.SetState(GameState.MainMenu);

            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 0f;
                StartCoroutine(FadeIn());
            }

            if (titleText != null)
                StartCoroutine(PulseTitle());
        }

        private void BindButtons()
        {
            playButton?.onClick.AddListener(OnPlay);
            multiplayerButton?.onClick.AddListener(OnMultiplayer);
            arModeButton?.onClick.AddListener(OnARMode);
            vrModeButton?.onClick.AddListener(OnVRMode);
            settingsButton?.onClick.AddListener(OnSettings);
            quitButton?.onClick.AddListener(OnQuit);
        }

        private void OnPlay()
        {
            LoadSceneSafe(gameScene);
        }

        private void OnMultiplayer()
        {
            LoadSceneSafe(multiplayerScene);
        }

        private void OnARMode()
        {
            LoadSceneSafe(arScene);
        }

        private void OnVRMode()
        {
            LoadSceneSafe(vrScene);
        }

        private void LoadSceneSafe(string sceneName)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        private void OnSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(!settingsPanel.activeSelf);
            }
            else
            {
                GameManager.Instance?.LoadScene(settingsScene);
            }
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                mainCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            mainCanvasGroup.alpha = 1f;
        }

        private IEnumerator PulseTitle()
        {
            while (true)
            {
                float t = (Mathf.Sin(Time.time * titlePulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                float scale = Mathf.Lerp(1f, titlePulseScale, t);
                titleText.transform.localScale = _titleBaseScale * scale;
                yield return null;
            }
        }

        private void OnDestroy()
        {
            playButton?.onClick.RemoveListener(OnPlay);
            multiplayerButton?.onClick.RemoveListener(OnMultiplayer);
            arModeButton?.onClick.RemoveListener(OnARMode);
            vrModeButton?.onClick.RemoveListener(OnVRMode);
            settingsButton?.onClick.RemoveListener(OnSettings);
            quitButton?.onClick.RemoveListener(OnQuit);
        }
    }
}
