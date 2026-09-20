using UnityEngine;

namespace MonsterChase.Player
{
    /// <summary>
    /// Spawns a one-shot effect at a hit and cleans it up. Effects come from prefabs
    /// assigned in the scene rather than Resources.Load, so a missing pack degrades to
    /// nothing visible instead of a null reference every time you pull the trigger.
    /// </summary>
    public class Impacts : MonoBehaviour
    {
        public static Impacts Instance { get; private set; }

        [SerializeField] GameObject bloodHit;
        [SerializeField] GameObject bloodPool;
        [SerializeField] float lifetime = 4f;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>A round landing in something that bleeds.</summary>
        public void Blood(Vector3 point, Vector3 normal)
        {
            if (bloodHit == null) return;
            var go = Instantiate(bloodHit, point, Quaternion.LookRotation(normal));
            Destroy(go, lifetime);
        }

        /// <summary>A pool that stays, for dressing the map.</summary>
        public GameObject Pool(Vector3 point, Transform parent = null)
        {
            if (bloodPool == null) return null;
            var go = Instantiate(bloodPool, point, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
            return go;
        }
    }
}
