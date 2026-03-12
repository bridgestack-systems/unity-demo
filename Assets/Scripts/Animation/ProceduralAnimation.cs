using UnityEngine;

namespace NexusArena.Animation
{
    public class ProceduralAnimation : MonoBehaviour
    {
        [Header("Hover")]
        [SerializeField] private bool enableHover;
        [SerializeField] private float hoverAmplitude = 0.25f;
        [SerializeField] private float hoverSpeed = 2f;
        [SerializeField] private float hoverPhaseOffset;

        [Header("Rotate")]
        [SerializeField] private bool enableRotation;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private float rotationSpeed = 90f;

        [Header("Scale Pulse")]
        [SerializeField] private bool enableScalePulse;
        [SerializeField] private float pulseAmplitude = 0.1f;
        [SerializeField] private float pulseSpeed = 1.5f;
        [SerializeField] private Vector3 pulseAxis = Vector3.one;

        [Header("Bounce")]
        [SerializeField] private bool enableBounce;
        [SerializeField] private float bounceHeight = 0.5f;
        [SerializeField] private float bounceSpeed = 3f;
        [SerializeField] private float bounceSquashAmount = 0.1f;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;

        private void Awake()
        {
            _initialPosition = transform.localPosition;
            _initialRotation = transform.localRotation;
            _initialScale = transform.localScale;
        }

        private void Update()
        {
            Vector3 positionOffset = Vector3.zero;
            Quaternion rotationOffset = Quaternion.identity;
            Vector3 scaleMultiplier = Vector3.one;

            if (enableHover)
                positionOffset += CalculateHover();

            if (enableRotation)
                rotationOffset = CalculateRotation();

            if (enableScalePulse)
                scaleMultiplier = Vector3.Scale(scaleMultiplier, CalculateScalePulse());

            if (enableBounce)
            {
                var (bounceOffset, bounceScale) = CalculateBounce();
                positionOffset += bounceOffset;
                scaleMultiplier = Vector3.Scale(scaleMultiplier, bounceScale);
            }

            transform.localPosition = _initialPosition + positionOffset;
            transform.localRotation = _initialRotation * rotationOffset;
            transform.localScale = Vector3.Scale(_initialScale, scaleMultiplier);
        }

        private Vector3 CalculateHover()
        {
            float y = Mathf.Sin((Time.time + hoverPhaseOffset) * hoverSpeed) * hoverAmplitude;
            return new Vector3(0f, y, 0f);
        }

        private Quaternion CalculateRotation()
        {
            float angle = rotationSpeed * Time.time;
            return Quaternion.AngleAxis(angle, rotationAxis.normalized) * Quaternion.Inverse(_initialRotation) * _initialRotation;
        }

        private Vector3 CalculateScalePulse()
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            return Vector3.one + pulseAxis.normalized * (pulse - 1f);
        }

        private (Vector3 offset, Vector3 scale) CalculateBounce()
        {
            float t = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed));
            float height = t * bounceHeight;

            float squashStretch = 1f + (t > 0.9f ? -bounceSquashAmount * (1f - t) * 10f : 0f);
            float inverseSquash = 1f / Mathf.Max(squashStretch, 0.01f);

            Vector3 offset = new Vector3(0f, height, 0f);
            Vector3 scale = new Vector3(
                Mathf.Sqrt(inverseSquash),
                squashStretch,
                Mathf.Sqrt(inverseSquash)
            );

            return (offset, scale);
        }

        public void SetHoverEnabled(bool enabled) => enableHover = enabled;
        public void SetRotationEnabled(bool enabled) => enableRotation = enabled;
        public void SetScalePulseEnabled(bool enabled) => enableScalePulse = enabled;
        public void SetBounceEnabled(bool enabled) => enableBounce = enabled;

        public void ResetToInitial()
        {
            transform.localPosition = _initialPosition;
            transform.localRotation = _initialRotation;
            transform.localScale = _initialScale;
        }

        private void OnDisable()
        {
            ResetToInitial();
        }
    }
}
