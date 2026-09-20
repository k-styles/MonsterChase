using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MonsterChase.Core;

namespace MonsterChase.Player
{
    /// <summary>
    /// Being caught. Freezes the player, fades to red, restarts the run.
    ///
    /// The ritual is deliberately not reset on death: the bodies you burned stay
    /// burned, so a run you died on was still progress. Otherwise every death throws
    /// away the one thing the game asks you to do.
    /// </summary>
    public class PlayerLife : MonoBehaviour
    {
        [SerializeField] Image bloodOverlay;
        [SerializeField] Text deathText;
        [SerializeField] float holdSeconds = 2.6f;

        bool dying;
        float timer;

        void OnEnable() => GameEvents.PlayerCaught += OnCaught;
        void OnDisable() => GameEvents.PlayerCaught -= OnCaught;

        void OnCaught()
        {
            if (dying) return;
            dying = true;
            timer = 0f;

            var controller = GetComponent<FirstPersonController>();
            if (controller != null) controller.enabled = false;

            var gun = GetComponent<Gun>();
            if (gun != null) gun.enabled = false;

            if (deathText != null)
            {
                deathText.enabled = true;
                deathText.text = "it found you";
            }
        }

        void Update()
        {
            if (!dying) return;

            timer += Time.unscaledDeltaTime;
            if (bloodOverlay != null)
                bloodOverlay.color = new Color(0.35f, 0.02f, 0.02f, Mathf.Clamp01(timer / holdSeconds));

            if (timer < holdSeconds) return;

            GameEvents.ClearAll();
            FirstPersonController.LockCursor(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
