using UnityEngine;

#if UNITY_XR_INTERACTION_TOOLKIT
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
#endif

namespace NexusArena.XR
{
    public class VRHandInteraction : MonoBehaviour
    {
        public enum Hand { Left, Right }

        [Header("Configuration")]
        [SerializeField] private Hand hand = Hand.Right;

        [Header("References")]
        [SerializeField] private Transform controllerTransform;
        [SerializeField] private GameObject handModelPrefab;
        [SerializeField] private LineRenderer uiRayRenderer;

        [Header("Grab Settings")]
        [SerializeField] private float grabRadius = 0.1f;
        [SerializeField] private LayerMask grabbableLayerMask = ~0;
        [SerializeField] private float throwForceMultiplier = 1.5f;
        [SerializeField] private int velocitySampleCount = 10;

        [Header("Haptics")]
        [SerializeField] private float grabHapticAmplitude = 0.3f;
        [SerializeField] private float grabHapticDuration = 0.1f;
        [SerializeField] private float throwHapticAmplitude = 0.6f;
        [SerializeField] private float throwHapticDuration = 0.15f;
        [SerializeField] private float impactHapticAmplitude = 0.8f;
        [SerializeField] private float impactHapticDuration = 0.2f;

        [Header("UI Interaction")]
        [SerializeField] private float uiRayMaxDistance = 10f;
        [SerializeField] private LayerMask uiLayerMask;

        private GameObject handModelInstance;
        private Rigidbody grabbedRigidbody;
        private Transform grabbedOriginalParent;
        private bool wasKinematic;

        private Vector3[] velocitySamples;
        private Vector3[] angularVelocitySamples;
        private int velocitySampleIndex;
        private Vector3 previousPosition;
        private Quaternion previousRotation;

#if UNITY_XR_INTERACTION_TOOLKIT
        private XRBaseInteractor interactor;
#endif

        private void Awake()
        {
            velocitySamples = new Vector3[velocitySampleCount];
            angularVelocitySamples = new Vector3[velocitySampleCount];

            if (handModelPrefab != null && controllerTransform != null)
            {
                handModelInstance = Instantiate(handModelPrefab, controllerTransform);
                handModelInstance.transform.localPosition = Vector3.zero;
                handModelInstance.transform.localRotation = Quaternion.identity;
            }

#if UNITY_XR_INTERACTION_TOOLKIT
            interactor = GetComponentInChildren<XRBaseInteractor>();
#endif

            if (controllerTransform != null)
            {
                previousPosition = controllerTransform.position;
                previousRotation = controllerTransform.rotation;
            }
        }

        private void Update()
        {
            if (controllerTransform == null) return;

            SampleVelocity();
            HandleGrabInput();
            UpdateUIRay();

            previousPosition = controllerTransform.position;
            previousRotation = controllerTransform.rotation;
        }

        private void SampleVelocity()
        {
            float dt = Time.deltaTime;
            if (dt < Mathf.Epsilon) return;

            Vector3 velocity = (controllerTransform.position - previousPosition) / dt;
            velocitySamples[velocitySampleIndex] = velocity;

            Quaternion deltaRot = controllerTransform.rotation * Quaternion.Inverse(previousRotation);
            deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            angularVelocitySamples[velocitySampleIndex] = axis * (angle * Mathf.Deg2Rad / dt);

            velocitySampleIndex = (velocitySampleIndex + 1) % velocitySampleCount;
        }

        private void HandleGrabInput()
        {
            bool gripPressed = GetGripPressed();
            bool gripReleased = GetGripReleased();

            if (gripPressed && grabbedRigidbody == null)
                TryGrab();
            else if (gripReleased && grabbedRigidbody != null)
                Release();
        }

        private void TryGrab()
        {
            Collider[] colliders = UnityEngine.Physics.OverlapSphere(controllerTransform.position, grabRadius, grabbableLayerMask);
            if (colliders.Length == 0) return;

            float closestDist = float.MaxValue;
            Rigidbody closestRb = null;

            foreach (var col in colliders)
            {
                var rb = col.attachedRigidbody;
                if (rb == null) continue;

                float dist = Vector3.Distance(controllerTransform.position, col.ClosestPoint(controllerTransform.position));
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestRb = rb;
                }
            }

            if (closestRb == null) return;

            grabbedRigidbody = closestRb;
            grabbedOriginalParent = grabbedRigidbody.transform.parent;
            wasKinematic = grabbedRigidbody.isKinematic;

            grabbedRigidbody.isKinematic = true;
            grabbedRigidbody.transform.SetParent(controllerTransform);

            TriggerHaptic(grabHapticAmplitude, grabHapticDuration);
        }

        private void Release()
        {
            if (grabbedRigidbody == null) return;

            grabbedRigidbody.transform.SetParent(grabbedOriginalParent);
            grabbedRigidbody.isKinematic = wasKinematic;

            if (!wasKinematic)
            {
                Vector3 avgVelocity = ComputeAverageVelocity();
                Vector3 avgAngularVelocity = ComputeAverageAngularVelocity();

                grabbedRigidbody.linearVelocity = avgVelocity * throwForceMultiplier;
                grabbedRigidbody.angularVelocity = avgAngularVelocity;

                if (avgVelocity.sqrMagnitude > 1f)
                    TriggerHaptic(throwHapticAmplitude, throwHapticDuration);
            }

            grabbedRigidbody = null;
            grabbedOriginalParent = null;
        }

        private Vector3 ComputeAverageVelocity()
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < velocitySampleCount; i++)
                sum += velocitySamples[i];
            return sum / velocitySampleCount;
        }

        private Vector3 ComputeAverageAngularVelocity()
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < velocitySampleCount; i++)
                sum += angularVelocitySamples[i];
            return sum / velocitySampleCount;
        }

        private void UpdateUIRay()
        {
            if (uiRayRenderer == null) return;

            Ray ray = new(controllerTransform.position, controllerTransform.forward);
            bool hitUI = UnityEngine.Physics.Raycast(ray, out RaycastHit hit, uiRayMaxDistance, uiLayerMask);

            uiRayRenderer.enabled = true;
            uiRayRenderer.SetPosition(0, controllerTransform.position);
            uiRayRenderer.SetPosition(1, hitUI
                ? hit.point
                : controllerTransform.position + controllerTransform.forward * uiRayMaxDistance);

            if (hitUI && GetTriggerPressed())
            {
                var pointer = hit.collider.GetComponentInParent<UnityEngine.UI.Selectable>();
                if (pointer != null)
                {
                    var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                    {
                        position = Camera.main != null
                            ? (Vector2)Camera.main.WorldToScreenPoint(hit.point)
                            : Vector2.zero
                    };
                    UnityEngine.EventSystems.ExecuteEvents.Execute(
                        pointer.gameObject,
                        pointerData,
                        UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler
                    );
                }
            }
        }

        public void TriggerImpactHaptic()
        {
            TriggerHaptic(impactHapticAmplitude, impactHapticDuration);
        }

        private void TriggerHaptic(float amplitude, float duration)
        {
#if UNITY_XR_INTERACTION_TOOLKIT
            var interactor = controllerTransform?.GetComponent<XRBaseInputInteractor>();
            if (interactor != null)
            {
                interactor.SendHapticImpulse(amplitude, duration);
            }
#else
            // Fallback: use InputSystem or XR directly if XRI is unavailable
            UnityEngine.XR.InputDevice device = default;
            var role = hand == Hand.Left
                ? UnityEngine.XR.InputDeviceRole.LeftHanded
                : UnityEngine.XR.InputDeviceRole.RightHanded;

            var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithRole(role, devices);
            if (devices.Count > 0)
            {
                device = devices[0];
                device.SendHapticImpulse(0, amplitude, duration);
            }
#endif
        }

        private bool GetGripPressed()
        {
            string button = hand == Hand.Left ? "XRI_Left_GripButton" : "XRI_Right_GripButton";
            try { return Input.GetButtonDown(button); }
            catch { return Input.GetKeyDown(hand == Hand.Left ? KeyCode.G : KeyCode.H); }
        }

        private bool GetGripReleased()
        {
            string button = hand == Hand.Left ? "XRI_Left_GripButton" : "XRI_Right_GripButton";
            try { return Input.GetButtonUp(button); }
            catch { return Input.GetKeyUp(hand == Hand.Left ? KeyCode.G : KeyCode.H); }
        }

        private bool GetTriggerPressed()
        {
            string button = hand == Hand.Left ? "XRI_Left_TriggerButton" : "XRI_Right_TriggerButton";
            try { return Input.GetButtonDown(button); }
            catch { return Input.GetMouseButtonDown(0); }
        }

        public void SetHandModel(GameObject prefab)
        {
            if (handModelInstance != null)
                Destroy(handModelInstance);

            if (prefab != null && controllerTransform != null)
            {
                handModelInstance = Instantiate(prefab, controllerTransform);
                handModelInstance.transform.localPosition = Vector3.zero;
                handModelInstance.transform.localRotation = Quaternion.identity;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (controllerTransform == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(controllerTransform.position, grabRadius);
        }
    }
}
