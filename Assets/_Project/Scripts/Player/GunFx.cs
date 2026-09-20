using UnityEngine;

namespace MonsterChase.Player
{
    /// <summary>
    /// What a shot looks like: flash at the muzzle, a tracer down the line of the
    /// bullet, brass out of the ejection port, and a spark where it lands.
    ///
    /// Built from plain particle systems rather than a pack, so it cannot break when
    /// a pack is reimported and it costs nothing to carry.
    /// </summary>
    public class GunFx : MonoBehaviour
    {
        [SerializeField] ParticleSystem flash;
        [SerializeField] ParticleSystem smoke;
        [SerializeField] ParticleSystem brass;
        [SerializeField] ParticleSystem tracer;
        [SerializeField] ParticleSystem impact;
        [SerializeField] Transform muzzle;

        /// <summary>Fired with the hit point, or the far end of the ray when it missed.</summary>
        public void Shot(Vector3 from, Vector3 to, bool hitSomething, Vector3 normal)
        {
            if (flash != null) flash.Play(true);
            if (smoke != null) smoke.Play(true);
            if (brass != null) brass.Play(true);

            if (tracer != null)
            {
                // Point the tracer down the actual line of the shot and scale its speed
                // so it covers the distance in roughly one lifetime.
                var dir = to - from;
                float distance = dir.magnitude;
                if (distance > 0.01f)
                {
                    tracer.transform.position = from;
                    tracer.transform.rotation = Quaternion.LookRotation(dir);
                    var main = tracer.main;
                    main.startSpeed = Mathf.Clamp(distance / Mathf.Max(0.01f, main.startLifetime.constant), 40f, 400f);
                    tracer.Play(true);
                }
            }

            if (hitSomething && impact != null)
            {
                impact.transform.position = to;
                impact.transform.rotation = Quaternion.LookRotation(normal);
                impact.Play(true);
            }
        }

        public Transform Muzzle => muzzle != null ? muzzle : transform;
    }
}
