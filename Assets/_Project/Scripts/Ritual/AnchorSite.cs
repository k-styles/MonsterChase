using UnityEngine;
using MonsterChase.Interaction;

namespace MonsterChase.Ritual
{
    /// <summary>
    /// A corpse that anchors the monster. Look at it and hold the button to set it
    /// alight; let go and the progress drains, so it has to be held while something is
    /// hunting you. That tension is the point -- burning is not free, it pins you in
    /// place with your back turned.
    /// </summary>
    public class AnchorSite : MonoBehaviour, IInteractable
    {
        [SerializeField] float secondsToBurn = 4f;
        [Tooltip("How fast progress drains when you let go, as a multiple of burn speed.")]
        [SerializeField] float drainMultiplier = 0.6f;

        [Header("Tell")]
        [SerializeField] ParticleSystem fire;
        [SerializeField] Light fireLight;
        [SerializeField] AudioSource fireAudio;

        public float Progress { get; private set; }
        public bool Burnt { get; private set; }

        /// <summary>True on the frames the player is actually feeding this one.</summary>
        public bool BeingBurned { get; private set; }

        bool burnedThisFrame;

        // ------------------------------------------------------------ IInteractable

        public string Prompt => Burnt ? "" : "hold to burn the body";
        public bool CanInteract => !Burnt;
        public bool IsHold => true;

        public void Interact(GameObject interactor) { }

        public void InteractHeld(GameObject interactor, float deltaTime)
        {
            if (Burnt) return;
            burnedThisFrame = true;
            Progress = Mathf.Clamp01(Progress + deltaTime / Mathf.Max(0.01f, secondsToBurn));
            if (Progress >= 1f) Burn();
        }

        // ------------------------------------------------------------------- drain

        void Start() => SetFire(false);

        void LateUpdate()
        {
            // Interactor runs in Update and sets the flag; draining is decided here,
            // after every interactor has had its turn. With co-op that matters -- two
            // players feeding the same body must not each cancel the other's drain.
            BeingBurned = burnedThisFrame;
            burnedThisFrame = false;

            if (Burnt || BeingBurned) return;

            Progress = Mathf.Clamp01(
                Progress - Time.deltaTime / Mathf.Max(0.01f, secondsToBurn) * drainMultiplier);
        }

        void Burn()
        {
            Burnt = true;
            Progress = 1f;
            SetFire(true);

            if (RitualState.Instance != null) RitualState.Instance.RegisterBurn();
            else Debug.LogWarning("[Anchor] No RitualState in the scene; this burn counts for nothing.");
        }

        void SetFire(bool on)
        {
            if (fire != null) { if (on) fire.Play(); else fire.Stop(); }
            if (fireLight != null) fireLight.enabled = on;
            if (fireAudio != null) { if (on) fireAudio.Play(); else fireAudio.Stop(); }
        }
    }
}
