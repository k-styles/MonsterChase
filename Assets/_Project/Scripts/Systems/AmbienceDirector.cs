using UnityEngine;
using MonsterChase.Monster;

namespace MonsterChase.Systems
{
    /// <summary>
    /// The bed the whole level sits on. One looping track, 2D so it does not come from
    /// anywhere, ducked when the monster is actually hunting you -- a score that keeps
    /// playing at full volume through a chase competes with the thing you need to hear,
    /// which is where the monster is.
    /// </summary>
    public class AmbienceDirector : MonoBehaviour
    {
        [SerializeField] AudioSource bed;
        [SerializeField] MonsterAI monster;

        [Header("Levels")]
        [SerializeField] float calmVolume = 0.34f;
        [Tooltip("Pulled down while it is chasing, so its growl and your footsteps win.")]
        [SerializeField] float huntedVolume = 0.12f;
        [SerializeField] float fadeSeconds = 2.2f;
        [SerializeField] float openingFadeIn = 6f;

        float elapsed;

        void Awake()
        {
            if (monster == null) monster = Object.FindFirstObjectByType<MonsterAI>();
            if (bed != null) bed.volume = 0f;
        }

        void Update()
        {
            if (bed == null) return;

            elapsed += Time.deltaTime;

            bool hunted = monster != null && monster.Current == MonsterAI.State.Chase;
            float target = hunted ? huntedVolume : calmVolume;

            // It arrives with you rather than being there from frame one.
            target *= Mathf.Clamp01(elapsed / Mathf.Max(0.01f, openingFadeIn));

            bed.volume = Mathf.MoveTowards(bed.volume, target, Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
        }
    }
}
