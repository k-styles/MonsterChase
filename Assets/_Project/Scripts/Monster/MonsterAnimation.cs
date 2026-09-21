using UnityEngine;
using UnityEngine.AI;

namespace MonsterChase.Monster
{
    /// <summary>
    /// Drives the demon's clips off how fast it is actually moving. The pack ships a
    /// demo controller whose parameters we do not know, so clips are played by name
    /// with CrossFade rather than through a state machine. Cruder, and it survives the
    /// pack being reimported.
    /// </summary>
    [RequireComponent(typeof(MonsterVitals))]
    public class MonsterAnimation : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] NavMeshAgent agent;

        [Header("Clip names as the pack spells them")]
        [SerializeField] string idleClip = "Demon|Idle1";
        [SerializeField] string walkClip = "Demon|Walk1";
        [SerializeField] string runClip = "Demon|Run1";
        [SerializeField] string staggerClip = "Demon|Get-damage";
        [SerializeField] string deathClip = "Demon|Death";

        [Header("Thresholds")]
        [Tooltip("Any real movement at all should be a walk, never the idle. Idle while the agent slides it along is the foot-sliding.")]
        [SerializeField] float walkAbove = 0.05f;
        [SerializeField] float runAbove = 3.2f;
        [SerializeField] float crossFade = 0.12f;

        [Header("Stride matching")]
        [Tooltip("Ground speed the walk clip was authored for.")]
        [SerializeField] float walkClipSpeed = 1.4f;
        [Tooltip("Ground speed the run clip was authored for.")]
        [SerializeField] float runClipSpeed = 3.4f;
        [Tooltip("Wide, because a clamped stride is a sliding stride: if the legs cannot keep up with the ground speed the feet skate.")]
        [SerializeField] Vector2 playbackClamp = new Vector2(0.4f, 4f);

        MonsterVitals vitals;
        string current = "";
        MonsterVitals.State lastState = MonsterVitals.State.Hunting;

        void Awake()
        {
            vitals = GetComponent<MonsterVitals>();
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            if (vitals.Current != lastState)
            {
                lastState = vitals.Current;
                if (lastState == MonsterVitals.State.Staggered) Play(staggerClip, 0.05f);
                else if (lastState == MonsterVitals.State.Dead) Play(deathClip, 0.05f);
            }

            if (vitals.Current != MonsterVitals.State.Hunting) return;

            // Horizontal only. Vertical velocity from the agent settling onto the
            // navmesh would otherwise register as movement and start a walk cycle on
            // the spot.
            float speed = 0f;
            if (agent != null)
            {
                var flat = agent.velocity; flat.y = 0f;
                speed = flat.magnitude;
            }

            if (speed >= runAbove) Play(runClip);
            else if (speed >= walkAbove) Play(walkClip);
            else Play(idleClip);

            MatchStride(speed);
        }

        /// <summary>
        /// Plays the locomotion clip at the rate the ground is moving under it, or the
        /// thing appears to skate across the floor at its authored stride.
        /// </summary>
        void MatchStride(float speed)
        {
            bool locomotion = current == runClip || current == walkClip;

            float target = 1f;
            if (current == runClip && runClipSpeed > 0.01f) target = speed / runClipSpeed;
            else if (current == walkClip && walkClipSpeed > 0.01f) target = speed / walkClipSpeed;

            if (locomotion) target = Mathf.Clamp(target, playbackClamp.x, playbackClamp.y);
            else target = 1f;

            // Snap rather than ease while moving. Easing the playback rate is itself a
            // slide: for the fraction of a second it takes to catch up, the ground is
            // moving faster than the legs.
            animator.speed = locomotion ? target
                                        : Mathf.Lerp(animator.speed, 1f, 8f * Time.deltaTime);
        }

        void Play(string clip, float fade = -1f)
        {
            if (string.IsNullOrEmpty(clip) || clip == current) return;
            current = clip;
            if (clip != runClip && clip != walkClip) animator.speed = 1f;
            animator.CrossFade(clip, fade >= 0f ? fade : crossFade, 0);
        }
    }
}
