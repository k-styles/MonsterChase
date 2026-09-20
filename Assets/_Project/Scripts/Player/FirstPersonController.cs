using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] float sensitivity = 0.11f;
        [SerializeField] float keyTurnSpeed = 140f;
        [SerializeField] float pitchClamp = 85f;

        CharacterController cc;
        float pitch, verticalVelocity;

        public bool IsCrouching { get; private set; }
        public float Height => cc != null ? cc.height : standHeight;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (cameraPivot == null && Camera.main != null) cameraPivot = Camera.main.transform.parent;
        }

        void OnEnable() => LockCursor(true);
        void OnDisable() => LockCursor(false);

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                LockCursor(Cursor.lockState != CursorLockMode.Locked);

            Look(kb);
            Move(kb);
        }

        void Look(Keyboard kb)
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 delta = Vector2.zero;

            var mouse = Mouse.current;
            if (mouse != null) delta += mouse.delta.ReadValue() * sensitivity;

            if (kb != null)
            {
                Vector2 keys = Vector2.zero;
                if (kb.leftArrowKey.isPressed)  keys.x -= 1f;
                if (kb.rightArrowKey.isPressed) keys.x += 1f;
                if (kb.downArrowKey.isPressed)  keys.y -= 1f;
                if (kb.upArrowKey.isPressed)    keys.y += 1f;
                if (keys != Vector2.zero) delta += keys * keyTurnSpeed * Time.deltaTime;
            }

            if (delta == Vector2.zero) return;

            transform.Rotate(Vector3.up * delta.x);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchClamp, pitchClamp);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void Move(Keyboard kb)
        {
            if (kb == null) return;

            // Crouch is held, not toggled, so you cannot accidentally stay stuck under
            // a bed while something is walking towards you.
            IsCrouching = kb.leftCtrlKey.isPressed || kb.cKey.isPressed;
            float wanted = IsCrouching ? crouchHeight : standHeight;
            cc.height = Mathf.Lerp(cc.height, wanted, crouchLerp * Time.deltaTime);
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);

            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            var dir = transform.right * x + transform.forward * z;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            float speed = IsCrouching ? crouchSpeed
                        : kb.leftShiftKey.isPressed ? runSpeed
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
