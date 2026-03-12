using UnityEngine;

namespace NexusArena.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerIK : MonoBehaviour
    {
        [Header("Foot IK")]
        [SerializeField] private bool enableFootIK = true;
        [SerializeField] private float footRaycastDistance = 1.2f;
        [SerializeField] private float footOffsetY = 0.05f;
        [SerializeField] private float footIKWeightSpeed = 8f;
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Header("Hand IK")]
        [SerializeField] private bool enableHandIK;
        [SerializeField] private Transform leftHandTarget;
        [SerializeField] private Transform rightHandTarget;
        [SerializeField] private float handIKWeightSpeed = 10f;

        [Header("Look IK")]
        [SerializeField] private bool enableLookIK = true;
        [SerializeField] private Transform lookTarget;
        [SerializeField] private float lookWeight = 0.6f;
        [SerializeField] private float bodyWeight = 0.3f;
        [SerializeField] private float headWeight = 0.8f;
        [SerializeField] private float eyesWeight = 1f;
        [SerializeField] private float clampWeight = 0.5f;
        [SerializeField] private float lookIKWeightSpeed = 5f;

        private Animator _animator;
        private float _leftFootWeight;
        private float _rightFootWeight;
        private float _leftHandWeight;
        private float _rightHandWeight;
        private float _currentLookWeight;

        private float _targetLeftFootWeight;
        private float _targetRightFootWeight;
        private float _targetLeftHandWeight;
        private float _targetRightHandWeight;
        private float _targetLookWeight;

        private Camera _mainCamera;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            _leftFootWeight = Mathf.MoveTowards(_leftFootWeight, _targetLeftFootWeight, footIKWeightSpeed * Time.deltaTime);
            _rightFootWeight = Mathf.MoveTowards(_rightFootWeight, _targetRightFootWeight, footIKWeightSpeed * Time.deltaTime);
            _leftHandWeight = Mathf.MoveTowards(_leftHandWeight, _targetLeftHandWeight, handIKWeightSpeed * Time.deltaTime);
            _rightHandWeight = Mathf.MoveTowards(_rightHandWeight, _targetRightHandWeight, handIKWeightSpeed * Time.deltaTime);
            _currentLookWeight = Mathf.MoveTowards(_currentLookWeight, _targetLookWeight, lookIKWeightSpeed * Time.deltaTime);

            _targetLeftFootWeight = enableFootIK ? 1f : 0f;
            _targetRightFootWeight = enableFootIK ? 1f : 0f;
            _targetLeftHandWeight = enableHandIK && leftHandTarget != null ? 1f : 0f;
            _targetRightHandWeight = enableHandIK && rightHandTarget != null ? 1f : 0f;
            _targetLookWeight = enableLookIK ? 1f : 0f;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null)
                return;

            ProcessFootIK();
            ProcessHandIK();
            ProcessLookIK();
        }

        private void ProcessFootIK()
        {
            SolveFootIK(AvatarIKGoal.LeftFoot, _leftFootWeight);
            SolveFootIK(AvatarIKGoal.RightFoot, _rightFootWeight);
        }

        private void SolveFootIK(AvatarIKGoal foot, float weight)
        {
            if (weight < 0.01f)
            {
                _animator.SetIKPositionWeight(foot, 0f);
                _animator.SetIKRotationWeight(foot, 0f);
                return;
            }

            _animator.SetIKPositionWeight(foot, weight);
            _animator.SetIKRotationWeight(foot, weight);

            Vector3 footPosition = _animator.GetIKPosition(foot);
            Vector3 rayOrigin = footPosition + Vector3.up * 0.5f;

            if (UnityEngine.Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, footRaycastDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 targetPosition = hit.point;
                targetPosition.y += footOffsetY;
                _animator.SetIKPosition(foot, targetPosition);

                Quaternion footRotation = Quaternion.LookRotation(
                    Vector3.ProjectOnPlane(transform.forward, hit.normal),
                    hit.normal
                );
                _animator.SetIKRotation(foot, footRotation);
            }
        }

        private void ProcessHandIK()
        {
            SolveHandIK(AvatarIKGoal.LeftHand, leftHandTarget, _leftHandWeight);
            SolveHandIK(AvatarIKGoal.RightHand, rightHandTarget, _rightHandWeight);
        }

        private void SolveHandIK(AvatarIKGoal hand, Transform target, float weight)
        {
            if (target == null || weight < 0.01f)
            {
                _animator.SetIKPositionWeight(hand, 0f);
                _animator.SetIKRotationWeight(hand, 0f);
                return;
            }

            _animator.SetIKPositionWeight(hand, weight);
            _animator.SetIKRotationWeight(hand, weight);
            _animator.SetIKPosition(hand, target.position);
            _animator.SetIKRotation(hand, target.rotation);
        }

        private void ProcessLookIK()
        {
            float weight = _currentLookWeight * lookWeight;

            if (weight < 0.01f)
            {
                _animator.SetLookAtWeight(0f);
                return;
            }

            Vector3 lookPosition = GetLookPosition();
            _animator.SetLookAtWeight(weight, bodyWeight, headWeight, eyesWeight, clampWeight);
            _animator.SetLookAtPosition(lookPosition);
        }

        private Vector3 GetLookPosition()
        {
            if (lookTarget != null)
                return lookTarget.position;

            if (_mainCamera != null)
            {
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (UnityEngine.Physics.Raycast(ray, out RaycastHit hit, 100f))
                    return hit.point;

                return ray.GetPoint(20f);
            }

            return transform.position + transform.forward * 10f;
        }

        public void SetLookTarget(Transform target)
        {
            lookTarget = target;
        }

        public void SetHandTargets(Transform leftTarget, Transform rightTarget)
        {
            leftHandTarget = leftTarget;
            rightHandTarget = rightTarget;
        }

        public void SetFootIKEnabled(bool enabled) => enableFootIK = enabled;
        public void SetHandIKEnabled(bool enabled) => enableHandIK = enabled;
        public void SetLookIKEnabled(bool enabled) => enableLookIK = enabled;
    }
}
