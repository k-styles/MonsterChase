using UnityEngine;
using MonsterChase.Interaction;

namespace MonsterChase.Player
{
    /// <summary>
    /// A box of rounds on the ground. Deliberately scarce and deliberately out in the
    /// open: going for ammo should mean leaving cover while something is looking for
    /// you, so the decision to top up is itself a risk.
    /// </summary>
    public class AmmoPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] int rounds = 12;
        [SerializeField] AudioClip pickupClip;
        [SerializeField] GameObject visual;

        bool taken;

        public string Prompt => taken ? "" : $"take {rounds} rounds";
        public bool CanInteract => !taken;
        public bool IsHold => false;

        public void InteractHeld(GameObject interactor, float deltaTime) { }

        public void Interact(GameObject interactor)
        {
            if (taken) return;

            var gun = interactor.GetComponentInChildren<Gun>();
            if (gun == null) return;

            // A full player leaves the box where it is, so it is still there later.
            if (gun.AddAmmo(rounds) <= 0) return;

            taken = true;
            if (pickupClip != null) AudioSource.PlayClipAtPoint(pickupClip, transform.position, 0.8f);
            if (visual != null) visual.SetActive(false);
            else gameObject.SetActive(false);
        }
    }
}
