using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NexusArena.Core
{
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public event Action<GameState> OnGameStateChanged;

        [SerializeField] private GameConfig config;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public int Score { get; private set; }
        public float ElapsedTime { get; private set; }
        public GameConfig Config => config;

        private bool timerRunning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (timerRunning && CurrentState == GameState.Playing)
            {
                ElapsedTime += Time.deltaTime;

                if (config != null && ElapsedTime >= config.roundDuration)
                {
                    SetState(GameState.GameOver);
                }
            }
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;

            switch (newState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    timerRunning = true;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    timerRunning = false;
                    break;
                case GameState.GameOver:
                    timerRunning = false;
                    break;
                case GameState.MainMenu:
                    ResetSession();
                    break;
            }

            OnGameStateChanged?.Invoke(newState);
        }

        public void LoadScene(string sceneName)
        {
            SetState(GameState.Loading);
            SceneManager.LoadScene(sceneName);
        }

        public void AddScore(int points)
        {
            Score += points;
        }

        public void ResetSession()
        {
            Score = 0;
            ElapsedTime = 0f;
            timerRunning = false;
            Time.timeScale = 1f;
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
