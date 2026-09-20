using UnityEngine;
using UnityEngine.UI;
using MonsterChase.Player;

namespace MonsterChase.UI
{
    /// <summary>Magazine over reserve, bottom right, the way a shooter does it.</summary>
    public class AmmoCounter : MonoBehaviour
    {
        [SerializeField] Gun gun;
        [SerializeField] Text label;
        [SerializeField] Color normal = new Color(0.92f, 0.92f, 0.94f);
        [SerializeField] Color low = new Color(0.86f, 0.35f, 0.28f);

        void Awake()
        {
            if (gun == null) gun = Object.FindFirstObjectByType<Gun>();
        }

        void Update()
        {
            if (gun == null || label == null) return;

            label.text = gun.Reloading ? "-- / " + gun.Reserve : $"{gun.Ammo} / {gun.Reserve}";

            // Red when the magazine is nearly out, or when there is nothing to reload from.
            bool worrying = gun.Ammo <= Mathf.Max(1, gun.Magazine / 4) || gun.Reserve == 0;
            label.color = worrying ? low : normal;
        }
    }
}
