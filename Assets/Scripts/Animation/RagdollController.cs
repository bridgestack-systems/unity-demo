using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NexusArena.Animation
{
    [RequireComponent(typeof(Animator))]
    public class RagdollController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float blendToAnimationDuration = 0.5f;
        [SerializeField] private float getUpDelay = 1.5f;

        [Header("Get Up Animations")]
        [SerializeField] private string getUpFromFrontTrigger = "GetUpFront";
        [SerializeField] private string getUpFromBackTrigger = "GetUpBack";

        public bool IsRagdolled { get; private set; }
        public bool IsBlending { get; private set; }

        private Animator _animator;
        private CharacterController _characterController;
        private Rigidbody[] _ragdollBodies;
        private Collider[] _ragdollColliders;
        private List<BoneTransformSnapshot> _ragdollSnapshot;

        private struct BoneTransformSnapshot
        {
            public Transform Transform;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _characterController = GetComponent<CharacterController>();
            CacheRagdollComponents();
            DisableRagdoll();
        }

        private void CacheRagdollComponents()
        {
            _ragdollBodies = GetComponentsInChildren<Rigidbody>();
            _ragdollColliders = GetComponentsInChildren<Collider>();
            _ragdollSnapshot = new List<BoneTransformSnapshot>(_ragdollBodies.Length);
        }

        public void EnableRagdoll()
        {
            if (IsRagdolled)
                return;

            IsRagdolled = true;
            _animator.enabled = false;

            if (_characterController != null)
                _characterController.enabled = false;

            foreach (var rb in _ragdollBodies)
            {
                if (rb.gameObject == gameObject)
                    continue;

                rb.isKinematic = false;
                rb.detectCollisions = true;
                rb.useGravity = true;
            }

            foreach (var col in _ragdollColliders)
            {
                if (col.gameObject == gameObject)
                    continue;

                col.enabled = true;
            }
        }

        public void DisableRagdoll()
        {
            IsRagdolled = false;

            foreach (var rb in _ragdollBodies)
            {
                if (rb.gameObject == gameObject)
                    continue;

                rb.isKinematic = true;
                rb.detectCollisions = false;
                rb.useGravity = false;
            }

            foreach (var col in _ragdollColliders)
            {
                if (col.gameObject == gameObject)
                    continue;

                col.enabled = false;
            }

            _animator.enabled = true;

            if (_characterController != null)
                _characterController.enabled = true;
        }

        public void ApplyForce(Vector3 force, Vector3 point)
        {
            if (!IsRagdolled)
                EnableRagdoll();

            foreach (var rb in _ragdollBodies)
            {
                if (rb.gameObject == gameObject)
                    continue;

                rb.AddExplosionForce(force.magnitude, point, 5f, 0.5f, ForceMode.Impulse);
            }
        }

        public void ApplyDirectionalForce(Vector3 force)
        {
            if (!IsRagdolled)
                EnableRagdoll();

            foreach (var rb in _ragdollBodies)
            {
                if (rb.gameObject == gameObject)
                    continue;

                rb.AddForce(force, ForceMode.Impulse);
            }
        }

        public void BlendToAnimation()
        {
            if (!IsRagdolled || IsBlending)
                return;

            StartCoroutine(BlendToAnimationCoroutine());
        }

        public void GetUp()
        {
            if (!IsRagdolled || IsBlending)
                return;

            StartCoroutine(GetUpCoroutine());
        }

        private IEnumerator GetUpCoroutine()
        {
            yield return new WaitForSeconds(getUpDelay);

            SnapshotBoneTransforms();

            bool isFaceDown = IsFaceDown();
            string triggerName = isFaceDown ? getUpFromFrontTrigger : getUpFromBackTrigger;

            AlignTransformToRagdoll();
            DisableRagdoll();

            yield return StartCoroutine(BlendBonesCoroutine());

            _animator.SetTrigger(Animator.StringToHash(triggerName));
        }

        private IEnumerator BlendToAnimationCoroutine()
        {
            IsBlending = true;

            SnapshotBoneTransforms();
            AlignTransformToRagdoll();
            DisableRagdoll();

            yield return StartCoroutine(BlendBonesCoroutine());

            IsBlending = false;
        }

        private IEnumerator BlendBonesCoroutine()
        {
            float elapsed = 0f;

            while (elapsed < blendToAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / blendToAnimationDuration);

                for (int i = 0; i < _ragdollSnapshot.Count; i++)
                {
                    var snapshot = _ragdollSnapshot[i];
                    if (snapshot.Transform == null)
                        continue;

                    snapshot.Transform.localPosition = Vector3.Lerp(
                        snapshot.Position,
                        _animator.GetBoneTransform(GetBoneForTransform(snapshot.Transform))?.localPosition ?? snapshot.Transform.localPosition,
                        t
                    );
                    snapshot.Transform.localRotation = Quaternion.Slerp(
                        snapshot.Rotation,
                        _animator.GetBoneTransform(GetBoneForTransform(snapshot.Transform))?.localRotation ?? snapshot.Transform.localRotation,
                        t
                    );
                }

                yield return null;
            }
        }

        private void SnapshotBoneTransforms()
        {
            _ragdollSnapshot.Clear();

            foreach (var rb in _ragdollBodies)
            {
                if (rb.gameObject == gameObject)
                    continue;

                _ragdollSnapshot.Add(new BoneTransformSnapshot
                {
                    Transform = rb.transform,
                    Position = rb.transform.localPosition,
                    Rotation = rb.transform.localRotation
                });
            }
        }

        private void AlignTransformToRagdoll()
        {
            Transform hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null)
                return;

            Vector3 hipPosition = hips.position;
            Vector3 originalHipsLocalPos = hips.localPosition;

            transform.position = new Vector3(hipPosition.x, transform.position.y, hipPosition.z);

            if (UnityEngine.Physics.Raycast(hipPosition, Vector3.down, out RaycastHit hit, 5f))
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);

            hips.localPosition = originalHipsLocalPos;
        }

        private bool IsFaceDown()
        {
            Transform hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null)
                return false;

            return Vector3.Dot(hips.forward, Vector3.down) > 0f;
        }

        private HumanBodyBones GetBoneForTransform(Transform boneTransform)
        {
            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var bone = (HumanBodyBones)i;
                if (_animator.GetBoneTransform(bone) == boneTransform)
                    return bone;
            }
            return HumanBodyBones.Hips;
        }
    }
}
