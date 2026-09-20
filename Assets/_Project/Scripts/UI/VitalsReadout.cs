using UnityEngine;
using UnityEngine.UI;
using MonsterChase.Monster;
using MonsterChase.Ritual;

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

        AnchorSite[] anchors;
        bool visible = true;

        void Start()
        {
            if (monster == null) monster = Object.FindFirstObjectByType<MonsterVitals>();
            anchors = Object.FindObjectsByType<AnchorSite>(FindObjectsSortMode.None);
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

        /// <summary>Shows the burn meter for whichever anchor you are standing at.</summary>
        void TickAnchorPrompt()
        {
            AnchorSite near = null;
            if (anchors != null)
                foreach (var a in anchors)
                    if (a != null && !a.Burnt && a.PlayerInRange) { near = a; break; }

            bool show = near != null;
            if (prompt != null)
            {
                prompt.enabled = show;
                if (show) prompt.text = "hold [F] to burn";
            }
            if (burnFill != null)
            {
                burnFill.transform.parent.gameObject.SetActive(show);
                if (show) burnFill.fillAmount = near.Progress;
            }
        }
    }
}
