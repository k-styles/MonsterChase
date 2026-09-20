using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MonsterChase.UI
{
    /// <summary>Title screen: play, controls, quit.</summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] GameObject rootPanel;
        [SerializeField] SettingsPanel settings;
        [SerializeField] Button playButton;
        [SerializeField] Button controlsButton;
        [SerializeField] Button quitButton;
        [SerializeField] string playScene = "Testbed";

        void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (controlsButton != null) controlsButton.onClick.AddListener(() => ShowSettings(true));
            if (quitButton != null) quitButton.onClick.AddListener(Quit);

            if (settings != null)
            {
                settings.Closed += () => ShowSettings(false);
                settings.gameObject.SetActive(false);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void ShowSettings(bool show)
        {
            if (settings != null) settings.gameObject.SetActive(show);
            if (rootPanel != null) rootPanel.SetActive(!show);
        }

        void Play()
        {
            if (Application.CanStreamedLevelBeLoaded(playScene)) SceneManager.LoadScene(playScene);
            else Debug.LogError($"[Menu] Scene '{playScene}' is not in Build Settings, so it cannot load.");
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
