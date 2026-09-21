using UnityEngine;
using UnityEngine.UI;
using MonsterChase.Monster;
using MonsterChase.Ritual;
using MonsterChase.Interaction;

namespace MonsterChase.UI
{
    /// <summary>
    /// Every number the anchor mechanic depends on, drawn on screen. This is a tuning
    /// instrument, not a HUD -- it exists so the feel of "burning an anchor changed
    /// something" can be checked against what actually changed, before any of it is
    /// dressed up. Switch it off with the backtick key.
    /// </summary>
    public class VitalsReadout : MonoBehaviour
    {
        [SerializeField] MonsterVitals monster;
        [SerializeField] Text readout;
        [SerializeField] Image healthFill;
        [SerializeField] Image burnFill;
        [SerializeField] Text prompt;

        Interactor interactor;
        bool visible = true;

        void Start()
        {
            if (monster == null) monster = Object.FindFirstObjectByType<MonsterVitals>();
            interactor = Object.FindFirstObjectByType<Interactor>();
        }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.backquoteKey.wasPressedThisFrame) visible = !visible;

            if (readout != null) readout.enabled = visible;
            if (healthFill != null) healthFill.transform.parent.gameObject.SetActive(visible);

            if (monster == null || readout == null) return;

            int burned = monster.AnchorsBurned;
            int total = RitualState.Instance != null ? RitualState.Instance.Total : 5;

            string verdict = !monster.CanHeal
                ? "HEALING OFF -- it can die now"
                : $"heals {monster.CurrentHealRate:F0}/s after {monster.CurrentHealDelay:F1}s";

            readout.text =
                Diagnostics() +
                $"anchors   {burned}/{total}\n" +
                $"state     {monster.Current}\n" +
                $"health    {monster.Health:F0}/{monster.MaxHealth:F0}\n" +
                $"{verdict}\n" +
                $"stagger   {monster.CurrentStagger:F1}s" +
                (monster.StaggerRemaining > 0f ? $"  (down {monster.StaggerRemaining:F1}s)" : "") + "\n" +
                (monster.HealDelayRemaining > 0f ? $"regen in  {monster.HealDelayRemaining:F1}s" : "");

            if (healthFill != null)
                healthFill.fillAmount = monster.MaxHealth > 0f ? monster.Health / monster.MaxHealth : 0f;

            TickAnchorPrompt();
        }

        /// <summary>
        /// Why the thing under your crosshair is or is not responding. Every link in
        /// the chain from input to burn progress, so a failure names itself instead of
        /// being guessed at from a description.
        /// </summary>
        string Diagnostics()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            bool holdDown = (mouse != null && mouse.leftButton.isPressed)
                         || (kb != null && (kb.eKey.isPressed || kb.fKey.isPressed));

            var target = interactor != null ? interactor.Current : null;
            var anchor = target as AnchorSite;

            return
                $"seen      {AnchorSite.MonsterSeen}\n" +
                $"cursor    {Cursor.lockState}\n" +
                $"target    {(target == null ? "none" : target.GetType().Name)}" +
                $"  can={(target != null && target.CanInteract)}\n" +
                $"holding   {holdDown}" +
                (anchor != null ? $"   burn {anchor.Progress * 100f:F0}%  feeding={anchor.BeingBurned}" : "") +
                "\n\n";
        }

        /// <summary>
        /// Prompt and burn meter for whatever the interactor currently has, so this
        /// works for every interactable rather than just anchors.
        /// </summary>
        void TickAnchorPrompt()
        {
            var target = interactor != null ? interactor.Current : null;
            var anchor = target as AnchorSite;
            bool show = target != null && target.CanInteract;

            if (prompt != null)
            {
                prompt.enabled = show;
                if (show) prompt.text = $"[LMB] or [E]  {target.Prompt}";
            }

            // Only a hold interactable has a meter worth drawing.
            bool meter = show && anchor != null;
            if (burnFill != null)
            {
                burnFill.transform.parent.gameObject.SetActive(meter);
                if (meter) burnFill.fillAmount = anchor.Progress;
            }
        }
    }
}
