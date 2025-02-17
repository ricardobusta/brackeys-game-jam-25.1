using System;
using UnityEngine;

namespace Scripts
{
    public class FpsController : MonoBehaviour
    {
        public static FpsController Instance { get; private set; }

        [Header("References")]
        [SerializeField] private FpsInput fpsInput;

        [SerializeField] private Transform HeadTransform;
        [SerializeField] private CharacterController CharacterController;
        [SerializeField] private AudioSource AudioSource;

        [Header("Assets")] 
        [SerializeField] private AudioClip StepSfx;
        [SerializeField] private AudioClip JumpSfx;
        [SerializeField] private AudioClip landSfx;

        [Header("Parameters")] [SerializeField]
        private LayerMask groundLayers;

        public bool gravityPowerUpEnabled = false;
        public bool sprintPowerUpEnabled = false;
        public bool onWater;

        [SerializeField] private float killHeight = -50f;
        [SerializeField] private float accelerationGround = 50f;
        [SerializeField] private float accelerationAir = 8f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float maxSpeedGround = 10f;
        [SerializeField] private float maxSpeedAir = 10f;
        [SerializeField] private float gravityAcceleration = 20f;
        [SerializeField] private float jumpSpeed = 8f;
        [SerializeField] private float groundCheckDistanceGround = 0.05f;
        [SerializeField] private float groundCheckDistanceAir = 0.07f;
        [SerializeField] private float verticalAngleLimit = 89f;
        [SerializeField] private float jumpGroundingDelay = 0.2f;
        [SerializeField] private float sprintModifier = 1.5f;
        [SerializeField] private float landSoundMinDelay = 0.3f;
        [SerializeField] private float walkStepDistance = 3.5f;

        [Header("Modifiers")] [SerializeField] private float gravityPowerUpModifier = 0.5f;
        [SerializeField] private float sprintPowerUpModifier = 1.5f;
        [SerializeField] private float speedWaterModifier = 0.3f;
        [SerializeField] private float jumpWaterModifier = 0.3f;
        [SerializeField] private float gravityWaterModifier = 0.3f;
        
        public bool blockedMovement;
        public bool blockedLook;

        [Header("Read Only")] [SerializeField] private Vector3 _velocity;
        [SerializeField] private float _verticalAngle;
        [SerializeField] private bool _grounded = true;
        [SerializeField] private float _timeSinceNotGrounded;
        [SerializeField] private Vector3 _groundNormal;
        [SerializeField] private float _lastTimeJumped;
        [SerializeField] private Vector3 _latestImpactSpeed;
        [SerializeField] private float _slopeFriction;
        [SerializeField] private float _distanceSinceLastStep;

        private float GetPowerUpGravityModifier => gravityPowerUpEnabled ? gravityPowerUpModifier : 1;
        private float GetGetPowerUpSprintModifier => sprintPowerUpEnabled ? sprintPowerUpModifier : 1;
        private float GetWaterSpeedModifier => onWater ? speedWaterModifier : 1;
        private float GetWaterJumpModifier => onWater ? jumpWaterModifier : 1;
        private float GetWaterGravityModifier => onWater ? gravityWaterModifier : 1;

        private float GetGravityModifier => GetWaterGravityModifier * GetPowerUpGravityModifier;
        private float GetJumpModifier => GetWaterJumpModifier;
        private float GetSpeedModifier => GetWaterSpeedModifier * GetGetPowerUpSprintModifier;

        private void Awake()
        {
            Instance = this;
            _grounded = true;
            _groundNormal = Vector3.up;
            _velocity = Vector3.zero;
            _verticalAngle = 0;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Update()
        {
            var tr = transform;
            if (tr.position.y < killHeight)
            {
                CharacterController.enabled = false; // Character controller do not like being moved around
                CharacterController.transform.position = Vector3.up;
                _velocity = Vector3.zero;
                // ReSharper disable once Unity.InefficientPropertyAccess
                CharacterController.enabled = true;
            }

            HandleInspect();
            HandleGround(tr);
            HandleMovement(tr);
            HandleLook(tr);
        }

        private void HandleInspect()
        {
        }

        private void HandleGround(Transform tr)
        {
            var wasGrounded = _grounded;

            // Actual Ground Check 
            var groundCheckDistance = _grounded
                ? CharacterController.skinWidth + groundCheckDistanceGround
                : groundCheckDistanceAir;

            _grounded = false;
            _groundNormal = Vector3.up;

            if (Time.time > _lastTimeJumped + jumpGroundingDelay)
            {
                var (bottom, top, radius) = GetCapsuleInfo(CharacterController, tr);
                if (Physics.CapsuleCast(bottom, top, radius, Vector3.down, out var hit,
                        groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore))
                {
                    _groundNormal = hit.normal;

                    _slopeFriction = 0;
                    if (IsSlopeGround(_groundNormal))
                    {
                        _grounded = true;

                        if (hit.distance > CharacterController.skinWidth)
                        {
                            CharacterController.Move(Vector3.down * hit.distance); // snap to ground
                        }
                    }
                    else if (IsSlopeStep(bottom, top, radius, groundCheckDistance, out var stepHit))
                    {
                        if (stepHit.distance > CharacterController.skinWidth)
                        {
                            _grounded = true;
                            var move = Vector3.up * (CharacterController.stepOffset - stepHit.distance);
                            CharacterController.Move(move); // snap to ground
                        }
                    }
                    else
                    {
                        // Add slope friction
                        if (_velocity.y > 0)
                        {
                            _slopeFriction = 1 - Vector3.Dot(_groundNormal, Vector3.up);
                            _velocity.y -= _slopeFriction * (accelerationGround * Time.deltaTime);
                        }
                    }
                }
            }

            if (!_grounded && wasGrounded)
            {
                _timeSinceNotGrounded = Time.time;
            }

            // Handle Landing
            if (_grounded && !wasGrounded)
            {
                if (Time.time > _timeSinceNotGrounded + landSoundMinDelay)
                {
                    AudioSource.PlayOneShot(landSfx);
                }
            }
        }

        private bool IsSlopeStep(Vector3 bottom, Vector3 top, float radius, float groundCheckDistance,
            out RaycastHit hit)
        {
            var offset = (Vector3) (transform.localToWorldMatrix * _velocity);
            offset.y = CharacterController.stepOffset;
            // Move capsule in the XZ movement direction, and up the max step size, then try hit test
            if (Physics.CapsuleCast(bottom + offset, top + offset, radius, Vector3.down, out hit,
                    groundCheckDistance + offset.y, groundLayers, QueryTriggerInteraction.Ignore))
            {
                return IsSlopeGround(hit.normal);
            }

            return false;
        }

        private bool IsSlopeGround(Vector3 groundNormal)
        {
            var characterUp = Vector3.up;
            var dot = Vector3.Dot(characterUp, groundNormal);
            if (dot <= 0)
            {
                return false; // it's on the ground or behind the ground
            }

            const double threshold = 1E-15;
            var num = Math.Sqrt(characterUp.sqrMagnitude * (double) groundNormal.sqrMagnitude);
            var angle = num < threshold
                ? 0.0f
                : (float) Math.Acos(Mathf.Clamp(dot / (float) num, -1f, 1f)) * Mathf.Rad2Deg;
            return angle < CharacterController.slopeLimit;
        }

        private void HandleMovement(Transform tr)
        {
            if (blockedMovement) return;

            var moveVector = fpsInput.GetMove();
            var worldMoveInput = tr.TransformVector(new Vector3(moveVector.x, 0, moveVector.y));
            var (beforeMoveBottom, beforeMoveTop, beforeMoveRadius) = GetCapsuleInfo(CharacterController, tr);

            if (_grounded)
            {
                HandleGroundMovement(tr, worldMoveInput, beforeMoveBottom, beforeMoveTop, beforeMoveRadius);
            }
            else
            {
                HandleAirMovement(worldMoveInput);
            }

            CharacterController.Move(_velocity * Time.deltaTime);

            // If movement caused a hit
            _latestImpactSpeed = Vector3.zero;
            if (Physics.CapsuleCast(beforeMoveBottom, beforeMoveTop, beforeMoveRadius, _velocity.normalized,
                    out var hit, _velocity.magnitude * Time.deltaTime, groundLayers, QueryTriggerInteraction.Ignore))
            {
                _latestImpactSpeed = _velocity;

                _velocity = Vector3.ProjectOnPlane(_velocity, hit.normal);
            }
        }

        private void HandleGroundMovement(Transform tr, Vector3 worldMoveInput, Vector3 beforeMoveBottom, Vector3 beforeMoveTop,
            float beforeMoveRadius)
        {
            var isSprinting = fpsInput.GetSprint();
            var speedModifier = isSprinting ? sprintModifier : 1f;
            speedModifier *= GetSpeedModifier;

            // Handle movement on ground
            var targetVelocity = worldMoveInput * (maxSpeedGround * speedModifier);
            targetVelocity = ReorientOnSlope(targetVelocity, _groundNormal, tr);
            _velocity = Vector3.MoveTowards(_velocity, targetVelocity, accelerationGround * Time.deltaTime);

            if (fpsInput.GetJump() && !Physics.CapsuleCast(beforeMoveBottom, beforeMoveTop, beforeMoveRadius,
                    Vector3.up,
                    out var ceilHit, jumpSpeed * GetJumpModifier * Time.deltaTime, groundLayers,
                    QueryTriggerInteraction.Ignore))
            {
                _velocity = new Vector3(_velocity.x, jumpSpeed * GetJumpModifier, _velocity.z);
                _lastTimeJumped = Time.time;
                _grounded = false;
                _groundNormal = Vector3.up;
                _timeSinceNotGrounded = Time.time;

                AudioSource.PlayOneShot(JumpSfx);
            }

            if (_distanceSinceLastStep > walkStepDistance / speedModifier)
            {
                _distanceSinceLastStep = 0;
                AudioSource.PlayOneShot(StepSfx);
            }

            _distanceSinceLastStep += _velocity.magnitude * Time.deltaTime;
        }

        private void HandleAirMovement(Vector3 worldMoveInput)
        {
            // Handle movement on air
            var verticalVelocity =
                Vector3.up * (_velocity.y - gravityAcceleration * GetGravityModifier * Time.deltaTime);
            var horizontalVelocity = new Vector3(_velocity.x, 0, _velocity.z);
            horizontalVelocity += worldMoveInput * (accelerationAir * Time.deltaTime * GetSpeedModifier);
            horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxSpeedAir * GetSpeedModifier);
            _velocity = verticalVelocity + horizontalVelocity;
        }

        private (Vector3 bottom, Vector3 top, float radius) GetCapsuleInfo(CharacterController controller,
            Transform tr)
        {
            var position = tr.position;
            var up = tr.up;
            var radius = CharacterController.radius;
            var bottom = position + up * radius;
            var top = position + up * (CharacterController.height - radius);
            return (bottom, top, radius);
        }

        private void HandleLook(Transform tr)
        {
            if (blockedLook) return;

            var look = fpsInput.GetLook() * (Time.deltaTime * rotationSpeed);
            
            // Look Horizontal
            tr.Rotate(new Vector3(0, look.x, 0));

            // Look Vertical
            _verticalAngle = Mathf.Clamp(_verticalAngle - look.y,
                -verticalAngleLimit, verticalAngleLimit);
            HeadTransform.localEulerAngles = new Vector3(_verticalAngle, 0, 0);
        }

        private Vector3 ReorientOnSlope(Vector3 vector, Vector3 slopeNormal, Transform tr)
        {
            var direction = vector.normalized;
            var directionRight = Vector3.Cross(direction, tr.up);
            return Vector3.Cross(slopeNormal, directionRight) * vector.magnitude;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var (bottom, top, radius) = GetCapsuleInfo(CharacterController, transform);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(top, radius);
            Gizmos.DrawSphere(bottom, radius);
        }
#endif
    }
}