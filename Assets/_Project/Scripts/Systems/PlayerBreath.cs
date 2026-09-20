using UnityEngine;
using UnityEngine.InputSystem;
using MonsterChase.Core;
using MonsterChase.Player;

namespace MonsterChase.Systems
{
    /// <summary>
    /// Breathing, and the decision to stop. Running costs air; standing still gives it
    /// back. Holding your breath makes you silent, and you cannot do it for long, and
    /// running out does not make you quiet again — it makes you louder than you have
    /// been all night, at the exact moment that matters most.
    /// </summary>
    public class PlayerBreath : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] FirstPersonController controller;
        [SerializeField] AudioSource breathing;
        [SerializeField] AudioClip gaspClip;

        [Header("Holding")]
        [Tooltip("Seconds you can hold it when rested.")]
        [SerializeField] float maxHold = 7f;
        [SerializeField] float recoverPerSecond = 1.4f;
        [Tooltip("Extra drain per second while your lungs are still going from running.")]
        [SerializeField] float exertionPenalty = 1.1f;
        [Tooltip("Seconds after a forced gasp before you can hold it again.")]
        [SerializeField] float gaspLockout = 2.5f;

        [Header("Exertion")]
        [Tooltip("Seconds of sprinting to reach full exertion.")]
        [SerializeField] float exertionRise = 3.5f;
        [SerializeField] float exertionFall = 0.28f;

        [Header("Noise it makes")]
        [SerializeField] float breathInterval = 1.7f;
        [SerializeField] float calmRadius = 2.5f;
        [SerializeField] float pantingRadius = 9f;
        [SerializeField] float gaspRadius = 16f;

        [Header("Volume")]
        [SerializeField] float calmVolume = 0.12f;
        [SerializeField] float pantingVolume = 0.7f;
        [SerializeField] float volumeLerp = 3f;

        float hold;
        float exertion;
        float lockout;
        float noiseTimer;

        /// <summary>0..1 of held breath remaining. Drive the HUD meter off this.</summary>
        public float HoldNormalized => maxHold <= 0f ? 0f : Mathf.Clamp01(hold / maxHold);
        public bool IsHolding { get; private set; }
        public bool Locked => lockout > 0f;
        /// <summary>0..1. How hard you are breathing, which is how far you can be heard.</summary>
        public float Exertion => Mathf.Clamp01(exertion);

        /// <summary>Raised when you run out and gasp. The loudest thing you will do.</summary>
        public System.Action Gasped;

        void Awake()
        {
            if (controller == null) controller = GetComponent<FirstPersonController>();
            hold = maxHold;
        }

        void Update()
        {
            TickExertion();
            TickHold();
            TickNoise();
            TickVolume();
        }

        void TickExertion()
        {
            bool working = controller != null && controller.IsSprinting;
            exertion += (working ? Time.deltaTime / Mathf.Max(0.01f, exertionRise)
                                 : -Time.deltaTime * exertionFall);
            exertion = Mathf.Clamp01(exertion);
        }

        void TickHold()
        {
            if (lockout > 0f) lockout -= Time.deltaTime;

            var kb = Keyboard.current;
            bool wants = kb != null && kb.spaceKey.isPressed
                         && !Locked
                         && (controller == null || controller.enabled);

            if (wants && hold > 0f)
            {
                IsHolding = true;
                hold -= Time.deltaTime * (1f + exertionPenalty * Exertion);

                if (hold <= 0f)
                {
                    hold = 0f;
                    IsHolding = false;
                    lockout = gaspLockout;
                    Gasp();
                }
                return;
            }

            IsHolding = false;
            hold = Mathf.Min(maxHold, hold + recoverPerSecond * Time.deltaTime);
        }

        void Gasp()
        {
            if (breathing != null && gaspClip != null) breathing.PlayOneShot(gaspClip, 1f);
            GameEvents.RaiseNoise(transform.position, gaspRadius);
            Gasped?.Invoke();
        }

        void TickNoise()
        {
            // Held breath is genuinely silent. That is the whole point of the mechanic.
            if (IsHolding) { noiseTimer = breathInterval; return; }

            noiseTimer -= Time.deltaTime;
            if (noiseTimer > 0f) return;

            noiseTimer = Mathf.Lerp(breathInterval, breathInterval * 0.55f, Exertion);
            float radius = Mathf.Lerp(calmRadius, pantingRadius, Exertion);
            if (controller != null && controller.IsCrouching) radius *= 0.55f;

            GameEvents.RaiseNoise(transform.position, radius);
        }

        void TickVolume()
        {
            if (breathing == null) return;

            float target = IsHolding ? 0f : Mathf.Lerp(calmVolume, pantingVolume, Exertion);
            breathing.volume = Mathf.Lerp(breathing.volume, target, volumeLerp * Time.deltaTime);
            breathing.pitch = Mathf.Lerp(0.92f, 1.12f, Exertion);
        }
    }
}
