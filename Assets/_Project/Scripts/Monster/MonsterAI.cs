using UnityEngine;
using UnityEngine.AI;
using MonsterChase.Core;
using MonsterChase.Hiding;
using MonsterChase.Ritual;
using MonsterChase;

namespace MonsterChase.Monster
{
    /// <summary>
    /// The hunt. Patrols the racetrack, sees down corridors, hears you run, chases when
    /// it has you, and searches outward from where it lost you.
    ///
    /// It is faster than your sprint on purpose. Running away in a straight line always
    /// loses, because the corridor is a loop and it will simply run you down. The way
    /// out is to break line of sight and get under a bed, which is why the beds exist.
    ///
    /// Burning anchors makes it *worse*: every body you burn adds speed and search
    /// patience. You are trading the ability to kill it against how hard it hunts,
    /// which is what stops the ritual from being a difficulty slider pointing one way.
    /// </summary>
    [RequireComponent(typeof(MonsterVitals))]
    public class MonsterAI : MonoBehaviour
    {
        public enum State { Patrol, Investigate, Chase, Search }

        [SerializeField] Transform player;
        [SerializeField] PatrolRoute route;

        [Header("Senses")]
        [Tooltip("It can see the length of the village. Cover is what saves you, not distance.")]
        [SerializeField] float sightRange = 85f;
        [SerializeField, Range(0f, 180f)] float sightHalfAngle = 85f;
        [SerializeField] LayerMask sightBlockers = ~0;
        [Tooltip("Unbroken seconds of sight before it commits.")]
        [SerializeField] float sightToChase = 0.3f;
        [SerializeField] float loseInterestSeconds = 4f;

        [Header("Speeds")]
        [Tooltip("Unhurried, so you can hear it coming before you see it.")]
        [SerializeField] float patrolSpeed = 2.6f;
        [SerializeField] float investigateSpeed = 4.2f;
        [Tooltip("Your sprint is 5.6. Well above it: running is not an escape, breaking line of sight is.")]
        [SerializeField] float chaseSpeed = 7.2f;

        [Header("Searching")]
        [SerializeField] float searchSeconds = 22f;
        [SerializeField] float searchRadius = 6f;

        [Header("Encounter")]
        [Tooltip("How far away laying eyes on it still counts as having met it.")]
        [SerializeField] float playerSightRange = 70f;

        [Header("Catching")]
        [SerializeField] float catchDistance = 1.7f;

        [Header("What each burnt anchor does to it")]
        [SerializeField] float chaseSpeedPerAnchor = 0.25f;
        [SerializeField] float searchSecondsPerAnchor = 3f;

        NavMeshAgent agent;
        MonsterVitals vitals;
        bool sweptPast;
        State state = State.Patrol;
        float stateTimer, sightTimer, lostTimer;
        Vector3 lastKnown;
        bool hasLastKnown;

        public State Current => state;

        int Burned => RitualState.Instance != null ? RitualState.Instance.Burned : 0;
        float CurrentChaseSpeed => chaseSpeed + chaseSpeedPerAnchor * Burned;
        float CurrentSearchSeconds => searchSeconds + searchSecondsPerAnchor * Burned;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            vitals = GetComponent<MonsterVitals>();

            if (player == null)
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null) player = tagged.transform;
            }
            if (route == null) route = Object.FindFirstObjectByType<PatrolRoute>();
        }

        void OnEnable() => GameEvents.NoiseMade += OnNoise;
        void OnDisable() => GameEvents.NoiseMade -= OnNoise;

        void Start()
        {
            if (route != null && route.HasPoints && agent.isOnNavMesh)
                agent.SetDestination(route.NearestTo(transform.position));
        }

        void Update()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            // Down is down. A staggered monster does not hunt, and a dead one never
            // hunts again -- this is what the whole gun/anchor arc is buying you.
            if (vitals.Current != MonsterVitals.State.Hunting)
            {
                agent.isStopped = true;
                return;
            }
            agent.isStopped = false;

            bool canSee = CanSeePlayer();
            sightTimer = canSee ? sightTimer + Time.deltaTime : 0f;

            // The encounter counts either way round. It noticing you is one way; you
            // laying eyes on it is the other, and it is the one the player actually
            // experiences as "I have seen what is out here". Waiting only on its own
            // sight meant the gate could stay shut for the whole run.
            if (canSee) Ritual.AnchorSite.MarkMonsterSeen();
            else TickPlayerSighting();

            if (TryCatch()) return;

            switch (state)
            {
                case State.Patrol:      TickPatrol(canSee); break;
                case State.Investigate: TickInvestigate(canSee); break;
                case State.Chase:       TickChase(canSee); break;
                case State.Search:      TickSearch(canSee); break;
            }
        }

        // ------------------------------------------------------------- senses

        bool CanSeePlayer()
        {
            if (player == null) return false;

            // Under a bed you are not visible, full stop. Being found while hidden is
            // the hiding spot's business, not a line-of-sight test that reaches through
            // a mattress.
            if (HidingSpot.PlayerHidden) return false;

            var eye = transform.position + Vector3.up * 1.4f;
            var target = player.position + Vector3.up * 1.2f;
            var to = target - eye;

            if (to.magnitude > sightRange) return false;
            if (Vector3.Angle(transform.forward, to) > sightHalfAngle) return false;

            // A corridor loop means most of the map is behind a wall most of the time.
            return !Physics.Linecast(eye, target, out _, sightBlockers, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Have you looked at it? On screen, close enough, and not behind a wall.</summary>
        void TickPlayerSighting()
        {
            if (Ritual.AnchorSite.MonsterSeen) return;

            var cam = Camera.main;
            if (cam == null) return;

            var chest = transform.position + Vector3.up * 1.6f;
            if (Vector3.Distance(cam.transform.position, chest) > playerSightRange) return;

            var viewport = cam.WorldToViewportPoint(chest);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f
                || viewport.y < 0f || viewport.y > 1f) return;

            if (Physics.Linecast(cam.transform.position, chest, out _, sightBlockers,
                                 QueryTriggerInteraction.Ignore)) return;

            Ritual.AnchorSite.MarkMonsterSeen();
        }

        void OnNoise(Vector3 at, float radius)
        {
            if (state == State.Chase) return;
            if ((at - transform.position).sqrMagnitude > radius * radius) return;
            EnterInvestigate(at);
        }

        /// <summary>
        /// Walks through where the noise came from and keeps going a little past it,
        /// rather than stopping dead on the spot. A shot pulls it across your position,
        /// which is the point of the noise mattering.
        /// </summary>
        void SweepPast()
        {
            if (!hasLastKnown) return;

            var through = lastKnown + (lastKnown - transform.position).normalized * 6f;
            if (NavMesh.SamplePosition(through, out var hit, 8f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
            sweptPast = true;
        }

        bool TryCatch()
        {
            if (player == null || HidingSpot.PlayerHidden) return false;
            if ((player.position - transform.position).sqrMagnitude > catchDistance * catchDistance) return false;

            GameEvents.RaisePlayerCaught();
            return true;
        }

        // ------------------------------------------------------------- states

        void TickPatrol(bool canSee)
        {
            agent.speed = patrolSpeed;
            if (canSee && sightTimer >= sightToChase) { EnterChase(); return; }

            if (!agent.pathPending && agent.remainingDistance < 1.2f && route != null && route.HasPoints)
                agent.SetDestination(route.Next());
        }

        void TickInvestigate(bool canSee)
        {
            agent.speed = investigateSpeed;
            if (canSee && sightTimer >= sightToChase) { EnterChase(); return; }

            stateTimer -= Time.deltaTime;

            bool arrived = !agent.pathPending && agent.remainingDistance < 1f;

            // Arriving is not the end of it: walk through and a few metres past first.
            if (arrived && !sweptPast) { SweepPast(); return; }

            if (stateTimer <= 0f || (arrived && sweptPast)) EnterSearch(agent.destination);
        }

        void TickChase(bool canSee)
        {
            agent.speed = CurrentChaseSpeed;

            if (canSee)
            {
                lostTimer = 0f;
                lastKnown = player.position;
                hasLastKnown = true;
                agent.SetDestination(lastKnown);
                return;
            }

            // It keeps coming for a few seconds after losing you, so ducking behind one
            // corner is not enough -- you have to actually get somewhere.
            lostTimer += Time.deltaTime;
            if (hasLastKnown) agent.SetDestination(lastKnown);
            if (lostTimer >= loseInterestSeconds) EnterSearch(hasLastKnown ? lastKnown : transform.position);
        }

        void TickSearch(bool canSee)
        {
            agent.speed = investigateSpeed;
            if (canSee && sightTimer >= sightToChase) { EnterChase(); return; }

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f) { EnterPatrol(); return; }

            if (agent.pathPending || agent.remainingDistance > 1.2f) return;

            // Widen the circle as it gives up hope, so it sweeps rooms near where you
            // vanished before drifting back to the corridor.
            float t = 1f - Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, CurrentSearchSeconds));
            var offset = Random.insideUnitCircle * searchRadius * (0.4f + t);
            var probe = lastKnown + new Vector3(offset.x, 0f, offset.y);

            if (NavMesh.SamplePosition(probe, out var hit, 4f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        public void EnterPatrol()
        {
            state = State.Patrol;
            hasLastKnown = false;
            if (route != null && route.HasPoints) agent.SetDestination(route.NearestTo(transform.position));
        }

        public void EnterInvestigate(Vector3 point)
        {
            if (state == State.Chase) return;

            lastKnown = point;
            hasLastKnown = true;
            sweptPast = false;

            if (NavMesh.SamplePosition(point, out var hit, 5f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);

            state = State.Investigate;
            stateTimer = 14f;
        }

        void EnterChase()
        {
            if (state == State.Chase) return;
            state = State.Chase;
            lostTimer = 0f;
            if (player != null) { lastKnown = player.position; hasLastKnown = true; }
        }

        void EnterSearch(Vector3 around)
        {
            lastKnown = around;
            hasLastKnown = true;
            state = State.Search;
            stateTimer = CurrentSearchSeconds;
        }
    }
}
