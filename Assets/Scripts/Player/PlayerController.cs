using UnityEngine;
using UnityEngine.InputSystem;
using NexusArena.Core;
using NexusArena.Physics;

namespace NexusArena.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private float rotationSmoothTime = 0.12f;
        [SerializeField] private float sprintMultiplier = 1.6f;

        [Header("Jump & Gravity")]
        [SerializeField] private float groundCheckRadius = 0.28f;
        [SerializeField] private float groundCheckOffset = -0.14f;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float gravity = -15f;
        [SerializeField] private float fallMultiplier = 2.5f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("References")]
        [SerializeField] private ThirdPersonCamera thirdPersonCamera;

        public bool IsGrounded { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsSprinting { get; private set; }
        public Vector3 Velocity { get; private set; }
        public float VerticalVelocity => _verticalVelocity;

        private CharacterController _controller;
        private Transform _cameraTransform;

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _sprintHeld;
        private bool _jumpRequested;

        private float _verticalVelocity;
        private float _rotationVelocity;
        private float _lastGroundedTime;
        private float _lastJumpPressedTime;
        private bool _hasJumped;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
                return;

            GroundCheck();
            ApplyGravity();
            HandleJump();
            HandleMovement();

            Velocity = _controller.velocity;
        }

        private void GroundCheck()
        {
            Vector3 spherePosition = transform.position + Vector3.up * groundCheckOffset;
            bool wasGrounded = IsGrounded;
            IsGrounded = UnityEngine.Physics.SphereCast(
                spherePosition + Vector3.up * groundCheckRadius,
                groundCheckRadius,
                Vector3.down,
                out _,
                groundCheckRadius * 2f,
                groundLayers,
                QueryTriggerInteraction.Ignore
            );

            if (IsGrounded)
            {
                _lastGroundedTime = Time.time;
                _hasJumped = false;
            }
        }

        private void ApplyGravity()
        {
            if (IsGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                float multiplier = _verticalVelocity < 0f ? fallMultiplier : 1f;
                _verticalVelocity += gravity * multiplier * Time.deltaTime;
            }
        }

        private void HandleJump()
        {
            bool withinCoyoteTime = Time.time - _lastGroundedTime <= coyoteTime;
            bool withinJumpBuffer = Time.time - _lastJumpPressedTime <= jumpBufferTime;

            if (withinJumpBuffer && withinCoyoteTime && !_hasJumped)
            {
                float jumpForce = gameConfig != null ? gameConfig.jumpForce : 5f;
                _verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
                _hasJumped = true;
                _jumpRequested = false;
            }
        }

        private void HandleMovement()
        {
            float speed = gameConfig != null ? gameConfig.moveSpeed : 5f;
            IsSprinting = _sprintHeld && _moveInput.sqrMagnitude > 0.01f;

            if (IsSprinting)
                speed *= sprintMultiplier;

            IsMoving = _moveInput.sqrMagnitude > 0.01f;

            Vector3 moveDirection = Vector3.zero;

            if (IsMoving && _cameraTransform != null)
            {
                Vector3 forward = _cameraTransform.forward;
                Vector3 right = _cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                moveDirection = forward * _moveInput.y + right * _moveInput.x;
                moveDirection.Normalize();

                float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    targetAngle,
                    ref _rotationVelocity,
                    rotationSmoothTime
                );
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            Vector3 horizontalMovement = moveDirection * speed;
            Vector3 totalMovement = new Vector3(horizontalMovement.x, _verticalVelocity, horizontalMovement.z);
            _controller.Move(totalMovement * Time.deltaTime);
        }

        private void HandleInteract()
        {
            float grabRange = gameConfig != null ? gameConfig.grabRange : 3f;
            Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);

            if (UnityEngine.Physics.Raycast(ray, out RaycastHit hit, grabRange))
            {
                if (hit.collider.CompareTag("Interactable"))
                {
                    var grabAndThrow = hit.collider.GetComponent<GrabAndThrow>();
                    grabAndThrow?.OnGrab();
                }
            }
        }

        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }

        public void OnJump(InputValue value)
        {
            if (value.isPressed)
            {
                _jumpRequested = true;
                _lastJumpPressedTime = Time.time;
            }
        }

        public void OnSprint(InputValue value)
        {
            _sprintHeld = value.isPressed;
        }

        public void OnInteract(InputValue value)
        {
            if (value.isPressed)
                HandleInteract();
        }

        public void OnLook(InputValue value)
        {
            _lookInput = value.Get<Vector2>();
            thirdPersonCamera?.HandleLookInput(_lookInput);
        }
    }
}
