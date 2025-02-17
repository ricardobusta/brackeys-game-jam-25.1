using Busta;
using UnityEngine;

namespace Scripts
{
    public class FpsInput : MonoBehaviour
    {
        private FpsInputAsset _input;
        public bool forceShowCursor;

        [Header("Parameters")] [SerializeField]
        private float webglSensitivityMultiplier = 0.25f;

        [SerializeField] private float mouseSensitivityMultiplier = 0.5f;
        [SerializeField] private float maxMovementSize = 1.0f;
        public bool locked;
        public bool joyControl;

        private static bool IsWebgl
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#endif
                return false;
            }
        }

        private void Awake()
        {
            _input = new FpsInputAsset();
        }

        private void Start()
        {
            if (!forceShowCursor)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }


        private void OnEnable()
        {
            _input.Enable();
        }

        private void OnDisable()
        {
            _input.Disable();
        }

        public Vector2 GetLook()
        {
            var scale = IsWebgl ? webglSensitivityMultiplier : mouseSensitivityMultiplier;
            return _input.Gameplay.Look.ReadValue<Vector2>() * scale;
        }

        public bool GetJump()
        {
            return _input.Gameplay.Jump.WasPressedThisFrame();
        }

        public bool GetSprint()
        {
            return _input.Gameplay.Sprint.IsPressed();
        }

        public Vector2 GetMove()
        {
            if (locked)
            {
                return Vector2.zero;
            }

            return LimitDiagonalMovement(_input.Gameplay.Movement.ReadValue<Vector2>());
        }

        public bool GetInteract()
        {
            return !locked && _input.Gameplay.Interact.WasPressedThisFrame();
        }

        public bool GetInspect()
        {
            return !locked && _input.Gameplay.Inspect.WasPressedThisFrame();
        }

        private Vector2 LimitDiagonalMovement(Vector2 movement)
        {
            return Vector2.ClampMagnitude(movement, maxMovementSize);
        }
    }
}