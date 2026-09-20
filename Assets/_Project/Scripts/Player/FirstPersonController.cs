using UnityEngine;
using UnityEngine.InputSystem;
using MonsterChase.Core;

namespace MonsterChase.Player
{
    /// <summary>
    /// Walk, run, crouch, look. Crouch is not cosmetic -- it is the height you need to
    /// get under a bed, so its collider height is the real constraint on hiding spots.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Move")]
        [SerializeField] float walkSpeed = 3.2f;
        [SerializeField] float runSpeed = 5.6f;
        [SerializeField] float crouchSpeed = 1.5f;
        [SerializeField] float gravity = -18f;

        [Header("Stand / crouch")]
        [SerializeField] float standHeight = 1.8f;
        [SerializeField] float crouchHeight = 0.9f;
        [SerializeField] float crouchLerp = 10f;

        [Header("Look")]
        [SerializeField] Transform cameraPivot;
        [SerializeField] Camera playerCamera;
        [SerializeField] float pitchClamp = 85f;
        // Mouse and arrow-key sensitivity, invert-Y and FOV all live in GameSettings so
        // the options menu can change them mid-game. Nothing is cached: they are read
        // per frame, which costs nothing and means a slider moves the camera live.

        CharacterController cc;
        float pitch, verticalVelocity;
        bool crouchToggled;

        public bool IsCrouching { get; private set; }
        /// <summary>Sprinting under load, which is what costs you breath.</summary>
        public bool IsSprinting { get; private set; }
        public float Height => cc != null ? cc.height : standHeight;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (cameraPivot == null && Camera.main != null) cameraPivot = Camera.main.transform.parent;
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            ApplyFieldOfView();
        }

        void OnEnable()
        {
            GameSettings.Changed += ApplyFieldOfView;
            LockCursor(true);
        }

        void OnDisable()
        {
            GameSettings.Changed -= ApplyFieldOfView;
            LockCursor(false);
        }

        void ApplyFieldOfView()
        {
            if (playerCamera != null) playerCamera.fieldOfView = GameSettings.FieldOfView;
        }

        void Update()
        {
            // Escape belongs to PauseMenu. Two scripts reacting to the same press
            // ends with the cursor locking and unlocking in the same frame.
            var kb = Keyboard.current;

            Look(kb);
            Move(kb);
        }

        void Look(Keyboard kb)
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 delta = Vector2.zero;

            var mouse = Mouse.current;
            if (mouse != null) delta += mouse.delta.ReadValue() * GameSettings.MouseSensitivity;

            if (kb != null)
            {
                Vector2 keys = Vector2.zero;
                if (kb.leftArrowKey.isPressed)  keys.x -= 1f;
                if (kb.rightArrowKey.isPressed) keys.x += 1f;
                if (kb.downArrowKey.isPressed)  keys.y -= 1f;
                if (kb.upArrowKey.isPressed)    keys.y += 1f;
                if (keys != Vector2.zero) delta += keys * GameSettings.ArrowSensitivity * Time.deltaTime;
            }

            if (delta == Vector2.zero) return;

            if (GameSettings.InvertY) delta.y = -delta.y;

            transform.Rotate(Vector3.up * delta.x);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchClamp, pitchClamp);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void Move(Keyboard kb)
        {
            if (kb == null) return;

            // Held by default, so you cannot accidentally stay stuck under a bed while
            // something walks towards you. Toggle is available for anyone who would
            // rather not hold a key down for a whole hiding sequence.
            bool crouchKey = kb.leftCtrlKey.isPressed || kb.cKey.isPressed;
            if (GameSettings.HoldToCrouch)
            {
                IsCrouching = crouchKey;
                crouchToggled = false;
            }
            else
            {
                if (kb.leftCtrlKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame)
                    crouchToggled = !crouchToggled;
                IsCrouching = crouchToggled;
            }
            float wanted = IsCrouching ? crouchHeight : standHeight;
            cc.height = Mathf.Lerp(cc.height, wanted, crouchLerp * Time.deltaTime);
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);

            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            var dir = transform.right * x + transform.forward * z;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            IsSprinting = !IsCrouching && kb.leftShiftKey.isPressed && dir.sqrMagnitude > 0.01f;

            float speed = IsCrouching ? crouchSpeed
                        : IsSprinting ? runSpeed
                        : walkSpeed;

            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;

            cc.Move((dir * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
