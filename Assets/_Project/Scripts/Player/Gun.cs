using UnityEngine;
using UnityEngine.InputSystem;
using MonsterChase.Monster;
using MonsterChase.Interaction;

namespace MonsterChase.Player
{
    /// <summary>
    /// Hitscan. Deliberately satisfying to fire and, for most of the game, useless --
    /// the monster heals whatever this takes off. That gap between how good it feels
    /// and how little it achieves is the thing the anchors eventually close.
    /// </summary>
    public class Gun : MonoBehaviour
    {
        [SerializeField] Camera sourceCamera;
        [SerializeField] Interactor interactor;
        [SerializeField] GunViewModel viewModel;
        [SerializeField] float damage = 26f;
        [SerializeField] float shotsPerSecond = 4f;
        [SerializeField] float range = 90f;
        [SerializeField] int magazine = 12;
        [SerializeField] float reloadSeconds = 1.8f;
        [SerializeField] LayerMask hits = ~0;

        [Header("Feel")]
        [SerializeField] AudioSource fireAudio;
        [Tooltip("Picked at random per shot so a magazine does not sound like a metronome.")]
        [SerializeField] AudioClip[] fireClips;
        [Tooltip("The report's tail, played under the shot at lower volume.")]
        [SerializeField] AudioClip[] tailClips;
        [SerializeField] AudioClip drySound;
        [Tooltip("Played in order across the reload, not all at once.")]
        [SerializeField] AudioClip[] reloadClips;

        float nextShot;
        float reloadUntil;
        int reloadStep;
        float nextReloadStep;

        public int Ammo { get; private set; }
        public int Magazine => magazine;
        public bool Reloading => Time.time < reloadUntil;

        void Awake()
        {
            Ammo = magazine;
            if (sourceCamera == null) sourceCamera = Camera.main;
            if (interactor == null) interactor = GetComponent<Interactor>();
            if (viewModel == null) viewModel = GetComponentInChildren<GunViewModel>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) BeginReload();

            if (Reloading)
            {
                TickReloadAudio();
                if (Time.time >= reloadUntil && Ammo == 0) Ammo = magazine;
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.isPressed) return;
            if (Time.time < nextShot || Cursor.lockState != CursorLockMode.Locked) return;

            // Looking at a door, a body, a pickup? The click belongs to that, not to
            // the gun. One button, and it always does the sensible thing.
            if (interactor != null && interactor.HasTarget) return;

            Fire();
        }

        void BeginReload()
        {
            if (Reloading || Ammo == magazine) return;
            reloadUntil = Time.time + reloadSeconds;
            reloadStep = 0;
            nextReloadStep = Time.time;
        }

        /// <summary>
        /// Walks the reload clips across the reload rather than stacking them on one
        /// frame: slide lock, magazine out, magazine in, slide release.
        /// </summary>
        void TickReloadAudio()
        {
            if (fireAudio == null || reloadClips == null || reloadClips.Length == 0) return;
            if (reloadStep >= reloadClips.Length || Time.time < nextReloadStep) return;

            var clip = reloadClips[reloadStep];
            if (clip != null) fireAudio.PlayOneShot(clip, 0.8f);

            reloadStep++;
            nextReloadStep = Time.time + reloadSeconds / Mathf.Max(1, reloadClips.Length);
        }

        void PlayShot()
        {
            if (fireAudio == null) return;

            if (fireClips != null && fireClips.Length > 0)
            {
                var clip = fireClips[Random.Range(0, fireClips.Length)];
                if (clip != null) fireAudio.PlayOneShot(clip);
            }
            if (tailClips != null && tailClips.Length > 0)
            {
                var tail = tailClips[Random.Range(0, tailClips.Length)];
                if (tail != null) fireAudio.PlayOneShot(tail, 0.45f);
            }
        }

        void Fire()
        {
            nextShot = Time.time + 1f / Mathf.Max(0.01f, shotsPerSecond);

            if (Ammo <= 0)
            {
                if (fireAudio != null && drySound != null) fireAudio.PlayOneShot(drySound, 0.5f);
                BeginReload();
                return;
            }

            Ammo--;
            PlayShot();
            if (viewModel != null) viewModel.Fired();

            if (sourceCamera == null) return;
            var ray = new Ray(sourceCamera.transform.position, sourceCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, range, hits, QueryTriggerInteraction.Ignore)) return;

            var vitals = hit.collider.GetComponentInParent<MonsterVitals>();
            if (vitals == null) return;

            vitals.TakeDamage(damage);

            // Blood only where there is something to bleed. A spray off a wall would
            // read as a hit and teach the player the wrong thing about what connected.
            if (Impacts.Instance != null) Impacts.Instance.Blood(hit.point, hit.normal);
        }
    }
}
