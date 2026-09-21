using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MonsterChase.Player
{
    /// <summary>
    /// The guns you carry and how you swap between them. Click the scroll wheel to open
    /// the selector, roll it to move through the list, click again or wait to commit.
    ///
    /// The wheel does not pause the game. Choosing a weapon while something is running
    /// at you is meant to cost you the seconds it takes.
    /// </summary>
    public class GunLoadout : MonoBehaviour
    {
        [System.Serializable]
        public class Weapon
        {
            public string name = "pistol";
            [Tooltip("The model under the view model rig. Only one is active at a time.")]
            public GameObject model;
            public Transform muzzle;

            [Header("Stats")]
            public float damage = 26f;
            public float shotsPerSecond = 4f;
            public int magazine = 12;
            public int reserveMax = 96;
            [Tooltip("Rounds this weapon starts the run with, outside the magazine.")]
            public int startingReserve = 24;
        }

        [SerializeField] List<Weapon> weapons = new List<Weapon>();
        [SerializeField] Gun gun;
        [SerializeField] GunViewModel viewModel;
        [SerializeField] GunFx fx;

        [Header("Selector")]
        [Tooltip("Seconds of no input before the selector closes on its own.")]
        [SerializeField] float selectorTimeout = 2.5f;

        public int Count => weapons.Count;
        public int Index { get; private set; }
        public bool SelectorOpen { get; private set; }
        public IReadOnlyList<Weapon> Weapons => weapons;

        float closeAt;

        void Awake()
        {
            if (gun == null) gun = GetComponentInChildren<Gun>();
            if (viewModel == null) viewModel = GetComponentInChildren<GunViewModel>();
            if (fx == null) fx = GetComponentInChildren<GunFx>();
            Equip(0, keepAmmo: false);
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.middleButton.wasPressedThisFrame)
            {
                SelectorOpen = !SelectorOpen;
                closeAt = Time.time + selectorTimeout;
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // Rolling the wheel is itself a request to swap, so it opens the
                // selector rather than needing the click first.
                SelectorOpen = true;
                closeAt = Time.time + selectorTimeout;

                int step = scroll > 0f ? 1 : -1;
                Equip(WrapIndex(Index + step), keepAmmo: true);
            }

            if (SelectorOpen && Time.time >= closeAt) SelectorOpen = false;
        }

        int WrapIndex(int i)
        {
            if (weapons.Count == 0) return 0;
            return ((i % weapons.Count) + weapons.Count) % weapons.Count;
        }

        public void Equip(int index, bool keepAmmo)
        {
            if (weapons.Count == 0) return;

            Index = WrapIndex(index);
            var w = weapons[Index];

            for (int i = 0; i < weapons.Count; i++)
                if (weapons[i].model != null) weapons[i].model.SetActive(i == Index);

            if (viewModel != null && w.model != null) viewModel.SetModel(w.model.transform, w.muzzle);
            if (fx != null && w.muzzle != null) fx.SetMuzzle(w.muzzle);

            if (gun != null)
                gun.Configure(w.damage, w.shotsPerSecond, w.magazine, w.reserveMax,
                              keepAmmo ? -1 : w.startingReserve);
        }
    }
}
