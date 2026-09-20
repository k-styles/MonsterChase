using UnityEngine;

namespace MonsterChase.Player
{
    /// <summary>
    /// The gun you can see. Rides the camera, kicks when fired, sways a little as you
    /// move so it does not look welded to the screen.
    /// </summary>
    public class GunViewModel : MonoBehaviour
    {
        [SerializeField] Transform model;
        [SerializeField] Transform muzzle;

        [Header("Kick")]
        [SerializeField] float kickBack = 0.06f;
        [SerializeField] float kickUp = 2.5f;
        [SerializeField] float recover = 12f;

        [Header("Sway")]
        [SerializeField] float swayAmount = 0.015f;
        [SerializeField] float swaySmooth = 6f;

        [Header("Muzzle flash")]
        [SerializeField] Light flash;
        [SerializeField] float flashSeconds = 0.05f;

        Vector3 restPos;
        Quaternion restRot;
        float kick, flashUntil;

        public Transform Muzzle => muzzle != null ? muzzle : transform;

        void Awake()
        {
            if (model == null) model = transform;
            restPos = model.localPosition;
            restRot = model.localRotation;
            if (flash != null) flash.enabled = false;
        }

        void LateUpdate()
        {
            kick = Mathf.Lerp(kick, 0f, recover * Time.deltaTime);

            var mouse = UnityEngine.InputSystem.Mouse.current;
            Vector2 look = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
            var sway = new Vector3(-look.x, -look.y, 0f) * swayAmount * 0.02f;

            model.localPosition = Vector3.Lerp(model.localPosition,
                restPos + sway + Vector3.forward * -kick * kickBack, swaySmooth * Time.deltaTime);
            model.localRotation = Quaternion.Slerp(model.localRotation,
                restRot * Quaternion.Euler(-kick * kickUp, 0f, 0f), swaySmooth * Time.deltaTime);

            if (flash != null && flash.enabled && Time.time >= flashUntil) flash.enabled = false;
        }

        /// <summary>Called by the gun on every round that actually leaves the barrel.</summary>
        public void Fired()
        {
            kick = 1f;
            if (flash == null) return;
            flash.enabled = true;
            flashUntil = Time.time + flashSeconds;
        }
    }
}
