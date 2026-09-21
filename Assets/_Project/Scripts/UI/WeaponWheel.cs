using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MonsterChase.Player;

namespace MonsterChase.UI
{
    /// <summary>
    /// The weapon list, shown while the selector is open. Deliberately a plain column
    /// rather than a radial menu: it is read at a glance while something is chasing you,
    /// and a radial needs the cursor, which the game does not have.
    /// </summary>
    public class WeaponWheel : MonoBehaviour
    {
        [SerializeField] GunLoadout loadout;
        [SerializeField] Text label;
        [SerializeField] CanvasGroup group;
        [SerializeField] float fadeSeconds = 0.18f;

        readonly List<string> lines = new List<string>();

        void Awake()
        {
            if (loadout == null) loadout = Object.FindFirstObjectByType<GunLoadout>();
            if (group != null) group.alpha = 0f;
        }

        void Update()
        {
            if (loadout == null || label == null) return;

            if (group != null)
                group.alpha = Mathf.MoveTowards(group.alpha, loadout.SelectorOpen ? 1f : 0f,
                                                Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));

            if (!loadout.SelectorOpen && (group == null || group.alpha <= 0.01f)) return;

            lines.Clear();
            for (int i = 0; i < loadout.Count; i++)
            {
                var w = loadout.Weapons[i];
                bool on = i == loadout.Index;
                lines.Add(on ? $"> {w.name}   {w.magazine} rnd" : $"   {w.name}");
            }
            label.text = string.Join("\n", lines);
        }
    }
}
