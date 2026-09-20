using UnityEngine;
using UnityEngine.UI;
using MonsterChase.Core;

namespace MonsterChase.UI
{
    /// <summary>
    /// The options screen. One component drives it in both places it appears -- the
    /// main menu and the in-game pause screen -- so the two can never drift apart.
    ///
    /// Sliders write straight to GameSettings, which saves and raises Changed, which
    /// the player rig is listening to. Move the mouse-sensitivity slider while paused
    /// and the camera is already using the new value when you unpause.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] Slider mouseSlider;
        [SerializeField] Text mouseValue;
        [SerializeField] Slider arrowSlider;
        [SerializeField] Text arrowValue;
        [SerializeField] Slider fovSlider;
        [SerializeField] Text fovValue;
        [SerializeField] Slider volumeSlider;
        [SerializeField] Text volumeValue;

        [Header("Toggles")]
        [SerializeField] Button invertButton;
        [SerializeField] Text invertValue;
        [SerializeField] Button crouchButton;
        [SerializeField] Text crouchValue;

        [Header("Actions")]
        [SerializeField] Button resetButton;
        [SerializeField] Button backButton;

        /// <summary>Raised when the player leaves the panel.</summary>
        public event System.Action Closed;

        bool writing;

        void Awake()
        {
            if (mouseSlider != null) mouseSlider.onValueChanged.AddListener(OnMouse);
            if (arrowSlider != null) arrowSlider.onValueChanged.AddListener(OnArrow);
            if (fovSlider != null) fovSlider.onValueChanged.AddListener(OnFov);
            if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolume);

            if (invertButton != null)
                invertButton.onClick.AddListener(() => { GameSettings.InvertY = !GameSettings.InvertY; Refresh(); });
            if (crouchButton != null)
                crouchButton.onClick.AddListener(() => { GameSettings.HoldToCrouch = !GameSettings.HoldToCrouch; Refresh(); });
            if (resetButton != null)
                resetButton.onClick.AddListener(() => { GameSettings.ResetToDefaults(); Refresh(); });
            if (backButton != null)
                backButton.onClick.AddListener(() => Closed?.Invoke());
        }

        void OnEnable() => Refresh();

        // Guarded, because assigning slider.value fires onValueChanged, which would
        // write the value straight back into settings and fight the user's drag.
        void OnMouse(float t)  { if (!writing) { GameSettings.MouseSensitivity = GameSettings.Denormalise(t, GameSettings.MouseMin, GameSettings.MouseMax); Labels(); } }
        void OnArrow(float t)  { if (!writing) { GameSettings.ArrowSensitivity = GameSettings.Denormalise(t, GameSettings.ArrowMin, GameSettings.ArrowMax); Labels(); } }
        void OnFov(float t)    { if (!writing) { GameSettings.FieldOfView = GameSettings.Denormalise(t, GameSettings.FovMin, GameSettings.FovMax); Labels(); } }
        void OnVolume(float t) { if (!writing) { GameSettings.MasterVolume = t; Labels(); } }

        public void Refresh()
        {
            writing = true;
            if (mouseSlider != null)  mouseSlider.value  = GameSettings.Normalise(GameSettings.MouseSensitivity, GameSettings.MouseMin, GameSettings.MouseMax);
            if (arrowSlider != null)  arrowSlider.value  = GameSettings.Normalise(GameSettings.ArrowSensitivity, GameSettings.ArrowMin, GameSettings.ArrowMax);
            if (fovSlider != null)    fovSlider.value    = GameSettings.Normalise(GameSettings.FieldOfView, GameSettings.FovMin, GameSettings.FovMax);
            if (volumeSlider != null) volumeSlider.value = GameSettings.MasterVolume;
            writing = false;
            Labels();
        }

        void Labels()
        {
            if (mouseValue != null)  mouseValue.text  = GameSettings.MouseSensitivity.ToString("0.00");
            if (arrowValue != null)  arrowValue.text  = $"{GameSettings.ArrowSensitivity:0}°/s";
            if (fovValue != null)    fovValue.text    = $"{GameSettings.FieldOfView:0}";
            if (volumeValue != null) volumeValue.text = $"{GameSettings.MasterVolume * 100f:0}%";
            if (invertValue != null) invertValue.text = GameSettings.InvertY ? "on" : "off";
            if (crouchValue != null) crouchValue.text = GameSettings.HoldToCrouch ? "hold" : "toggle";
        }
    }
}
