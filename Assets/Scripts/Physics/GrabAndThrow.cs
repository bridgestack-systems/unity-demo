using UnityEngine;
using UnityEngine.InputSystem;
using NexusArena.Core;

namespace NexusArena.Physics
{
    public class GrabAndThrow : MonoBehaviour
    {
        [Header("Grab Settings")]
        [SerializeField] private Transform holdPoint;
        [SerializeField] private float grabRange = 5f;
        [SerializeField] private float holdSmoothing = 15f;
        [SerializeField] private LayerMask grabLayerMask = ~0;

        [Header("Throw Settings")]
        [SerializeField] private float throwForce = 20f;
        [SerializeField] private float throwUpwardBias = 0.15f;

        [Header("Visual Indicator")]
        [SerializeField] private Color highlightColor = new(1f, 0.9f, 0.3f, 1f);
        [SerializeField] private float highlightEmissionIntensity = 2f;

        private Camera _cam;
        private Rigidbody _heldRb;
        private GameObject _heldObject;
        private GameObject _hoveredObject;
        private Material[] _hoveredOriginalMaterials;
        private bool _isHolding;

        private InputAction _grabAction;
        private InputAction _throwAction;

        private void Awake()
        {
            _cam = Camera.main;

            _grabAction = new InputAction("Grab", InputActionType.Button, "<Mouse>/leftButton");
            _throwAction = new InputAction("Throw", InputActionType.Button, "<Mouse>/rightButton");

            _grabAction.performed += _ => OnGrab();
            _throwAction.performed += _ => OnThrow();
        }

        private void OnEnable()
        {
            _grabAction.Enable();
            _throwAction.Enable();
        }

        private void OnDisable()
        {
            _grabAction.Disable();
            _throwAction.Disable();
            if (_isHolding) Drop();
        }

        private void Update()
        {
            if (_isHolding)
            {
                UpdateHeldObject();
            }
            else
            {
                UpdateHoverHighlight();
            }
        }

        public void OnGrab()
        {
            if (_isHolding)
            {
                Drop();
                return;
            }

            if (!TryGetGrabbable(out var hit)) return;

            var rb = hit.collider.attachedRigidbody;
            if (rb == null) return;

            _heldObject = rb.gameObject;
            _heldRb = rb;
            _heldRb.isKinematic = true;
            _heldRb.interpolation = RigidbodyInterpolation.Interpolate;
            _heldObject.transform.SetParent(holdPoint);
            _isHolding = true;

            ClearHoverHighlight();
        }

        private void OnThrow()
        {
            if (!_isHolding) return;

            Vector3 direction = _cam.transform.forward + Vector3.up * throwUpwardBias;
            Release();
            _heldRb.AddForce(direction.normalized * throwForce, ForceMode.Impulse);
            _heldRb = null;
            _heldObject = null;
        }

        public void Drop()
        {
            if (!_isHolding) return;
            Release();
            _heldRb = null;
            _heldObject = null;
        }

        private void Release()
        {
            _heldObject.transform.SetParent(null);
            _heldRb.isKinematic = false;
            _isHolding = false;
        }

        private void UpdateHeldObject()
        {
            if (_heldObject == null)
            {
                _isHolding = false;
                return;
            }

            _heldObject.transform.localPosition = Vector3.Lerp(
                _heldObject.transform.localPosition,
                Vector3.zero,
                Time.deltaTime * holdSmoothing
            );

            _heldObject.transform.localRotation = Quaternion.Slerp(
                _heldObject.transform.localRotation,
                Quaternion.identity,
                Time.deltaTime * holdSmoothing
            );
        }

        private void UpdateHoverHighlight()
        {
            if (TryGetGrabbable(out var hit))
            {
                var target = hit.collider.attachedRigidbody?.gameObject;
                if (target != null && target != _hoveredObject)
                {
                    ClearHoverHighlight();
                    _hoveredObject = target;
                    ApplyHoverHighlight();
                }
            }
            else
            {
                ClearHoverHighlight();
            }
        }

        private bool TryGetGrabbable(out RaycastHit hit)
        {
            var ray = new Ray(_cam.transform.position, _cam.transform.forward);
            if (UnityEngine.Physics.Raycast(ray, out hit, grabRange, grabLayerMask))
            {
                return hit.collider.CompareTag("Interactable") &&
                       hit.collider.attachedRigidbody != null;
            }
            return false;
        }

        private void ApplyHoverHighlight()
        {
            if (_hoveredObject == null) return;

            var renderers = _hoveredObject.GetComponentsInChildren<Renderer>();
            _hoveredOriginalMaterials = new Material[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                _hoveredOriginalMaterials[i] = renderers[i].material;
                var mat = renderers[i].material;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", highlightColor * highlightEmissionIntensity);
            }
        }

        private void ClearHoverHighlight()
        {
            if (_hoveredObject == null) return;

            var renderers = _hoveredObject.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length && i < _hoveredOriginalMaterials.Length; i++)
            {
                renderers[i].material = _hoveredOriginalMaterials[i];
            }

            _hoveredObject = null;
            _hoveredOriginalMaterials = null;
        }
    }
}
