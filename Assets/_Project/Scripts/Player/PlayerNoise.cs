using UnityEngine;
using MonsterChase.Core;
using MonsterChase.Hiding;

namespace MonsterChase.Player
{
    /// <summary>
    /// How loud you are is how you get caught. Sprinting carries across the ward;
    /// walking carries a little; crouching is silent. This is the lever the player
    /// actually controls, and it is what makes the crouch speed penalty worth paying.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerNoise : MonoBehaviour
    {
        [SerializeField] FirstPersonController controller;

        [Header("How far each gait carries, in metres")]
        [SerializeField] float sprintRadius = 26f;
        [SerializeField] float walkRadius = 9f;
        [SerializeField] float crouchRadius = 0f;

        [Header("Seconds between footfalls")]
        [SerializeField] float sprintInterval = 0.34f;
        [SerializeField] float walkInterval = 0.55f;

        CharacterController cc;
        float nextStep;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (controller == null) controller = GetComponent<FirstPersonController>();
        }

        void Update()
        {
            if (HidingSpot.PlayerHidden || !cc.enabled || !cc.isGrounded) return;

            var flat = cc.velocity; flat.y = 0f;
            float speed = flat.magnitude;
            if (speed < 0.25f) return;

            bool crouching = controller != null && controller.IsCrouching;
            bool sprinting = !crouching && speed > 4.5f;

            float radius = crouching ? crouchRadius : sprinting ? sprintRadius : walkRadius;
            if (radius <= 0.01f) return;

            nextStep -= Time.deltaTime;
            if (nextStep > 0f) return;
            nextStep = sprinting ? sprintInterval : walkInterval;

            GameEvents.RaiseNoise(transform.position, radius);
        }
    }
}
