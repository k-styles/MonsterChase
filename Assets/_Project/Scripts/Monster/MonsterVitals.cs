using System;
using UnityEngine;
using MonsterChase.Ritual;

namespace MonsterChase.Monster
{
    /// <summary>
    /// The monster's health, healing and stagger -- and the single authoritative owner
    /// of all three. Guns call <see cref="TakeDamage"/>; nothing else writes health.
    ///
    /// The arc the whole game rests on:
    ///
    ///   0 anchors burned   shooting it drops it for a moment, then it heals to full
    ///                      almost immediately. Guns feel useless, and should.
    ///   partway            each burn delays the healing, slows it, and keeps the
    ///                      monster down longer. Guns become a tool for buying time.
    ///   all 5 burned       healing stops entirely. The next time it goes down it
    ///                      stays down. Guns are finally lethal.
    ///
    /// Every number here is public and drawn on screen by VitalsReadout, because if the
    /// player cannot feel the difference between four anchors and five, the ritual is
    /// just fetch-questing. Tune against the readout before dressing any of it up.
    /// </summary>
    public class MonsterVitals : MonoBehaviour
    {
        public enum State { Hunting, Staggered, Dead }

        [Header("Health")]
        [SerializeField] float maxHealth = 100f;

        [Header("Healing -- at zero anchors burned")]
        [Tooltip("Seconds after the last hit before regeneration starts.")]
        [SerializeField] float baseHealDelay = 2.5f;
        [Tooltip("Health per second once regeneration starts.")]
        [SerializeField] float baseHealRate = 22f;

        [Header("Healing -- what each anchor takes away")]
        [Tooltip("Seconds added to the delay per anchor burned.")]
        [SerializeField] float healDelayPerAnchor = 1.6f;

        [Header("Stagger")]
        [Tooltip("Seconds it stays down when dropped, at zero anchors burned.")]
        [SerializeField] float baseStagger = 3.5f;
        [Tooltip("Extra seconds down per anchor burned.")]
        [SerializeField] float staggerPerAnchor = 2.8f;

        public float Health { get; private set; }
        public float MaxHealth => maxHealth;
        public State Current { get; private set; } = State.Hunting;

        /// <summary>Seconds left of the current stagger, or 0.</summary>
        public float StaggerRemaining { get; private set; }
        /// <summary>Seconds left before healing kicks in, or 0 if it already has.</summary>
        public float HealDelayRemaining { get; private set; }

        public int AnchorsBurned => RitualState.Instance != null ? RitualState.Instance.Burned : 0;
        float RitualProgress => RitualState.Instance != null ? RitualState.Instance.Progress : 0f;

        /// <summary>Healing is switched off entirely once the ritual is done.</summary>
        public bool CanHeal => RitualState.Instance == null || !RitualState.Instance.Complete;
        /// <summary>Until the ritual is complete, dropping it only staggers it.</summary>
        public bool CanDie => RitualState.Instance != null && RitualState.Instance.Complete;

        public float CurrentHealDelay => baseHealDelay + healDelayPerAnchor * AnchorsBurned;
        public float CurrentHealRate  => CanHeal ? baseHealRate * (1f - RitualProgress) : 0f;
        public float CurrentStagger   => baseStagger + staggerPerAnchor * AnchorsBurned;

        public event Action Staggered;
        public event Action Recovered;
        public event Action Died;

        void Awake() => Health = maxHealth;

        void Update()
        {
            switch (Current)
            {
                case State.Staggered: TickStagger(); break;
                case State.Hunting:   TickHealing(); break;
            }
        }

        void TickStagger()
        {
            StaggerRemaining -= Time.deltaTime;
            if (StaggerRemaining > 0f) return;

            // It gets up with whatever healing has given it back, not at full. At high
            // anchor counts that means it returns already hurt, which compounds.
            StaggerRemaining = 0f;
            Current = State.Hunting;
            HealDelayRemaining = CurrentHealDelay;
            if (Health <= 0f) Health = 1f;
            Recovered?.Invoke();
        }

        void TickHealing()
        {
            if (!CanHeal || Health >= maxHealth) return;

            if (HealDelayRemaining > 0f)
            {
                HealDelayRemaining -= Time.deltaTime;
                return;
            }

            Health = Mathf.Min(maxHealth, Health + CurrentHealRate * Time.deltaTime);
        }

        public void TakeDamage(float amount)
        {
            if (Current == State.Dead || amount <= 0f) return;

            Health = Mathf.Max(0f, Health - amount);
            HealDelayRemaining = CurrentHealDelay;

            if (Health > 0f) return;

            if (CanDie)
            {
                Current = State.Dead;
                StaggerRemaining = 0f;
                Died?.Invoke();
            }
            else if (Current != State.Staggered)
            {
                Current = State.Staggered;
                StaggerRemaining = CurrentStagger;
                Staggered?.Invoke();
            }
        }

        public void ResetVitals()
        {
            Health = maxHealth;
            Current = State.Hunting;
            StaggerRemaining = 0f;
            HealDelayRemaining = 0f;
        }
    }
}
