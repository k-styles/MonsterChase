using System;
using UnityEngine;

namespace MonsterChase.Core
{
    /// <summary>
    /// A tiny event bus, so the player does not need a reference to the monster in
    /// order to be heard by it. Netcode-shaped: when co-op lands, the server
    /// subscribes and clients raise.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>(worldPosition, loudness in metres it carries).</summary>
        public static event Action<Vector3, float> NoiseMade;

        /// <summary>Raised when the monster reaches the player.</summary>
        public static event Action PlayerCaught;

        public static void RaiseNoise(Vector3 at, float radius) => NoiseMade?.Invoke(at, radius);
        public static void RaisePlayerCaught() => PlayerCaught?.Invoke();

        /// <summary>Scene reloads leave stale subscribers behind otherwise.</summary>
        public static void ClearAll()
        {
            NoiseMade = null;
            PlayerCaught = null;
        }
    }
}
