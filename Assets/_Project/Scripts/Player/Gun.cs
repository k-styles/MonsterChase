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
        [SerializeField] float damage = 26f;
        [SerializeField] float shotsPerSecond = 4f;
        [SerializeField] float range = 90f;
        [SerializeField] int magazine = 12;
        [SerializeField] float reloadSeconds = 1.8f;
        [SerializeField] LayerMask hits = ~0;

        [Header("Feel")]
        [SerializeField] AudioSource fireAudio;
        [SerializeField] AudioClip fireClip;
        [SerializeField] AudioClip drySound;

        float nextShot;
        float reloadUntil;

        public int Ammo { get; private set; }
        public int Magazine => magazine;
        public bool Reloading => Time.time < reloadUntil;

        void Awake()
        {
            Ammo = magazine;
            if (sourceCamera == null) sourceCamera = Camera.main;
            if (interactor == null) interactor = GetComponent<Interactor>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) BeginReload();

            if (Reloading)
            {
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
            if (fireAudio != null && fireClip != null) fireAudio.PlayOneShot(fireClip);

            if (sourceCamera == null) return;
            var ray = new Ray(sourceCamera.transform.position, sourceCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, range, hits, QueryTriggerInteraction.Ignore)) return;

            var vitals = hit.collider.GetComponentInParent<MonsterVitals>();
            if (vitals != null) vitals.TakeDamage(damage);
        }
    }
}
