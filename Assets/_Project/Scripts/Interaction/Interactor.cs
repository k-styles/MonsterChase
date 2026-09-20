using UnityEngine;
using UnityEngine.InputSystem;

namespace MonsterChase.Interaction
{
    /// <summary>
    /// Looks for something usable near the crosshair and acts on it.
    ///
    /// A plain Raycast was wrong for this. Bodies lie on the ground and ammo boxes are
    /// 18cm tall, so the terrain is almost always the closest hit and the interactable
    /// behind it never gets seen -- which is why nothing could be picked up or burnt.
    ///
    /// Instead: sweep a sphere so aim does not have to be pixel-perfect, take every
    /// hit, and pick the nearest interactable. Something solid only blocks it if it is
    /// meaningfully in front, so the floor a body is lying on does not count as cover.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [SerializeField] Camera sourceCamera;
        [SerializeField] float range = 3.5f;
        [Tooltip("Sweep radius. Bigger is more forgiving to aim.")]
        [SerializeField] float aimRadius = 0.3f;
        [Tooltip("How far in front of the target something must be to actually block it.")]
        [SerializeField] float blockingMargin = 0.5f;
        [SerializeField] LayerMask mask = ~0;

        public bool HasTarget => current != null && current.CanInteract;
        public IInteractable Current => current;
        public string Prompt { get; private set; } = "";

        IInteractable current;
        readonly RaycastHit[] hits = new RaycastHit[24];

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
            if (Cursor.lockState != CursorLockMode.Locked) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;

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

            var ray = sourceCamera.ScreenPointToRay(
                new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));

            int count = Physics.SphereCastNonAlloc(ray, aimRadius, hits, range, mask,
                                                   QueryTriggerInteraction.Collide);
            if (count == 0) return;

            IInteractable best = null;
            float bestDistance = float.MaxValue;
            float nearestSolid = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                // SphereCast reports distance 0 when it starts overlapping; treat those
                // as touching rather than discarding them.
                float d = hit.distance <= 0.0001f ? 0f : hit.distance;

                var candidate = hit.collider.GetComponentInParent<IInteractable>();
                if (candidate != null && candidate.CanInteract)
                {
                    if (d < bestDistance) { bestDistance = d; best = candidate; }
                }
                else if (!hit.collider.isTrigger && d < nearestSolid)
                {
                    nearestSolid = d;
                }
            }

            if (best == null) return;

            // A wall between you and it still blocks; the ground it rests on does not.
            if (nearestSolid + blockingMargin < bestDistance) return;

            current = best;
        }
    }
}
