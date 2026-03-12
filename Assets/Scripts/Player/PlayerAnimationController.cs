using UnityEngine;
using NexusArena.Core;

namespace NexusArena.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private float speedDampTime = 0.1f;
        [SerializeField] private float verticalVelocityDampTime = 0.05f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");

        private Animator _animator;
        private float _currentSpeed;
        private float _speedVelocity;
        private float _currentVerticalVelocity;
        private float _verticalVelSmoothing;
        private bool _wasGrounded;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (playerController == null)
                playerController = GetComponentInParent<PlayerController>();
        }

        private void Update()
        {
            if (playerController == null)
                return;

            UpdateLocomotion();
            UpdateJumpAndFall();
        }

        private void UpdateLocomotion()
        {
            float targetSpeed = playerController.IsMoving
                ? (playerController.IsSprinting ? 2f : 1f)
                : 0f;

            _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedVelocity, speedDampTime);
            _animator.SetFloat(SpeedHash, _currentSpeed);
            _animator.SetBool(IsSprintingHash, playerController.IsSprinting);
        }

        private void UpdateJumpAndFall()
        {
            bool isGrounded = playerController.IsGrounded;
            _animator.SetBool(IsGroundedHash, isGrounded);

            if (!_wasGrounded && isGrounded)
            {
                // Landed
            }
            else if (_wasGrounded && !isGrounded && playerController.VerticalVelocity > 0f)
            {
                _animator.SetTrigger(JumpHash);
            }

            float targetVerticalVel = playerController.VerticalVelocity;
            _currentVerticalVelocity = Mathf.SmoothDamp(
                _currentVerticalVelocity,
                targetVerticalVel,
                ref _verticalVelSmoothing,
                verticalVelocityDampTime
            );
            _animator.SetFloat(VerticalVelocityHash, _currentVerticalVelocity);

            _wasGrounded = isGrounded;
        }

        [SerializeField] private AudioClip footstepClip;

        public void OnFootstep()
        {
            if (!playerController.IsGrounded || footstepClip == null)
                return;

            AudioManager.Instance?.PlaySFX(footstepClip, transform.position, 0.5f);
        }
    }
}
