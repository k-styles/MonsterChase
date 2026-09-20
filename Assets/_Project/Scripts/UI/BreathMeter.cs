using UnityEngine;
using UnityEngine.UI;
using MonsterChase.Systems;

namespace MonsterChase.UI
{
    /// <summary>
    /// How much breath is left. Goes red as it runs out, because running out is not a
    /// soft failure -- it gasps, and the gasp is the loudest thing you do all night.
    /// </summary>
    public class BreathMeter : MonoBehaviour
    {
        [SerializeField] PlayerBreath breath;
        [SerializeField] Image fill;
        [SerializeField] CanvasGroup group;

        void Awake()
        {
            if (breath == null) breath = Object.FindFirstObjectByType<PlayerBreath>();
        }

        void Update()
        {
            if (breath == null || fill == null) return;

            fill.fillAmount = breath.HoldNormalized;
            fill.color = breath.Locked
                ? new Color(0.8f, 0.25f, 0.2f)
                : Color.Lerp(new Color(0.85f, 0.4f, 0.25f), new Color(0.62f, 0.78f, 0.86f),
                             breath.HoldNormalized);

            // Only worth showing while it matters: holding, recovering, or locked out.
            if (group != null)
                group.alpha = Mathf.MoveTowards(group.alpha,
                    breath.IsHolding || breath.Locked || breath.HoldNormalized < 0.99f ? 1f : 0.25f,
                    3f * Time.deltaTime);
        }
    }
}
