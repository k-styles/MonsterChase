using System;
using UnityEngine;

namespace MonsterChase.Ritual
{
    /// <summary>
    /// The single authoritative owner of how far the ritual has got. Nothing else
    /// writes this. Anchors ask it to burn them; everything else reads it.
    ///
    /// Netcode-shaped on purpose: when co-op lands, this object becomes the server's
    /// and <see cref="Burned"/> becomes a replicated variable. Because no other script
    /// mutates ritual state, that change stays contained here instead of rippling
    /// through every anchor, gun and door in the project.
    /// </summary>
    public class RitualState : MonoBehaviour
    {
        public static RitualState Instance { get; private set; }

        [SerializeField] int totalAnchors = 5;

        /// <summary>Anchors burned so far.</summary>
        public int Burned { get; private set; }

        public int Total => totalAnchors;
        public bool Complete => Burned >= totalAnchors;

        /// <summary>0 at the start, 1 once every anchor is burned.</summary>
        public float Progress => totalAnchors <= 0 ? 1f : Mathf.Clamp01((float)Burned / totalAnchors);

        /// <summary>Raised with (burned, total) whenever an anchor goes up.</summary>
        public event Action<int, int> AnchorBurned;

        void Awake()
        {
            // Last one wins rather than first, so reloading a scene cannot leave a
            // destroyed instance behind that every anchor then talks to.
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Called by an anchor as it burns. Returns false if it was already counted.</summary>
        public bool RegisterBurn()
        {
            if (Burned >= totalAnchors) return false;
            Burned++;
            AnchorBurned?.Invoke(Burned, totalAnchors);
            return true;
        }

        public void ResetRitual() => Burned = 0;
    }
}
