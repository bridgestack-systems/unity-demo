using UnityEngine;

#if UNITY_XR_INTERACTION_TOOLKIT
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
#endif

namespace NexusArena.XR
{
    public class VRPlayerRig : MonoBehaviour
    {
        [Header("Rig References")]
        [SerializeField] private Transform xrOrigin;
        [SerializeField] private Transform headTransform;
        [SerializeField] private Transform leftControllerTransform;
        [SerializeField] private Transform rightControllerTransform;
        [SerializeField] private Camera vrCamera;

        [Header("Locomotion")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float snapTurnAngle = 45f;
        [SerializeField] private bool enableContinuousMove = true;
        [SerializeField] private bool enableTeleport = true;

        [Header("Comfort")]
        [SerializeField] private bool enableVignette = true;
        [SerializeField] [Range(0f, 1f)] private float vignetteIntensity = 0.5f;
        [SerializeField] private Material vignetteMaterial;

        [Header("Boundary")]
        [SerializeField] private bool showBoundary = true;
        [SerializeField] private float boundaryRadius = 3f;
        [SerializeField] private float boundaryWarningDistance = 0.5f;
        [SerializeField] private LineRenderer boundaryRenderer;

        [Header("Calibration")]
        [SerializeField] private float defaultPlayerHeight = 1.7f;

        public float MoveSpeed
        {
            get => moveSpeed;
            set
            {
                moveSpeed = value;
                ApplyMoveSpeed();
            }
        }

        public bool VignetteEnabled
        {
            get => enableVignette;
            set => enableVignette = value;
        }

        private float calibratedHeightOffset;
        private bool isMoving;
        private Vector3 previousPosition;

#if UNITY_XR_INTERACTION_TOOLKIT
        private ContinuousMoveProvider continuousMoveProvider;
        private SnapTurnProvider snapTurnProvider;
        private TeleportationProvider teleportationProvider;
#endif

        private void Awake()
        {
#if UNITY_XR_INTERACTION_TOOLKIT
            continuousMoveProvider = GetComponentInChildren<ContinuousMoveProvider>();
            snapTurnProvider = GetComponentInChildren<SnapTurnProvider>();
            teleportationProvider = GetComponentInChildren<TeleportationProvider>();
#endif

            if (vrCamera == null)
                vrCamera = GetComponentInChildren<Camera>();

            previousPosition = transform.position;
            ApplyLocomotionSettings();
        }

        private void Update()
        {
            UpdateHeadTracking();
            UpdateBoundaryVisualization();
            UpdateVignette();
            previousPosition = transform.position;
        }

        private void UpdateHeadTracking()
        {
            if (headTransform == null || vrCamera == null) return;
            vrCamera.transform.localPosition = headTransform.localPosition + Vector3.up * calibratedHeightOffset;
            vrCamera.transform.localRotation = headTransform.localRotation;
        }

        private void UpdateBoundaryVisualization()
        {
            if (!showBoundary || boundaryRenderer == null || xrOrigin == null) return;

            Vector3 headLocal = headTransform != null
                ? new Vector3(headTransform.localPosition.x, 0f, headTransform.localPosition.z)
                : Vector3.zero;

            float distFromCenter = headLocal.magnitude;
            float warnStart = boundaryRadius - boundaryWarningDistance;

            if (distFromCenter > warnStart)
            {
                float alpha = Mathf.InverseLerp(warnStart, boundaryRadius, distFromCenter);
                DrawBoundaryCircle(alpha);
                boundaryRenderer.enabled = true;
            }
            else
            {
                boundaryRenderer.enabled = false;
            }
        }

        private void DrawBoundaryCircle(float alpha)
        {
            if (boundaryRenderer == null) return;

            const int segments = 64;
            boundaryRenderer.positionCount = segments + 1;
            boundaryRenderer.startColor = new Color(1f, 0.3f, 0.3f, alpha);
            boundaryRenderer.endColor = new Color(1f, 0.3f, 0.3f, alpha);

            Vector3 center = xrOrigin != null ? xrOrigin.position : transform.position;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * boundaryRadius;
                float z = Mathf.Sin(angle) * boundaryRadius;
                boundaryRenderer.SetPosition(i, center + new Vector3(x, 0.01f, z));
            }
        }

        private void UpdateVignette()
        {
            if (!enableVignette || vignetteMaterial == null) return;

            Vector3 velocity = (transform.position - previousPosition) / Time.deltaTime;
            isMoving = velocity.sqrMagnitude > 0.01f;
            float targetIntensity = isMoving ? vignetteIntensity : 0f;
            float current = vignetteMaterial.GetFloat("_VignetteIntensity");
            vignetteMaterial.SetFloat("_VignetteIntensity",
                Mathf.Lerp(current, targetIntensity, Time.deltaTime * 8f));
        }

        public void CalibrateHeight()
        {
            if (headTransform == null) return;
            float currentHeadHeight = headTransform.localPosition.y;
            calibratedHeightOffset = defaultPlayerHeight - currentHeadHeight;
        }

        public void SetHeight(float height)
        {
            defaultPlayerHeight = height;
            CalibrateHeight();
        }

        private void ApplyLocomotionSettings()
        {
#if UNITY_XR_INTERACTION_TOOLKIT
            if (continuousMoveProvider != null)
            {
                continuousMoveProvider.enabled = enableContinuousMove;
                continuousMoveProvider.moveSpeed = moveSpeed;
            }

            if (snapTurnProvider != null)
            {
                snapTurnProvider.turnAmount = snapTurnAngle;
            }

            if (teleportationProvider != null)
            {
                teleportationProvider.enabled = enableTeleport;
            }
#endif
        }

        private void ApplyMoveSpeed()
        {
#if UNITY_XR_INTERACTION_TOOLKIT
            if (continuousMoveProvider != null)
                continuousMoveProvider.moveSpeed = moveSpeed;
#endif
        }

        public void SetContinuousMove(bool enabled)
        {
            enableContinuousMove = enabled;
#if UNITY_XR_INTERACTION_TOOLKIT
            if (continuousMoveProvider != null)
                continuousMoveProvider.enabled = enabled;
#endif
        }

        public void SetTeleport(bool enabled)
        {
            enableTeleport = enabled;
#if UNITY_XR_INTERACTION_TOOLKIT
            if (teleportationProvider != null)
                teleportationProvider.enabled = enabled;
#endif
        }

        public void SetSnapTurnAngle(float angle)
        {
            snapTurnAngle = angle;
#if UNITY_XR_INTERACTION_TOOLKIT
            if (snapTurnProvider != null)
                snapTurnProvider.turnAmount = angle;
#endif
        }

        public Transform GetLeftController() => leftControllerTransform;
        public Transform GetRightController() => rightControllerTransform;
        public Camera GetCamera() => vrCamera;
    }
}
