using UnityEngine;
using Cinemachine;

namespace NexusArena.Player
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CinemachineFreeLook freeLookCamera;
        [SerializeField] private Transform followTarget;

        [Header("Sensitivity")]
        [SerializeField] private float horizontalSensitivity = 300f;
        [SerializeField] private float verticalSensitivity = 2f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minZoomRadius = 1f;
        [SerializeField] private float maxZoomRadius = 10f;

        [Header("Damping")]
        [SerializeField] private float followDamping = 0.1f;

        [Header("Lock-On")]
        [SerializeField] private float lockOnRange = 20f;
        [SerializeField] private LayerMask lockOnLayers = ~0;
        [SerializeField] private float lockOnSmoothSpeed = 5f;

        [Header("Camera Shake")]
        [SerializeField] private CinemachineImpulseSource impulseSource;

        private Transform _lockOnTarget;
        private bool _isLockedOn;
        private Vector2 _lookInput;
        private CinemachineCollider _cinemachineCollider;

        private float _shakeTimer;
        private float _shakeIntensity;
        private Vector3 _originalFollowOffset;

        public Transform LockOnTarget => _lockOnTarget;
        public bool IsLockedOn => _isLockedOn;

        private void Awake()
        {
            if (freeLookCamera == null)
                freeLookCamera = GetComponentInChildren<CinemachineFreeLook>();

            if (freeLookCamera != null)
            {
                _cinemachineCollider = freeLookCamera.GetComponent<CinemachineCollider>();

                if (_cinemachineCollider == null)
                    _cinemachineCollider = freeLookCamera.gameObject.AddComponent<CinemachineCollider>();

                _cinemachineCollider.m_Strategy = CinemachineCollider.ResolutionStrategy.PullCameraForward;
                _cinemachineCollider.m_Damping = 0.2f;

                freeLookCamera.Follow = followTarget;
                freeLookCamera.LookAt = followTarget;

                for (int i = 0; i < 3; i++)
                {
                    freeLookCamera.m_Orbits[i].m_Height = freeLookCamera.m_Orbits[i].m_Height;
                    freeLookCamera.m_Orbits[i].m_Radius = Mathf.Clamp(
                        freeLookCamera.m_Orbits[i].m_Radius,
                        minZoomRadius,
                        maxZoomRadius
                    );
                }
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (freeLookCamera == null)
                return;

            HandleZoom();
            HandleLockOn();
            HandleShake();
        }

        public void HandleLookInput(Vector2 input)
        {
            _lookInput = input;

            if (freeLookCamera == null || _isLockedOn)
                return;

            freeLookCamera.m_XAxis.Value += input.x * horizontalSensitivity * Time.deltaTime;
            freeLookCamera.m_YAxis.Value -= input.y * verticalSensitivity * Time.deltaTime;
        }

        private void HandleZoom()
        {
            float scrollInput = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollInput) < 0.01f)
                return;

            for (int i = 0; i < 3; i++)
            {
                float currentRadius = freeLookCamera.m_Orbits[i].m_Radius;
                float newRadius = currentRadius - scrollInput * zoomSpeed;
                freeLookCamera.m_Orbits[i].m_Radius = Mathf.Clamp(newRadius, minZoomRadius, maxZoomRadius);
            }
        }

        private void HandleLockOn()
        {
            if (!_isLockedOn || _lockOnTarget == null)
                return;

            Vector3 directionToTarget = _lockOnTarget.position - followTarget.position;
            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude > lockOnRange * lockOnRange)
            {
                DisengageLockOn();
                return;
            }

            float targetAngle = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
            freeLookCamera.m_XAxis.Value = Mathf.LerpAngle(
                freeLookCamera.m_XAxis.Value,
                targetAngle,
                lockOnSmoothSpeed * Time.deltaTime
            );
        }

        private void HandleShake()
        {
            if (_shakeTimer <= 0f)
                return;

            _shakeTimer -= Time.deltaTime;
        }

        public void Shake(float intensity, float duration)
        {
            _shakeIntensity = intensity;
            _shakeTimer = duration;

            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse(Vector3.one * intensity);
            }
            else if (freeLookCamera != null)
            {
                var noise = freeLookCamera.GetRig(0).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                if (noise != null)
                {
                    noise.m_AmplitudeGain = intensity;
                    Invoke(nameof(ResetShake), duration);
                }
            }
        }

        private void ResetShake()
        {
            if (freeLookCamera == null) return;

            for (int i = 0; i < 3; i++)
            {
                var noise = freeLookCamera.GetRig(i).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                if (noise != null)
                    noise.m_AmplitudeGain = 0f;
            }
        }

        public void EngageLockOn(Transform target)
        {
            if (target == null) return;

            _lockOnTarget = target;
            _isLockedOn = true;
        }

        public void DisengageLockOn()
        {
            _lockOnTarget = null;
            _isLockedOn = false;
        }

        public void ToggleLockOn()
        {
            if (_isLockedOn)
            {
                DisengageLockOn();
                return;
            }

            Collider[] candidates = UnityEngine.Physics.OverlapSphere(
                followTarget.position,
                lockOnRange,
                lockOnLayers
            );

            Transform bestTarget = null;
            float bestAngle = float.MaxValue;
            Vector3 cameraForward = Camera.main != null ? Camera.main.transform.forward : followTarget.forward;

            foreach (var candidate in candidates)
            {
                if (candidate.transform == followTarget)
                    continue;

                Vector3 dirToCandidate = (candidate.transform.position - followTarget.position).normalized;
                float angle = Vector3.Angle(cameraForward, dirToCandidate);

                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    bestTarget = candidate.transform;
                }
            }

            if (bestTarget != null)
                EngageLockOn(bestTarget);
        }
    }
}
