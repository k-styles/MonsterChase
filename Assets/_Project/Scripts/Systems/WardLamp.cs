using System.Collections;
using UnityEngine;

namespace MonsterChase.Systems
{
    /// <summary>
    /// One strip light in the ward. It can be killed, and killing it is the only
    /// lighting cue the chase has: the corridor behind you stops existing a few
    /// metres at a time, so the dark is arriving rather than already there.
    /// </summary>
    public class WardLamp : MonoBehaviour
    {
        [SerializeField] Light bulb;
        [SerializeField] Renderer shade;
        [Tooltip("Swapped onto the shade once the lamp is dead, so it reads as off in silhouette.")]
        [SerializeField] Material deadMaterial;
        [SerializeField] AudioSource pop;

        [Header("Dying")]
        [SerializeField] Vector2 flickerCount = new Vector2(2f, 5f);
        [SerializeField] Vector2 flickerGap = new Vector2(0.03f, 0.13f);

        float fullIntensity;
        bool dead;

        public bool IsDead => dead;

        void Awake()
        {
            if (bulb == null) bulb = GetComponentInChildren<Light>();
            if (bulb != null) fullIntensity = bulb.intensity;
        }

        /// <summary>Flicker a couple of times and go out. Does nothing if already dead.</summary>
        public void Kill()
        {
            if (dead) return;
            dead = true;
            if (isActiveAndEnabled) StartCoroutine(Dying());
            else GoDark();
        }

        IEnumerator Dying()
        {
            int flickers = Mathf.RoundToInt(Random.Range(flickerCount.x, flickerCount.y));
            for (int i = 0; i < flickers; i++)
            {
                if (bulb != null) bulb.intensity = 0f;
                yield return new WaitForSeconds(Random.Range(flickerGap.x, flickerGap.y));
                if (bulb != null) bulb.intensity = fullIntensity * Random.Range(0.4f, 1f);
                yield return new WaitForSeconds(Random.Range(flickerGap.x, flickerGap.y));
            }

            if (pop != null) pop.Play();
            GoDark();
        }

        void GoDark()
        {
            if (bulb != null) bulb.enabled = false;
            if (shade != null && deadMaterial != null) shade.sharedMaterial = deadMaterial;
        }

        /// <summary>
        /// Guttering rather than dying: the light that is still deciding. Used for the
        /// opening, where the ward is dark and only a few fixtures are doing anything.
        /// </summary>
        public void StartFlickering(float onChance = 0.35f, Vector2? gap = null)
        {
            if (dead) return;
            StopAllCoroutines();
            StartCoroutine(Flickering(onChance, gap ?? new Vector2(0.05f, 0.45f)));
        }

        IEnumerator Flickering(float onChance, Vector2 gap)
        {
            while (true)
            {
                bool on = Random.value < onChance;
                if (bulb != null)
                {
                    bulb.enabled = true;
                    bulb.intensity = on ? fullIntensity * Random.Range(0.55f, 1f) : 0f;
                }
                yield return new WaitForSeconds(Random.Range(gap.x, gap.y));
            }
        }

        /// <summary>Stop guttering and hold steady at full.</summary>
        public void Settle()
        {
            if (dead) return;
            StopAllCoroutines();
            if (bulb != null) { bulb.enabled = true; bulb.intensity = fullIntensity; }
        }

        /// <summary>Off, without the dying animation. For setting up a dark room.</summary>
        public void Douse()
        {
            StopAllCoroutines();
            if (bulb != null) bulb.enabled = false;
        }

        /// <summary>Back on, for rebuilding or restarting the run without a reload.</summary>
        public void Revive()
        {
            StopAllCoroutines();
            dead = false;
            if (bulb != null)
            {
                bulb.enabled = true;
                bulb.intensity = fullIntensity;
            }
        }
    }
}
