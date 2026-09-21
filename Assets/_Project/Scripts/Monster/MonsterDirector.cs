using UnityEngine;
using UnityEngine.AI;

namespace MonsterChase.Monster
{
    /// <summary>
    /// The second mind. The creature itself is local and clueless: it only knows what
    /// it can see and hear. This one sits above the map, always knows exactly where you
    /// are, and never tells the creature precisely -- only roughly, and only once you
    /// have been lost for a while.
    ///
    /// The point is that hiding has to stay tense without becoming safe. A creature
    /// that only uses its senses eventually wanders off and the run goes quiet; one
    /// that always knows is unfair and unfun. A director that says "somewhere over
    /// there, about now" keeps the pressure on while leaving you a real chance.
    ///
    /// The hint gets less vague the longer you stay lost, so hiding in one place
    /// forever stops working without anything ever teleporting to you.
    /// </summary>
    [RequireComponent(typeof(MonsterAI))]
    public class MonsterDirector : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] MonsterAI creature;

        [Header("Patience")]
        [Tooltip("Seconds out of contact before the director says anything at all.")]
        [SerializeField] float quietSeconds = 25f;
        [Tooltip("Seconds between hints once it has started.")]
        [SerializeField] float hintInterval = 18f;

        [Header("How rough the hint is")]
        [Tooltip("Metres of error on the first hint. It is a direction, not an address.")]
        [SerializeField] float startingError = 35f;
        [Tooltip("Metres of error once you have been lost a long time.")]
        [SerializeField] float minimumError = 9f;
        [Tooltip("Seconds of being lost over which the error shrinks from starting to minimum.")]
        [SerializeField] float tighteningSeconds = 120f;

        /// <summary>Seconds since the creature last had the player itself.</summary>
        public float SecondsLost { get; private set; }
        public int HintsGiven { get; private set; }
        public Vector3 LastHint { get; private set; }

        float nextHintAt;

        void Awake()
        {
            if (creature == null) creature = GetComponent<MonsterAI>();
            if (player == null)
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null) player = tagged.transform;
            }
        }

        void Update()
        {
            if (player == null || creature == null) return;

            // Chasing means it has you itself. The director has nothing to add, and
            // its clock resets -- losing you starts the countdown again from zero.
            if (creature.Current == MonsterAI.State.Chase)
            {
                SecondsLost = 0f;
                nextHintAt = quietSeconds;
                return;
            }

            SecondsLost += Time.deltaTime;
            if (SecondsLost < nextHintAt) return;

            nextHintAt = SecondsLost + hintInterval;
            Hint();
        }

        void Hint()
        {
            // Error shrinks the longer you stay lost, so a perfect hiding place slowly
            // stops being one.
            float t = Mathf.Clamp01(SecondsLost / Mathf.Max(0.01f, tighteningSeconds));
            float error = Mathf.Lerp(startingError, minimumError, t);

            var offset = Random.insideUnitCircle.normalized * Random.Range(error * 0.5f, error);
            var guess = player.position + new Vector3(offset.x, 0f, offset.y);

            if (!NavMesh.SamplePosition(guess, out var hit, error, NavMesh.AllAreas)) return;

            LastHint = hit.position;
            HintsGiven++;

            // Told, not teleported: it walks there and searches like it would after a
            // noise. It may well arrive at nothing.
            creature.EnterInvestigate(LastHint);
        }
    }
}
