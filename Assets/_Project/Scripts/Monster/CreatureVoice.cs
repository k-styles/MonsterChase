using UnityEngine;

namespace MonsterChase.Monster
{
    /// <summary>
    /// What the thing sounds like, driven by what it is doing. Spatialised on the
    /// creature itself, so a roar while it is behind you is behind you, and the moment
    /// it stops being behind you is information the player gets for free.
    /// </summary>
    [RequireComponent(typeof(MonsterAI))]
    public class CreatureVoice : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] MonsterAI ai;
        [SerializeField] AudioSource mouth;
        [SerializeField] Transform player;

        [Header("Clips")]
        [SerializeField] AudioClip[] roars;
        [SerializeField] AudioClip[] snarls;
        [Tooltip("Played the moment it spots you and commits. One is picked at random.")]
        [SerializeField] AudioClip[] noticeSounds;

        [Header("Pacing (seconds between sounds)")]
        [SerializeField] Vector2 huntingInterval = new Vector2(2.6f, 5f);
        [SerializeField] Vector2 searchingInterval = new Vector2(4f, 9f);
        [SerializeField] Vector2 idleInterval = new Vector2(9f, 16f);
        [Tooltip("Within this distance it snarls instead of roaring, and more often.")]
        [SerializeField] float closeRange = 9f;

        float nextAt;
        MonsterAI.State lastState;

        void Awake()
        {
            if (ai == null) ai = GetComponent<MonsterAI>();
            if (mouth == null) mouth = GetComponent<AudioSource>();
            if (player == null)
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null) player = tagged.transform;
            }
            Schedule();
        }

        void Update()
        {
            if (ai == null || mouth == null) return;

            if (ai.Current != lastState)
            {
                // Committing to a chase gets its own sound, every time, no cooldown.
                if (ai.Current == MonsterAI.State.Chase && lastState != MonsterAI.State.Chase)
                {
                    var notice = Pick(noticeSounds);
                    Play(notice != null ? notice : Pick(roars), 1f);
                    Schedule();
                }
                lastState = ai.Current;
            }

            // No Dormant state here: it is always awake, just not always interested.
            if (Time.time < nextAt) return;

            bool close = player != null && Vector3.Distance(transform.position, player.position) <= closeRange;
            Play(close ? Pick(snarls) : Pick(roars), close ? 0.95f : 0.8f);
            Schedule();
        }

        void Schedule()
        {
            Vector2 range = ai == null ? idleInterval : ai.Current switch
            {
                MonsterAI.State.Chase => huntingInterval,
                MonsterAI.State.Search => searchingInterval,
                MonsterAI.State.Investigate => searchingInterval,
                _ => idleInterval
            };

            bool close = player != null && Vector3.Distance(transform.position, player.position) <= closeRange;
            float t = Random.Range(range.x, range.y) * (close ? 0.6f : 1f);
            nextAt = Time.time + t;
        }

        AudioClip Pick(AudioClip[] bank)
        {
            if (bank == null || bank.Length == 0) return null;
            return bank[Random.Range(0, bank.Length)];
        }

        void Play(AudioClip clip, float volume)
        {
            if (clip == null) return;
            mouth.pitch = Random.Range(0.93f, 1.07f);
            mouth.PlayOneShot(clip, volume);
        }

        /// <summary>For scripted moments, like the one where it reaches you.</summary>
        public void Shout(AudioClip clip, float volume = 1f) => Play(clip, volume);
    }
}
