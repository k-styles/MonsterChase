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
        [SerializeField] float walkAbove = 0.15f;
        [SerializeField] float runAbove = 3.2f;
        [SerializeField] float crossFade = 0.2f;

        [Header("Stride matching")]
        [Tooltip("Ground speed the walk clip was authored for.")]
        [SerializeField] float walkClipSpeed = 1.4f;
        [Tooltip("Ground speed the run clip was authored for.")]
        [SerializeField] float runClipSpeed = 3.4f;
        [SerializeField] Vector2 playbackClamp = new Vector2(0.6f, 2.2f);

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

            float speed = agent != null ? agent.velocity.magnitude : 0f;

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
            float target = 1f;
            if (current == runClip && runClipSpeed > 0.01f) target = speed / runClipSpeed;
            else if (current == walkClip && walkClipSpeed > 0.01f) target = speed / walkClipSpeed;

            if (current != runClip && current != walkClip) target = 1f;
            else target = Mathf.Clamp(target, playbackClamp.x, playbackClamp.y);

            animator.speed = Mathf.Lerp(animator.speed, target, 8f * Time.deltaTime);
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
