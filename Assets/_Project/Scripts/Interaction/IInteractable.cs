using UnityEngine;

namespace MonsterChase.Interaction
{
    /// <summary>Anything the player can look at and act on.</summary>
    public interface IInteractable
    {
        /// <summary>Shown under the crosshair, e.g. "open" or "take pistol".</summary>
        string Prompt { get; }

        /// <summary>False greys the prompt out -- visible, but not usable yet.</summary>
        bool CanInteract { get; }

        /// <summary>True if this wants the button held rather than clicked.</summary>
        bool IsHold { get; }

        /// <summary>Click interactables. Called once per press.</summary>
        void Interact(GameObject interactor);

        /// <summary>Hold interactables. Called every frame the button is down.</summary>
        void InteractHeld(GameObject interactor, float deltaTime);
    }
}
