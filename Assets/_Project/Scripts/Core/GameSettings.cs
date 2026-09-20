using System;
using UnityEngine;

namespace MonsterChase.Core
{
    /// <summary>
    /// Every player-facing setting, in one place, saved to PlayerPrefs.
    ///
    /// Static rather than a component because settings outlive scenes: the menu writes
    /// them, the player rig reads them, and neither has to find the other. Anything
    /// that cares about a live change subscribes to <see cref="Changed"/> instead of
    /// polling, so moving a slider updates the game underneath the menu immediately.
    /// </summary>
    public static class GameSettings
    {
        const string KeyMouse = "mc.mouseSensitivity";
        const string KeyArrow = "mc.arrowSensitivity";
        const string KeyInvert = "mc.invertY";
        const string KeyFov = "mc.fieldOfView";
        const string KeyMaster = "mc.masterVolume";
        const string KeyHoldCrouch = "mc.holdToCrouch";

        public const float MouseMin = 0.02f, MouseMax = 0.40f, MouseDefault = 0.11f;
        public const float ArrowMin = 40f,  ArrowMax = 400f,  ArrowDefault = 140f;
        public const float FovMin = 60f,    FovMax = 100f,    FovDefault = 70f;

        static float mouse = MouseDefault;
        static float arrow = ArrowDefault;
        static bool invertY;
        static float fov = FovDefault;
        static float master = 1f;
        static bool holdToCrouch = true;
        static bool loaded;

        /// <summary>Raised on any change, so live objects can re-read immediately.</summary>
        public static event Action Changed;

        /// <summary>Degrees of yaw per pixel of mouse movement.</summary>
        public static float MouseSensitivity
        {
            get { Load(); return mouse; }
            set { Load(); mouse = Mathf.Clamp(value, MouseMin, MouseMax); Save(KeyMouse, mouse); }
        }

        /// <summary>Degrees per second when looking with the arrow keys.</summary>
        public static float ArrowSensitivity
        {
            get { Load(); return arrow; }
            set { Load(); arrow = Mathf.Clamp(value, ArrowMin, ArrowMax); Save(KeyArrow, arrow); }
        }

        public static bool InvertY
        {
            get { Load(); return invertY; }
            set { Load(); invertY = value; PlayerPrefs.SetInt(KeyInvert, value ? 1 : 0); Flush(); }
        }

        public static float FieldOfView
        {
            get { Load(); return fov; }
            set { Load(); fov = Mathf.Clamp(value, FovMin, FovMax); Save(KeyFov, fov); }
        }

        public static float MasterVolume
        {
            get { Load(); return master; }
            set
            {
                Load();
                master = Mathf.Clamp01(value);
                AudioListener.volume = master;
                Save(KeyMaster, master);
            }
        }

        /// <summary>True: crouch while held. False: press to toggle.</summary>
        public static bool HoldToCrouch
        {
            get { Load(); return holdToCrouch; }
            set { Load(); holdToCrouch = value; PlayerPrefs.SetInt(KeyHoldCrouch, value ? 1 : 0); Flush(); }
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;   // set first: the getters below would otherwise recurse

            mouse = PlayerPrefs.GetFloat(KeyMouse, MouseDefault);
            arrow = PlayerPrefs.GetFloat(KeyArrow, ArrowDefault);
            invertY = PlayerPrefs.GetInt(KeyInvert, 0) == 1;
            fov = PlayerPrefs.GetFloat(KeyFov, FovDefault);
            master = PlayerPrefs.GetFloat(KeyMaster, 1f);
            holdToCrouch = PlayerPrefs.GetInt(KeyHoldCrouch, 1) == 1;

            AudioListener.volume = master;
        }

        static void Save(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            Flush();
        }

        static void Flush()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void ResetToDefaults()
        {
            Load();
            mouse = MouseDefault;
            arrow = ArrowDefault;
            invertY = false;
            fov = FovDefault;
            master = 1f;
            holdToCrouch = true;

            PlayerPrefs.SetFloat(KeyMouse, mouse);
            PlayerPrefs.SetFloat(KeyArrow, arrow);
            PlayerPrefs.SetInt(KeyInvert, 0);
            PlayerPrefs.SetFloat(KeyFov, fov);
            PlayerPrefs.SetFloat(KeyMaster, master);
            PlayerPrefs.SetInt(KeyHoldCrouch, 1);
            AudioListener.volume = master;
            Flush();
        }

        /// <summary>0..1 position of a value on its slider, and back.</summary>
        public static float Normalise(float value, float min, float max) =>
            Mathf.Approximately(max, min) ? 0f : Mathf.Clamp01((value - min) / (max - min));

        public static float Denormalise(float t, float min, float max) =>
            Mathf.Lerp(min, max, Mathf.Clamp01(t));
    }
}
