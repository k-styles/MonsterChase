using UnityEngine;

namespace MonsterChase.Ritual
{
    /// <summary>
    /// A corpse that anchors the monster. Hold the burn key near it to set it alight;
    /// let go and the progress drains, so it has to be held while something is hunting
    /// you. That tension is the point -- burning is not free, it pins you in place.
    /// </summary>
    public class AnchorSite : MonoBehaviour
    {
        [SerializeField] float secondsToBurn = 4f;
        [Tooltip("How fast progress drains when you let go, as a multiple of burn speed.")]
        [SerializeField] float drainMultiplier = 0.6f;
        [SerializeField] float playerRange = 2.5f;

        [Header("Tell")]
        [SerializeField] ParticleSystem fire;
        [SerializeField] Light fireLight;
        [SerializeField] AudioSource fireAudio;

        public float Progress { get; private set; }
        public bool Burnt { get; private set; }
        public bool PlayerInRange { get; private set; }

        Transform player;

        void Start()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) player = tagged.transform;
            SetFire(false);
        }

        void Update()
        {
            if (Burnt) return;

            PlayerInRange = player != null &&
                Vector3.Distance(player.position, transform.position) <= playerRange;

            bool holding = PlayerInRange && BurnHeld();

            if (holding) Progress += Time.deltaTime / Mathf.Max(0.01f, secondsToBurn);
            else Progress -= Time.deltaTime / Mathf.Max(0.01f, secondsToBurn) * drainMultiplier;

            Progress = Mathf.Clamp01(Progress);

            if (Progress >= 1f) Burn();
        }

        static bool BurnHeld()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.fKey.isPressed;
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
