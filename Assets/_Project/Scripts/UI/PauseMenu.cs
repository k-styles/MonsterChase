using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MonsterChase.Player;

namespace MonsterChase.UI
{
    /// <summary>
    /// Escape during play. Owns the Escape key outright so the controller and this
    /// cannot both react to the same press and fight over the cursor.
    ///
    /// Pausing sets timeScale to 0, so the settings sliders are safe to fiddle with
    /// while something is standing over you.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameObject rootPanel;
        [SerializeField] SettingsPanel settings;
        [SerializeField] Button resumeButton;
        [SerializeField] Button controlsButton;
        [SerializeField] Button menuButton;
        [SerializeField] string menuScene = "Menu";

        [Header("While paused")]
        [Tooltip("Frame cap for the pause screen. With timeScale at 0 there is nothing to simulate, so the renderer will otherwise run flat out over a static image and cook a fanless machine.")]
        [SerializeField] int pausedFrameRate = 30;

        public bool Paused { get; private set; }

        int frameRateBeforePause;

        void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (controlsButton != null) controlsButton.onClick.AddListener(() => ShowSettings(true));
            if (menuButton != null) menuButton.onClick.AddListener(ToMenu);
            if (settings != null) settings.Closed += () => ShowSettings(false);

            SetPaused(false);
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            Application.targetFrameRate = frameRateBeforePause == 0 ? -1 : frameRateBeforePause;
        }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

            // Escape backs out of the settings first, then out of the pause screen.
            if (settings != null && settings.gameObject.activeSelf) ShowSettings(false);
            else SetPaused(!Paused);
        }

        void Resume() => SetPaused(false);

        void ShowSettings(bool show)
        {
            if (settings != null) settings.gameObject.SetActive(show);
            if (rootPanel != null) rootPanel.SetActive(!show);
        }

        void SetPaused(bool paused)
        {
            Paused = paused;

            if (rootPanel != null) rootPanel.SetActive(paused);
            if (!paused && settings != null) settings.gameObject.SetActive(false);

            Time.timeScale = paused ? 0f : 1f;

            if (paused)
            {
                frameRateBeforePause = Application.targetFrameRate;
                Application.targetFrameRate = pausedFrameRate;
            }
            else
            {
                Application.targetFrameRate = frameRateBeforePause == 0 ? -1 : frameRateBeforePause;
            }

            FirstPersonController.LockCursor(!paused);

            // Stop the player looking around behind the menu.
            var controller = Object.FindFirstObjectByType<FirstPersonController>();
            if (controller != null) controller.enabled = !paused;
        }

        void ToMenu()
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(menuScene)) SceneManager.LoadScene(menuScene);
            else Debug.LogError($"[Pause] Scene '{menuScene}' is not in Build Settings.");
        }
    }
}
