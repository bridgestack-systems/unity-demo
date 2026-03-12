using System.Collections.Generic;
using UnityEngine;

#if UNITY_XR_ARFOUNDATION
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#endif

namespace NexusArena.XR
{
    public class ARPlacementController : MonoBehaviour
    {
        [Header("Placement")]
        [SerializeField] private GameObject arenaModelPrefab;
        [SerializeField] private GameObject reticlePrefab;
        [SerializeField] private LayerMask placementLayerMask = ~0;

        [Header("Scale / Rotation Limits")]
        [SerializeField] private float minScale = 0.1f;
        [SerializeField] private float maxScale = 3f;
        [SerializeField] private float rotationSpeed = 50f;

        [Header("UI")]
        [SerializeField] private UnityEngine.UI.Button lockButton;
        [SerializeField] private UnityEngine.UI.Button resetButton;

        public bool IsPlacementLocked { get; private set; }
        public bool HasPlacedObject => placedObject != null;

        private GameObject placedObject;
        private GameObject reticleInstance;
        private bool reticleVisible;

#if UNITY_XR_ARFOUNDATION
        private ARRaycastManager arRaycastManager;
        private ARSession arSession;
        private static readonly List<ARRaycastHit> arHits = new();
#endif

        private float initialPinchDistance;
        private float initialScale;
        private float initialTwistAngle;
        private float initialRotationY;

        private void Awake()
        {
#if UNITY_XR_ARFOUNDATION
            arRaycastManager = FindFirstObjectByType<ARRaycastManager>();
            arSession = FindFirstObjectByType<ARSession>();
#endif

            if (reticlePrefab != null)
            {
                reticleInstance = Instantiate(reticlePrefab);
                reticleInstance.SetActive(false);
            }

            lockButton?.onClick.AddListener(ToggleLock);
            resetButton?.onClick.AddListener(ResetPlacement);
        }

        private void Update()
        {
            if (IsPlacementLocked) return;

#if UNITY_XR_ARFOUNDATION
            if (!IsARSessionReady()) return;
#endif

            UpdateReticle();
            HandlePlacementInput();
            HandleScaleRotateInput();
        }

        private void UpdateReticle()
        {
            if (placedObject != null)
            {
                SetReticleVisible(false);
                return;
            }

            if (TryGetPlacementPose(out Pose pose))
            {
                SetReticleVisible(true);
                if (reticleInstance != null)
                {
                    reticleInstance.transform.position = pose.position;
                    reticleInstance.transform.rotation = pose.rotation;
                }
            }
            else
            {
                SetReticleVisible(false);
            }
        }

        private void HandlePlacementInput()
        {
            if (Input.touchCount != 1) return;
            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began) return;

            if (IsOverUI(touch.position)) return;

            if (placedObject == null)
            {
                if (TryGetPlacementPose(out Pose pose))
                    PlaceObject(pose);
            }
        }

        private void HandleScaleRotateInput()
        {
            if (placedObject == null || Input.touchCount < 2) return;

            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                initialPinchDistance = Vector2.Distance(t0.position, t1.position);
                initialScale = placedObject.transform.localScale.x;

                Vector2 dir = t1.position - t0.position;
                initialTwistAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                initialRotationY = placedObject.transform.eulerAngles.y;
            }
            else if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
            {
                // Pinch to scale
                float currentDistance = Vector2.Distance(t0.position, t1.position);
                if (initialPinchDistance > 0.01f)
                {
                    float scaleRatio = currentDistance / initialPinchDistance;
                    float newScale = Mathf.Clamp(initialScale * scaleRatio, minScale, maxScale);
                    placedObject.transform.localScale = Vector3.one * newScale;
                }

                // Twist to rotate
                Vector2 currentDir = t1.position - t0.position;
                float currentAngle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;
                float angleDelta = currentAngle - initialTwistAngle;
                float newRotY = initialRotationY - angleDelta;
                Vector3 euler = placedObject.transform.eulerAngles;
                placedObject.transform.eulerAngles = new Vector3(euler.x, newRotY, euler.z);
            }
        }

        private void PlaceObject(Pose pose)
        {
            if (arenaModelPrefab == null) return;

            placedObject = Instantiate(arenaModelPrefab, pose.position, pose.rotation);
            SetReticleVisible(false);
        }

        public void ToggleLock()
        {
            IsPlacementLocked = !IsPlacementLocked;
        }

        public void ResetPlacement()
        {
            if (placedObject != null)
            {
                Destroy(placedObject);
                placedObject = null;
            }
            IsPlacementLocked = false;
        }

        private bool TryGetPlacementPose(out Pose pose)
        {
            pose = default;

#if UNITY_XR_ARFOUNDATION
            if (arRaycastManager != null)
            {
                Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
                if (arRaycastManager.Raycast(screenCenter, arHits, TrackableType.PlaneWithinPolygon))
                {
                    pose = arHits[0].pose;
                    return true;
                }
            }
#endif

            // Editor fallback: raycast from camera center
            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
                if (UnityEngine.Physics.Raycast(ray, out RaycastHit hit, 100f, placementLayerMask))
                {
                    pose = new Pose(hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
                    return true;
                }
            }

            return false;
        }

        private void SetReticleVisible(bool visible)
        {
            if (reticleVisible == visible) return;
            reticleVisible = visible;
            reticleInstance?.SetActive(visible);
        }

#if UNITY_XR_ARFOUNDATION
        private bool IsARSessionReady()
        {
            return arSession != null && ARSession.state >= ARSessionState.SessionTracking;
        }
#endif

        private static bool IsOverUI(Vector2 screenPosition)
        {
            return UnityEngine.EventSystems.EventSystem.current != null &&
                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }

        private void OnDestroy()
        {
            if (reticleInstance != null)
                Destroy(reticleInstance);
        }
    }
}
