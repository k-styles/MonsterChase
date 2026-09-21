using UnityEngine;

namespace MonsterChase.Core
{
    /// <summary>
    /// Keeps the renderer from running unbounded.
    ///
    /// Left alone, Unity draws as many frames as the hardware will give it. In a quiet
    /// scene that is thousands a second of no benefit, and on a fanless laptop it is
    /// just heat -- which then throttles the frames that actually matter, during a
    /// chase. Vertical sync ties it to the display instead.
    /// </summary>
    public class FrameRate : MonoBehaviour
    {
        [Tooltip("0 disables vsync and uses the cap below instead.")]
        [SerializeField] int vSyncCount = 1;
        [Tooltip("Only used when vsync is off. -1 means uncapped.")]
        [SerializeField] int targetFrameRate = 120;

        void Awake()
        {
            QualitySettings.vSyncCount = Mathf.Max(0, vSyncCount);
            Application.targetFrameRate = vSyncCount > 0 ? -1 : targetFrameRate;
            DontDestroyOnLoad(gameObject);
        }
    }
}
