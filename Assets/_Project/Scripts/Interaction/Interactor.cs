using UnityEngine;
using UnityEngine.InputSystem;

namespace MonsterChase.Interaction
{
    /// <summary>
    /// Looks for something usable under the crosshair and acts on it.
    ///
    /// Both the left mouse button and E work. The gun asks <see cref="HasTarget"/>
    /// before firing, so clicking a door opens it instead of putting a round through
    /// it -- the same button does the sensible thing in each context, and you are
    /// never left wondering why a click did nothing.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [SerializeField] Camera sourceCamera;
        [SerializeField] float range = 3.2f;
        [SerializeField] LayerMask mask = ~0;

        /// <summary>Something usable is under the crosshair right now.</summary>
        public bool HasTarget => current != null && current.CanInteract;
        public IInteractable Current => current;
        public string Prompt { get; private set; } = "";

        IInteractable current;

        void Awake()
        {
            if (sourceCamera == null) sourceCamera = Camera.main;
        }

        void Update()
        {
            Scan();

            if (current == null) { Prompt = ""; return; }

            Prompt = current.CanInteract ? current.Prompt : "";
            if (!current.CanInteract) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (Cursor.lockState != CursorLockMode.Locked) return;

            if (current.IsHold)
            {
                bool held = (mouse != null && mouse.leftButton.isPressed)
                         || (kb != null && (kb.eKey.isPressed || kb.fKey.isPressed));
                if (held) current.InteractHeld(gameObject, Time.deltaTime);
            }
            else
            {
                bool pressed = (mouse != null && mouse.leftButton.wasPressedThisFrame)
                            || (kb != null && kb.eKey.wasPressedThisFrame);
                if (pressed) current.Interact(gameObject);
            }
        }

        void Scan()
        {
            current = null;
            if (sourceCamera == null) return;

            var ray = new Ray(sourceCamera.transform.position, sourceCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, range, mask, QueryTriggerInteraction.Collide)) return;

            current = hit.collider.GetComponentInParent<IInteractable>();
        }
    }
}
