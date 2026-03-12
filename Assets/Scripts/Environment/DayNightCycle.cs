using UnityEngine;
using UnityEngine.Events;

namespace NexusArena.Environment
{
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Sun")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private float dayDurationSeconds = 120f;

        [Header("Light Colors")]
        [SerializeField] private Gradient lightColorGradient;
        [SerializeField] private AnimationCurve lightIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float maxLightIntensity = 1.3f;
        [SerializeField] private float minLightIntensity = 0.1f;

        [Header("Ambient")]
        [SerializeField] private Gradient ambientColorGradient;

        [Header("Skybox")]
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private float skyboxRotationSpeed = 1f;

        [Header("Time")]
        [SerializeField] [Range(0f, 24f)] private float startTimeOfDay = 8f;
        [SerializeField] private float timeMultiplier = 1f;

        [Header("Events")]
        public UnityEvent OnSunrise;
        public UnityEvent OnSunset;
        public UnityEvent OnNoon;
        public UnityEvent OnMidnight;

        private float _currentTime;
        private bool _sunriseFired, _sunsetFired, _noonFired, _midnightFired;

        public float TimeOfDay => _currentTime;
        public float NormalizedTime => _currentTime / 24f;

        private void Start()
        {
            _currentTime = startTimeOfDay;

            if (directionalLight == null)
            {
                directionalLight = RenderSettings.sun;
                if (directionalLight == null)
                {
                    var sunGo = new GameObject("Sun_DirectionalLight");
                    directionalLight = sunGo.AddComponent<Light>();
                    directionalLight.type = LightType.Directional;
                }
            }

            if (lightColorGradient.colorKeys.Length <= 1)
                InitializeDefaultGradients();
        }

        private void Update()
        {
            float hoursPerSecond = 24f / dayDurationSeconds;
            _currentTime += Time.deltaTime * hoursPerSecond * timeMultiplier;
            _currentTime %= 24f;

            UpdateSunRotation();
            UpdateLighting();
            UpdateSkybox();
            CheckTimeEvents();
        }

        private void UpdateSunRotation()
        {
            float sunAngle = (NormalizedTime * 360f) - 90f;
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
        }

        private void UpdateLighting()
        {
            float t = NormalizedTime;
            directionalLight.color = lightColorGradient.Evaluate(t);

            float intensityCurveValue = lightIntensityCurve.Evaluate(t);
            directionalLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, intensityCurveValue);

            RenderSettings.ambientLight = ambientColorGradient.Evaluate(t);
        }

        private void UpdateSkybox()
        {
            if (skyboxMaterial == null) return;
            float rotation = skyboxMaterial.GetFloat("_Rotation");
            skyboxMaterial.SetFloat("_Rotation", rotation + skyboxRotationSpeed * Time.deltaTime);
        }

        private void CheckTimeEvents()
        {
            CheckEvent(6f, 0.5f, ref _sunriseFired, OnSunrise);
            CheckEvent(12f, 0.5f, ref _noonFired, OnNoon);
            CheckEvent(18f, 0.5f, ref _sunsetFired, OnSunset);
            CheckEvent(0f, 0.5f, ref _midnightFired, OnMidnight);

            // Reset flags once out of range
            if (_sunriseFired && Mathf.Abs(_currentTime - 6f) > 1f) _sunriseFired = false;
            if (_noonFired && Mathf.Abs(_currentTime - 12f) > 1f) _noonFired = false;
            if (_sunsetFired && Mathf.Abs(_currentTime - 18f) > 1f) _sunsetFired = false;
            if (_midnightFired && _currentTime > 1f) _midnightFired = false;
        }

        private void CheckEvent(float targetHour, float threshold, ref bool fired, UnityEvent evt)
        {
            if (fired) return;
            if (Mathf.Abs(_currentTime - targetHour) < threshold)
            {
                fired = true;
                evt?.Invoke();
            }
        }

        private void InitializeDefaultGradients()
        {
            lightColorGradient = new Gradient();
            lightColorGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.08f, 0.08f, 0.25f), 0f),      // midnight: dark blue
                    new GradientColorKey(new Color(1f, 0.75f, 0.3f), 0.25f),        // sunrise: warm gold
                    new GradientColorKey(new Color(1f, 0.98f, 0.95f), 0.5f),        // noon: bright white
                    new GradientColorKey(new Color(1f, 0.55f, 0.2f), 0.75f),        // sunset: warm orange
                    new GradientColorKey(new Color(0.08f, 0.08f, 0.25f), 1f)        // midnight: dark blue
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            ambientColorGradient = new Gradient();
            ambientColorGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.03f, 0.03f, 0.12f), 0f),      // midnight: deep blue
                    new GradientColorKey(new Color(0.45f, 0.35f, 0.25f), 0.25f),    // sunrise: warm amber
                    new GradientColorKey(new Color(0.55f, 0.55f, 0.6f), 0.5f),      // noon: neutral bright
                    new GradientColorKey(new Color(0.4f, 0.25f, 0.15f), 0.75f),     // sunset: deep orange
                    new GradientColorKey(new Color(0.03f, 0.03f, 0.12f), 1f)        // midnight: deep blue
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }

        public void SetTimeOfDay(float hour)
        {
            _currentTime = Mathf.Repeat(hour, 24f);
        }
    }
}
