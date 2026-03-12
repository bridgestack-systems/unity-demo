using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexusArena.UI
{
    public class SettingsManager : MonoBehaviour
    {
        [Header("Graphics")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;

        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeLabel;
        [SerializeField] private TMP_Text sfxVolumeLabel;
        [SerializeField] private TMP_Text bgmVolumeLabel;

        [Header("Controls")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Text sensitivityLabel;
        [SerializeField] private Toggle invertYToggle;

        [Header("Buttons")]
        [SerializeField] private Button resetButton;
        [SerializeField] private Button applyButton;

        private Resolution[] _resolutions;

        private const string KeyQuality = "Settings_Quality";
        private const string KeyResolution = "Settings_Resolution";
        private const string KeyFullscreen = "Settings_Fullscreen";
        private const string KeyVSync = "Settings_VSync";
        private const string KeyMasterVolume = "Settings_MasterVolume";
        private const string KeySFXVolume = "Settings_SFXVolume";
        private const string KeyBGMVolume = "Settings_BGMVolume";
        private const string KeySensitivity = "Settings_Sensitivity";
        private const string KeyInvertY = "Settings_InvertY";

        private static class Defaults
        {
            public const int Quality = 2;
            public const bool Fullscreen = true;
            public const bool VSync = true;
            public const float MasterVolume = 0.8f;
            public const float SFXVolume = 0.8f;
            public const float BGMVolume = 0.6f;
            public const float Sensitivity = 1f;
            public const bool InvertY = false;
        }

        private void Awake()
        {
            PopulateResolutions();
            PopulateQualityLevels();
            LoadSettings();
            BindListeners();
        }

        private void PopulateResolutions()
        {
            _resolutions = Screen.resolutions;
            if (resolutionDropdown == null) return;

            resolutionDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            int currentIndex = 0;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                var r = _resolutions[i];
                options.Add($"{r.width} x {r.height} @ {r.refreshRateRatio.value:F0}Hz");

                if (r.width == Screen.currentResolution.width &&
                    r.height == Screen.currentResolution.height)
                {
                    currentIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = PlayerPrefs.GetInt(KeyResolution, currentIndex);
            resolutionDropdown.RefreshShownValue();
        }

        private void PopulateQualityLevels()
        {
            if (qualityDropdown == null) return;

            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.RefreshShownValue();
        }

        private void BindListeners()
        {
            qualityDropdown?.onValueChanged.AddListener(OnQualityChanged);
            resolutionDropdown?.onValueChanged.AddListener(OnResolutionChanged);
            fullscreenToggle?.onValueChanged.AddListener(OnFullscreenChanged);
            vsyncToggle?.onValueChanged.AddListener(OnVSyncChanged);
            masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
            sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);
            bgmVolumeSlider?.onValueChanged.AddListener(OnBGMVolumeChanged);
            sensitivitySlider?.onValueChanged.AddListener(OnSensitivityChanged);
            invertYToggle?.onValueChanged.AddListener(OnInvertYChanged);
            resetButton?.onClick.AddListener(ResetToDefaults);
            applyButton?.onClick.AddListener(SaveSettings);
        }

        private void OnQualityChanged(int index)
        {
            QualitySettings.SetQualityLevel(index, true);
            PlayerPrefs.SetInt(KeyQuality, index);
        }

        private void OnResolutionChanged(int index)
        {
            if (index < 0 || index >= _resolutions.Length) return;
            var r = _resolutions[index];
            Screen.SetResolution(r.width, r.height, Screen.fullScreen);
            PlayerPrefs.SetInt(KeyResolution, index);
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt(KeyFullscreen, isFullscreen ? 1 : 0);
        }

        private void OnVSyncChanged(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            PlayerPrefs.SetInt(KeyVSync, enabled ? 1 : 0);
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(KeyMasterVolume, value);
            if (masterVolumeLabel != null)
                masterVolumeLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat(KeySFXVolume, value);
            if (sfxVolumeLabel != null)
                sfxVolumeLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void OnBGMVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat(KeyBGMVolume, value);
            if (bgmVolumeLabel != null)
                bgmVolumeLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void OnSensitivityChanged(float value)
        {
            PlayerPrefs.SetFloat(KeySensitivity, value);
            if (sensitivityLabel != null)
                sensitivityLabel.text = $"{value:F1}x";
        }

        private void OnInvertYChanged(bool inverted)
        {
            PlayerPrefs.SetInt(KeyInvertY, inverted ? 1 : 0);
        }

        public void LoadSettings()
        {
            if (qualityDropdown != null)
                qualityDropdown.value = PlayerPrefs.GetInt(KeyQuality, Defaults.Quality);

            if (fullscreenToggle != null)
                fullscreenToggle.isOn = PlayerPrefs.GetInt(KeyFullscreen, Defaults.Fullscreen ? 1 : 0) == 1;

            if (vsyncToggle != null)
                vsyncToggle.isOn = PlayerPrefs.GetInt(KeyVSync, Defaults.VSync ? 1 : 0) == 1;

            if (masterVolumeSlider != null)
                masterVolumeSlider.value = PlayerPrefs.GetFloat(KeyMasterVolume, Defaults.MasterVolume);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = PlayerPrefs.GetFloat(KeySFXVolume, Defaults.SFXVolume);

            if (bgmVolumeSlider != null)
                bgmVolumeSlider.value = PlayerPrefs.GetFloat(KeyBGMVolume, Defaults.BGMVolume);

            if (sensitivitySlider != null)
                sensitivitySlider.value = PlayerPrefs.GetFloat(KeySensitivity, Defaults.Sensitivity);

            if (invertYToggle != null)
                invertYToggle.isOn = PlayerPrefs.GetInt(KeyInvertY, Defaults.InvertY ? 1 : 0) == 1;

            ApplyLoadedSettings();
        }

        private void ApplyLoadedSettings()
        {
            QualitySettings.SetQualityLevel(PlayerPrefs.GetInt(KeyQuality, Defaults.Quality), true);
            Screen.fullScreen = PlayerPrefs.GetInt(KeyFullscreen, 1) == 1;
            QualitySettings.vSyncCount = PlayerPrefs.GetInt(KeyVSync, 1) == 1 ? 1 : 0;
            AudioListener.volume = PlayerPrefs.GetFloat(KeyMasterVolume, Defaults.MasterVolume);
        }

        public void SaveSettings()
        {
            PlayerPrefs.Save();
        }

        public void ResetToDefaults()
        {
            if (qualityDropdown != null) qualityDropdown.value = Defaults.Quality;
            if (fullscreenToggle != null) fullscreenToggle.isOn = Defaults.Fullscreen;
            if (vsyncToggle != null) vsyncToggle.isOn = Defaults.VSync;
            if (masterVolumeSlider != null) masterVolumeSlider.value = Defaults.MasterVolume;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = Defaults.SFXVolume;
            if (bgmVolumeSlider != null) bgmVolumeSlider.value = Defaults.BGMVolume;
            if (sensitivitySlider != null) sensitivitySlider.value = Defaults.Sensitivity;
            if (invertYToggle != null) invertYToggle.isOn = Defaults.InvertY;

            SaveSettings();
        }

        public static float GetSensitivity() =>
            PlayerPrefs.GetFloat(KeySensitivity, Defaults.Sensitivity);

        public static bool GetInvertY() =>
            PlayerPrefs.GetInt(KeyInvertY, 0) == 1;

        public static float GetSFXVolume() =>
            PlayerPrefs.GetFloat(KeySFXVolume, Defaults.SFXVolume);

        public static float GetBGMVolume() =>
            PlayerPrefs.GetFloat(KeyBGMVolume, Defaults.BGMVolume);
    }
}
