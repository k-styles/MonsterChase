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
        [SerializeField] GunFx fx;
        [SerializeField] float damage = 26f;
        [SerializeField] float shotsPerSecond = 4f;
        [SerializeField] float range = 90f;
        [SerializeField] int magazine = 12;
        [Tooltip("Rounds carried outside the magazine. Reloading draws from here.")]
        [SerializeField] int reserve = 24;
        [SerializeField] int reserveMax = 96;
        [SerializeField] float reloadSeconds = 1.8f;
        [SerializeField] LayerMask hits = ~0;

        [Header("Noise")]
        [Tooltip("How far a shot carries. The monster comes to look, and walks the spot once.")]
        [SerializeField] float shotNoiseRadius = 70f;

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
        bool reloading;

        public int Ammo { get; private set; }
        public int Magazine => magazine;
        public int Reserve => reserve;
        public int ReserveMax => reserveMax;
        public bool Reloading => reloading;

        /// <summary>Returns what was actually taken, so a full player leaves the box alone.</summary>
        public int AddAmmo(int rounds)
        {
            int before = reserve;
            reserve = Mathf.Clamp(reserve + rounds, 0, reserveMax);
            return reserve - before;
        }

        void Awake()
        {
            Ammo = magazine;
            if (sourceCamera == null) sourceCamera = Camera.main;
            if (interactor == null) interactor = GetComponent<Interactor>();
            if (viewModel == null) viewModel = GetComponentInChildren<GunViewModel>();
            if (fx == null) fx = GetComponentInChildren<GunFx>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) BeginReload();

            if (reloading)
            {
                TickReloadAudio();

                // This used to sit inside `if (Reloading)` where Reloading meant
                // "Time.time < reloadUntil", so the completion branch could never be
                // reached and the magazine never refilled. Hence the explicit flag.
                if (Time.time >= reloadUntil) FinishReload();
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
            // Nothing to reload from is not a reload. No timer, and no reload sound --
            // hearing a magazine change with an empty pouch is what made it feel broken.
            if (reloading || Ammo == magazine || reserve <= 0) return;

            reloading = true;
            reloadUntil = Time.time + reloadSeconds;
            reloadStep = 0;
            nextReloadStep = Time.time;
        }

        void FinishReload()
        {
            int wanted = magazine - Ammo;
            int taken = Mathf.Min(wanted, reserve);
            Ammo += taken;
            reserve -= taken;
            reloading = false;
        }

        /// <summary>
        /// Walks the reload clips across the reload rather than stacking them on one
        /// frame: slide lock, magazine out, magazine in, slide release.
        /// </summary>
        void TickReloadAudio()
        {
            if (!reloading) return;
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
                // Dry click either way; only actually reload if there is something left.
                if (fireAudio != null && drySound != null) fireAudio.PlayOneShot(drySound, 0.5f);
                if (reserve > 0) BeginReload();
                return;
            }

            Ammo--;
            PlayShot();

            // Firing is the loudest thing in the game. It should cost you.
            MonsterChase.Core.GameEvents.RaiseNoise(transform.position, shotNoiseRadius);
            if (viewModel != null) viewModel.Fired();

            if (sourceCamera == null) return;

            // Fired through the crosshair itself rather than the camera's forward axis.
            // They are the same only when the viewport is centred and undistorted; this
            // is always exactly where the dot is drawn.
            var ray = sourceCamera.ScreenPointToRay(
                new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            bool connected = Physics.Raycast(ray, out var hit, range, hits, QueryTriggerInteraction.Ignore);

            // The tracer starts at the barrel, not the camera, or every shot appears to
            // come out of the player's forehead.
            var from = fx != null ? fx.Muzzle.position
                     : viewModel != null ? viewModel.Muzzle.position
                     : ray.origin;
            var to = connected ? hit.point : ray.origin + ray.direction * range;
            if (fx != null) fx.Shot(from, to, connected, connected ? hit.normal : -ray.direction);

            if (!connected) return;

            var vitals = hit.collider.GetComponentInParent<MonsterVitals>();
            if (vitals == null) return;

            vitals.TakeDamage(damage);

            // Blood only where there is something to bleed. A spray off a wall would
            // read as a hit and teach the player the wrong thing about what connected.
            if (Impacts.Instance != null) Impacts.Instance.Blood(hit.point, hit.normal);
        }
    }
}
