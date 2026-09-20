using UnityEngine;
using MonsterChase.Interaction;
using MonsterChase.Player;

namespace MonsterChase.Hiding
{
    /// <summary>
    /// Under a bed, inside a locker, behind a curtain. You interact to get in and
    /// interact again to get out.
    ///
    /// Deliberately not physical crawling: a 0.9m crouch capsule cannot navigate under
    /// a bed at a believable height, and forcing the geometry to allow it makes every
    /// bed in the ward look wrong. Instead the player is placed at a hide anchor with
    /// the camera dropped, movement off, and Concealed set. It reads the same and it
    /// never wedges you in a collider.
    /// </summary>
    public class HidingSpot : MonoBehaviour, IInteractable
    {
        [SerializeField] Transform hideAnchor;
        [SerializeField] string enterPrompt = "hide under the bed";
        [SerializeField] string exitPrompt = "come out";
        [Tooltip("Where the player is put down when they climb out.")]
        [SerializeField] Transform exitAnchor;

        public bool Occupied { get; private set; }

        static GameObject occupant;
        static Vector3 exitPosition;
        static HidingSpot activeSpot;

        /// <summary>True while the player is inside any hiding spot.</summary>
        public static bool PlayerHidden => activeSpot != null;

        public string Prompt => Occupied ? exitPrompt : enterPrompt;
        public bool CanInteract => activeSpot == null || activeSpot == this;
        public bool IsHold => false;

        void Awake()
        {
            if (hideAnchor == null) hideAnchor = transform;
        }

        public void Interact(GameObject interactor)
        {
            if (Occupied) Exit();
            else Enter(interactor);
        }

        public void InteractHeld(GameObject interactor, float deltaTime) { }

        void Enter(GameObject player)
        {
            if (activeSpot != null) return;

            occupant = player;
            activeSpot = this;
            Occupied = true;

            exitPosition = exitAnchor != null ? exitAnchor.position : player.transform.position;

            // The controller drives a CharacterController, which will fight a direct
            // transform write, so it goes off before the player is moved.
            var controller = player.GetComponent<FirstPersonController>();
            if (controller != null) controller.enabled = false;

            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = hideAnchor.position;
            player.transform.rotation = hideAnchor.rotation;
        }

        void Exit()
        {
            if (occupant == null) { Clear(); return; }

            occupant.transform.position = exitPosition;

            var cc = occupant.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;

            var controller = occupant.GetComponent<FirstPersonController>();
            if (controller != null) controller.enabled = true;

            Clear();
        }

        void Clear()
        {
            Occupied = false;
            occupant = null;
            activeSpot = null;
        }
    }
}
