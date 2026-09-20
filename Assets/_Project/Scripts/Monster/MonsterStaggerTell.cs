using UnityEngine;

namespace MonsterChase.Monster
{
    /// <summary>
    /// Placeholder feedback: the capsule goes dark and drops while staggered, so the
    /// state is visible without animation. Replaced by a real ragdoll later.
    /// </summary>
    [RequireComponent(typeof(MonsterVitals))]
    public class MonsterStaggerTell : MonoBehaviour
    {
        MonsterVitals vitals;
        Renderer body;
        Vector3 upright;
        Color alive, down;

        void Awake()
        {
            vitals = GetComponent<MonsterVitals>();
            body = GetComponentInChildren<Renderer>();
            upright = transform.eulerAngles;
            if (body != null)
            {
                alive = body.sharedMaterial != null ? body.sharedMaterial.color : Color.red;
                down = alive * 0.35f;
            }
        }

        void Update()
        {
            bool isDown = vitals.Current != MonsterVitals.State.Hunting;

            var wantedRot = isDown ? new Vector3(upright.x + 78f, upright.y, upright.z) : upright;
            transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, wantedRot, 9f * Time.deltaTime);

            if (body != null)
                body.material.color = Color.Lerp(body.material.color, isDown ? down : alive, 7f * Time.deltaTime);
        }
    }
}
