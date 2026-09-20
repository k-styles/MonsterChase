using UnityEngine;
using MonsterChase.Interaction;
using MonsterChase.Monster;

namespace MonsterChase.Ritual
{
    /// <summary>
    /// A corpse that anchors the monster. Look at it and hold to set it alight; let go
    /// and the progress drains, so it has to be held while something is hunting you.
    ///
    /// You cannot burn anything before you have seen what you are dealing with. Until
    /// the monster has been in front of you once, a body is just a body -- no prompt,
    /// no meter. The first sighting is what turns the map into a list of jobs.
    /// </summary>
    public class AnchorSite : MonoBehaviour, IInteractable
    {
        [SerializeField] float secondsToBurn = 4f;
        [Tooltip("How fast progress drains when you let go, as a multiple of burn speed.")]
        [SerializeField] float drainMultiplier = 0.6f;
        [Tooltip("Seconds the body keeps burning before it is gone.")]
        [SerializeField] float burnDownSeconds = 6f;

        [Header("Tell")]
        [SerializeField] ParticleSystem fire;
        [SerializeField] Light fireLight;
        [SerializeField] AudioSource fireAudio;
        [Tooltip("What is hidden once the body has burnt away. Usually the model.")]
        [SerializeField] GameObject corpseVisual;

        public float Progress { get; private set; }
        public bool Burnt { get; private set; }
        public bool BeingBurned { get; private set; }

        /// <summary>
        /// Set once the player has actually seen the monster. Until then no anchor is
        /// interactable. Static because it is a fact about the run, not about one body.
        /// </summary>
        public static bool MonsterSeen { get; private set; }

        public static void MarkMonsterSeen() => MonsterSeen = true;
        public static void ResetSightings() => MonsterSeen = false;

        bool burnedThisFrame;
        float burnDownLeft;

        // ------------------------------------------------------------ IInteractable

        public string Prompt => Burnt || !MonsterSeen ? "" : "hold to burn the body";
        public bool CanInteract => !Burnt && MonsterSeen;
        public bool IsHold => true;

        public void Interact(GameObject interactor) { }

        public void InteractHeld(GameObject interactor, float deltaTime)
        {
            if (Burnt || !MonsterSeen) return;
            burnedThisFrame = true;
            Progress = Mathf.Clamp01(Progress + deltaTime / Mathf.Max(0.01f, secondsToBurn));
            if (Progress >= 1f) Burn();
        }

        // ------------------------------------------------------------------ ticking

        void Start()
        {
            // Nothing is alight until the player lights it. The fire prefab plays its
            // particles and its crackle on awake, which meant every body sounded like
            // it was already burning from across the village.
            SetFire(false);
        }

        void LateUpdate()
        {
            if (Burnt)
            {
                TickBurnDown();
                return;
            }

            // Interactor runs in Update and sets the flag; the drain is decided here so
            // that with co-op two players feeding one body do not cancel each other.
            BeingBurned = burnedThisFrame;
            burnedThisFrame = false;
            if (BeingBurned) return;

            Progress = Mathf.Clamp01(
                Progress - Time.deltaTime / Mathf.Max(0.01f, secondsToBurn) * drainMultiplier);
        }

        void TickBurnDown()
        {
            if (burnDownLeft <= 0f) return;

            burnDownLeft -= Time.deltaTime;
            if (burnDownLeft > 0f) return;

            // Burnt out: the body is gone, and the fire goes with it. What is left is
            // an anchor that is spent, which the ritual has already counted.
            if (corpseVisual != null) corpseVisual.SetActive(false);
            SetFire(false);
        }

        void Burn()
        {
            Burnt = true;
            Progress = 1f;
            burnDownLeft = burnDownSeconds;
            SetFire(true);

            if (RitualState.Instance != null) RitualState.Instance.RegisterBurn();
            else Debug.LogWarning("[Anchor] No RitualState in the scene; this burn counts for nothing.");
        }

        void SetFire(bool on)
        {
            if (fire != null)
            {
                if (on) fire.Play(true);
                else fire.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (fireLight != null) fireLight.enabled = on;
            if (fireAudio != null)
            {
                if (on) fireAudio.Play();
                else fireAudio.Stop();
            }
        }
    }
}
