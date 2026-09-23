using UnityEngine;

namespace MonsterChase.Monster
{
    /// <summary>
    /// Shuts the creature up when it finally dies.
    ///
    /// Its voice lives across several AudioSources -- the one-shot mouth, the constant
    /// spatialised growl -- and none of them knew death was possible, so the corpse
    /// kept snarling. Killing it is the payoff for the whole ritual; it should be
    /// followed by the first real silence in the run.
    /// </summary>
    [RequireComponent(typeof(MonsterVitals))]
    public class MonsterSilence : MonoBehaviour
    {
        [Tooltip("Seconds to fade out over. Instant is a cut, which reads as a bug.")]
        [SerializeField] float fadeSeconds = 1.2f;

        MonsterVitals vitals;
        AudioSource[] voices;
        float[] startVolumes;
        CreatureVoice voice;
        bool dying;
        float fade = 1f;

        void Awake()
        {
            vitals = GetComponent<MonsterVitals>();
            voices = GetComponentsInChildren<AudioSource>(true);
            voice = GetComponent<CreatureVoice>();

            // Remembered up front: reading them at death would capture whatever the
            // fade had already done on a previous frame.
            startVolumes = new float[voices.Length];
            for (int i = 0; i < voices.Length; i++)
                startVolumes[i] = voices[i] != null ? voices[i].volume : 0f;
        }

        void OnEnable() => vitals.Died += OnDied;
        void OnDisable() => vitals.Died -= OnDied;

        void OnDied()
        {
            if (dying) return;
            dying = true;

            // Stop it choosing anything new to say while the last sound fades.
            if (voice != null) voice.enabled = false;
        }

        void Update()
        {
            if (!dying) return;

            fade = Mathf.MoveTowards(fade, 0f, Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));

            for (int i = 0; i < voices.Length; i++)
            {
                var a = voices[i];
                if (a == null) continue;

                a.volume = startVolumes[i] * fade;
                if (fade > 0f) continue;

                a.loop = false;
                a.Stop();
            }

            if (fade <= 0f) enabled = false;
        }
    }
}
