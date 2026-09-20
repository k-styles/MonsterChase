using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine.AI;
using MonsterChase.Hiding;
using MonsterChase.Monster;
using MonsterChase.Player;
using MonsterChase.Ritual;
using MonsterChase.Interaction;
using MonsterChase.UI;

namespace MonsterChase.EditorTools
{
    /// <summary>
    /// A hospital inpatient unit built as a racetrack, which is how real nursing units
    /// are laid out: patient rooms line the outside wall, support rooms fill a solid
    /// core in the middle, and the corridor loops all the way round between them. The
    /// shape exists in real hospitals to minimise how far nurses walk.
    ///
    /// It also happens to be an excellent chase map. A continuous loop has no dead
    /// ends, the core blocks line of sight from one side to the other, and every room
    /// opens onto the corridor from one side only.
    ///
    /// Dimensions are the real ones: 2.4m main corridors, patient rooms over 12m2,
    /// 1.3m doorways for a stretcher, 3m ceiling.
    /// </summary>
    public static class BuildHospital
    {
        const string ScenePath = "Assets/_Project/Scenes/Hospital.unity";

        // Footprint
        const float HalfW = 22.5f;      // 45m across
        const float HalfD = 16.5f;      // 33m deep
        const float Band = 4.5f;        // patient room depth
        const float Corridor = 2.4f;    // code minimum for a stretcher route

        static float CoreHalfW => HalfW - Band - Corridor;   // 15.6
        static float CoreHalfD => HalfD - Band - Corridor;   // 9.6

        static Material floorMat, wallMat, coreMat, bedMat, sheetMat, trimMat, bodyMat, glassMat;

        [MenuItem("MonsterChase/Build Hospital Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");

            floorMat = HospitalKit.Mat("Floor",  new Color(0.36f, 0.37f, 0.36f));
            wallMat  = HospitalKit.Mat("Wall",   new Color(0.62f, 0.63f, 0.60f));
            coreMat  = HospitalKit.Mat("Core",   new Color(0.50f, 0.52f, 0.52f));
            bedMat   = HospitalKit.Mat("Bed",    new Color(0.78f, 0.79f, 0.80f), 0.35f);
            sheetMat = HospitalKit.Mat("Sheet",  new Color(0.86f, 0.87f, 0.85f));
            trimMat  = HospitalKit.Mat("Trim",   new Color(0.30f, 0.42f, 0.44f));
            bodyMat  = HospitalKit.Mat("Body",   new Color(0.44f, 0.22f, 0.22f));
            glassMat = HospitalKit.Mat("Glass",  new Color(0.55f, 0.66f, 0.68f), 0.8f);

            var root = new GameObject("Hospital").transform;

            BuildShell(root);
            var rooms = BuildPatientRooms(root);
            BuildCore(root);
            BuildCorners(root);
            BuildLighting(root);

            var ritual = new GameObject("RitualState");
            ritual.AddComponent<RitualState>();

            var player = BuildPlayer(new Vector3(0f, 0.2f, -(HalfD - Band - Corridor * 0.5f)));
            PlaceAnchors(root, rooms);

            // Bake before the agent exists: an agent dropped onto a scene with no
            // NavMesh reports isOnNavMesh false forever and simply never moves.
            BuildNavMesh(root);

            var patrol = BuildPatrolRing(root);
            var monster = BuildMonster(new Vector3(0f, 0f, HalfD - Band - Corridor * 0.5f),
                                       player.transform, patrol);
            var hud = BuildHud(monster);
            BuildPauseMenu();
            WirePlayerLife(player, hud);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScene();

            Debug.Log($"[Hospital] Built {ScenePath}. Racetrack ward 45x33m, {rooms.Count} patient rooms, " +
                      $"{rooms.Count * 2} beds you can hide under, 2.4m corridors, 3m ceiling. " +
                      $"It hunts: patrols the loop, sees {30}m down a corridor, hears you sprint from 26m, " +
                      $"and runs at 5.9 against your 5.6.");
        }

        // ---------------------------------------------------------------- shell

        static void BuildShell(Transform root)
        {
            HospitalKit.Box(root, "Floor", new Vector3(0f, -0.05f, 0f),
                new Vector3(HalfW * 2f, 0.1f, HalfD * 2f), floorMat);

            HospitalKit.Box(root, "Ceiling", new Vector3(0f, HospitalKit.WallHeight + 0.05f, 0f),
                new Vector3(HalfW * 2f, 0.1f, HalfD * 2f), coreMat);

            // Outer envelope
            HospitalKit.Wall(root, "Wall_N", new Vector2(-HalfW,  HalfD), new Vector2( HalfW,  HalfD), wallMat);
            HospitalKit.Wall(root, "Wall_S", new Vector2(-HalfW, -HalfD), new Vector2( HalfW, -HalfD), wallMat);
            HospitalKit.Wall(root, "Wall_W", new Vector2(-HalfW, -HalfD), new Vector2(-HalfW,  HalfD), wallMat);
            HospitalKit.Wall(root, "Wall_E", new Vector2( HalfW, -HalfD), new Vector2( HalfW,  HalfD), wallMat);
        }

        // -------------------------------------------------------- patient rooms

        class Room
        {
            public string Name;
            public Vector3 Centre;
            public Quaternion Facing;   // rotation so +Z points from the room into the corridor
            public float Width, Depth;
        }

        /// <summary>
        /// Rooms line all four sides. Each is divided from its neighbour by a partition
        /// and opens onto the corridor through a single 1.3m doorway.
        /// </summary>
        static List<Room> BuildPatientRooms(Transform root)
        {
            var rooms = new List<Room>();
            var holder = new GameObject("PatientRooms").transform;
            holder.SetParent(root, false);

            // North and south bands run the width of the core.
            BuildBand(holder, rooms, horizontal: true,  positive: true,  count: 6);
            BuildBand(holder, rooms, horizontal: true,  positive: false, count: 6);
            BuildBand(holder, rooms, horizontal: false, positive: true,  count: 4);
            BuildBand(holder, rooms, horizontal: false, positive: false, count: 4);

            foreach (var room in rooms) FurnishRoom(holder, room);
            return rooms;
        }

        static void BuildBand(Transform holder, List<Room> rooms, bool horizontal, bool positive, int count)
        {
            float span = horizontal ? CoreHalfW * 2f : CoreHalfD * 2f;
            float each = span / count;
            float outerEdge = horizontal ? HalfD : HalfW;
            float innerEdge = outerEdge - Band;
            float sign = positive ? 1f : -1f;

            string side = horizontal ? (positive ? "N" : "S") : (positive ? "E" : "W");

            for (int i = 0; i < count; i++)
            {
                float t = -span * 0.5f + each * (i + 0.5f);

                Vector3 centre = horizontal
                    ? new Vector3(t, 0f, sign * (innerEdge + Band * 0.5f))
                    : new Vector3(sign * (innerEdge + Band * 0.5f), 0f, t);

                var room = new Room
                {
                    Name = $"Room_{side}{i + 1:D2}",
                    Centre = centre,
                    Width = each,
                    Depth = Band,
                    Facing = horizontal
                        ? Quaternion.Euler(0f, positive ? 180f : 0f, 0f)
                        : Quaternion.Euler(0f, positive ? 270f : 90f, 0f)
                };
                rooms.Add(room);

                // Partition between this room and the next one along.
                if (i < count - 1)
                {
                    float edge = -span * 0.5f + each * (i + 1);
                    if (horizontal)
                        HospitalKit.Wall(holder, $"Part_{side}{i}",
                            new Vector2(edge, sign * innerEdge), new Vector2(edge, sign * outerEdge), wallMat);
                    else
                        HospitalKit.Wall(holder, $"Part_{side}{i}",
                            new Vector2(sign * innerEdge, edge), new Vector2(sign * outerEdge, edge), wallMat);
                }

                // Corridor-facing wall, with the doorway in it.
                if (horizontal)
                    HospitalKit.Wall(holder, $"Face_{side}{i}",
                        new Vector2(t - each * 0.5f, sign * innerEdge),
                        new Vector2(t + each * 0.5f, sign * innerEdge), wallMat, doorAt: 0.5f);
                else
                    HospitalKit.Wall(holder, $"Face_{side}{i}",
                        new Vector2(sign * innerEdge, t - each * 0.5f),
                        new Vector2(sign * innerEdge, t + each * 0.5f), wallMat, doorAt: 0.5f);
            }
        }

        // ------------------------------------------------------------ furniture

        static void FurnishRoom(Transform holder, Room room)
        {
            var go = new GameObject(room.Name);
            go.transform.SetParent(holder, false);
            go.transform.position = room.Centre;
            go.transform.rotation = room.Facing;

            // Two beds per room, heads to the outside wall.
            float offset = Mathf.Min(room.Width, 5.2f) * 0.25f;
            MakeBed(go.transform, "Bed_A", new Vector3(-offset, 0f, -0.6f));
            MakeBed(go.transform, "Bed_B", new Vector3( offset, 0f, -0.6f));
        }

        /// <summary>
        /// A bed on a frame, with a hiding spot under it. The frame is raised enough to
        /// read as somewhere a person could get under without looking absurd.
        /// </summary>
        static void MakeBed(Transform parent, string name, Vector3 localPos)
        {
            var bed = new GameObject(name);
            bed.transform.SetParent(parent, false);
            bed.transform.localPosition = localPos;

            const float deck = 0.62f;     // top of the mattress base
            HospitalKit.Box(bed.transform, "Frame", new Vector3(0f, deck, 0f),
                new Vector3(0.95f, 0.10f, 2.05f), bedMat);
            HospitalKit.Box(bed.transform, "Mattress", new Vector3(0f, deck + 0.13f, 0f),
                new Vector3(0.90f, 0.16f, 1.95f), sheetMat);
            HospitalKit.Box(bed.transform, "HeadBoard", new Vector3(0f, deck + 0.30f, -1.05f),
                new Vector3(0.95f, 0.55f, 0.08f), trimMat);
            HospitalKit.Box(bed.transform, "FootBoard", new Vector3(0f, deck + 0.20f, 1.05f),
                new Vector3(0.95f, 0.35f, 0.08f), trimMat);

            foreach (var x in new[] { -0.4f, 0.4f })
            foreach (var z in new[] { -0.9f, 0.9f })
                HospitalKit.Box(bed.transform, "Leg", new Vector3(x, deck * 0.5f, z),
                    new Vector3(0.06f, deck, 0.06f), trimMat, collider: false);

            // Bedside cabinet, and something to break the silhouette.
            HospitalKit.Box(bed.transform, "Cabinet", new Vector3(0.75f, 0.32f, -0.7f),
                new Vector3(0.45f, 0.64f, 0.45f), bedMat);

            // The hiding spot. Its collider is the thing the interact ray hits, so it
            // is a trigger sized to the gap under the frame rather than the bed itself.
            var spotGo = new GameObject("HideUnderBed");
            spotGo.transform.SetParent(bed.transform, false);
            spotGo.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            var trigger = spotGo.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(0.9f, 0.5f, 1.9f);

            var hideAnchor = new GameObject("HideAnchor");
            hideAnchor.transform.SetParent(spotGo.transform, false);
            hideAnchor.transform.localPosition = new Vector3(0f, -0.1f, 0.2f);

            var exitAnchor = new GameObject("ExitAnchor");
            exitAnchor.transform.SetParent(bed.transform, false);
            exitAnchor.transform.localPosition = new Vector3(-0.95f, 0.1f, 0f);

            var spot = spotGo.AddComponent<HidingSpot>();
            var so = new SerializedObject(spot);
            so.FindProperty("hideAnchor").objectReferenceValue = hideAnchor.transform;
            so.FindProperty("exitAnchor").objectReferenceValue = exitAnchor.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ----------------------------------------------------------------- core

        /// <summary>
        /// The solid middle: nurse station plus the support rooms that in a real unit
        /// are what the racetrack is wrapped around. For us it is a sight-line blocker
        /// with doors, which is where most of the hiding happens.
        /// </summary>
        static void BuildCore(Transform root)
        {
            var holder = new GameObject("Core").transform;
            holder.SetParent(root, false);

            // Core envelope, with a door on each face so it can be cut through.
            HospitalKit.Wall(holder, "Core_N", new Vector2(-CoreHalfW,  CoreHalfD), new Vector2( CoreHalfW,  CoreHalfD), coreMat, 0.5f);
            HospitalKit.Wall(holder, "Core_S", new Vector2(-CoreHalfW, -CoreHalfD), new Vector2( CoreHalfW, -CoreHalfD), coreMat, 0.5f);
            HospitalKit.Wall(holder, "Core_W", new Vector2(-CoreHalfW, -CoreHalfD), new Vector2(-CoreHalfW,  CoreHalfD), coreMat, 0.5f);
            HospitalKit.Wall(holder, "Core_E", new Vector2( CoreHalfW, -CoreHalfD), new Vector2( CoreHalfW,  CoreHalfD), coreMat, 0.5f);

            // Split the core into a nurse station in the middle and support rooms
            // either side, so cutting through it is not a straight run.
            HospitalKit.Wall(holder, "Core_Div_W", new Vector2(-5.2f, -CoreHalfD), new Vector2(-5.2f, CoreHalfD), coreMat, 0.3f);
            HospitalKit.Wall(holder, "Core_Div_E", new Vector2( 5.2f, -CoreHalfD), new Vector2( 5.2f, CoreHalfD), coreMat, 0.7f);

            // Nurse station desk: an island, so you can be seen over it but not through.
            HospitalKit.Box(holder, "NurseDesk", new Vector3(0f, 0.55f, 0f), new Vector3(6.5f, 1.1f, 2.2f), trimMat);
            HospitalKit.Box(holder, "DeskScreen", new Vector3(0f, 1.3f, -0.9f), new Vector3(6.5f, 0.4f, 0.08f), glassMat);

            // Supply shelving in the side rooms: cover you can break line of sight behind.
            foreach (var x in new[] { -10.5f, 10.5f })
            for (int i = 0; i < 3; i++)
            {
                float z = -5f + i * 5f;
                HospitalKit.Box(holder, $"Shelf_{x:0}_{i}", new Vector3(x, 1.0f, z),
                    new Vector3(2.6f, 2.0f, 0.55f), coreMat);
            }
        }

        static void BuildCorners(Transform root)
        {
            var holder = new GameObject("Corners").transform;
            holder.SetParent(root, false);

            // The four corner blocks the racetrack turns around. Solid, so the loop
            // reads as a loop rather than a square room.
            foreach (var sx in new[] { -1f, 1f })
            foreach (var sz in new[] { -1f, 1f })
            {
                float cx = sx * (CoreHalfW + Corridor + Band * 0.5f);
                float cz = sz * (CoreHalfD + Corridor + Band * 0.5f);
                HospitalKit.Box(holder, $"CornerBlock_{sx:0}_{sz:0}",
                    new Vector3(cx, HospitalKit.WallHeight * 0.5f, cz),
                    new Vector3(Band, HospitalKit.WallHeight, Band), coreMat);
            }
        }

        static void BuildLighting(Transform root)
        {
            var holder = new GameObject("Lights").transform;
            holder.SetParent(root, false);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.11f, 0.13f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.022f;
            RenderSettings.fogColor = new Color(0.03f, 0.035f, 0.04f);

            // Strip lights down the middle of the corridor loop.
            float ringX = CoreHalfW + Corridor * 0.5f;
            float ringZ = CoreHalfD + Corridor * 0.5f;

            for (float x = -ringX; x <= ringX; x += 5.2f)
            {
                Lamp(holder, new Vector3(x, 2.8f,  ringZ));
                Lamp(holder, new Vector3(x, 2.8f, -ringZ));
            }
            for (float z = -ringZ + 4f; z <= ringZ - 4f; z += 4.8f)
            {
                Lamp(holder, new Vector3( ringX, 2.8f, z));
                Lamp(holder, new Vector3(-ringX, 2.8f, z));
            }
        }

        static void Lamp(Transform parent, Vector3 pos)
        {
            var go = new GameObject("Strip");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.82f, 0.86f, 0.92f);
            l.intensity = 1.6f;
            l.range = 8.5f;
            l.shadows = LightShadows.Soft;
        }

        // ------------------------------------------------------------ cast

        static GameObject BuildPlayer(Vector3 position)
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = position;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.9f, 0f);

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.65f, 0f);

            var camGo = new GameObject("MainCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.02f; cam.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();

            var controller = player.AddComponent<FirstPersonController>();
            var cso = new SerializedObject(controller);
            cso.FindProperty("cameraPivot").objectReferenceValue = pivot.transform;
            cso.FindProperty("playerCamera").objectReferenceValue = cam;
            cso.ApplyModifiedPropertiesWithoutUndo();

            var interactor = player.AddComponent<Interactor>();
            var iso = new SerializedObject(interactor);
            iso.FindProperty("sourceCamera").objectReferenceValue = cam;
            iso.ApplyModifiedPropertiesWithoutUndo();

            var audio = player.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.spatialBlend = 0f;

            var gun = player.AddComponent<Gun>();
            var gso = new SerializedObject(gun);
            gso.FindProperty("sourceCamera").objectReferenceValue = cam;
            gso.FindProperty("fireAudio").objectReferenceValue = audio;
            gso.FindProperty("interactor").objectReferenceValue = interactor;
            gso.ApplyModifiedPropertiesWithoutUndo();

            return player;
        }

        static MonsterVitals BuildMonster(Vector3 position, Transform player, PatrolRoute route)
        {
            var go = new GameObject("Monster");
            go.transform.position = position;

            // Tall enough to fill a 3m corridor and read as wrong at a distance,
            // short enough to clear the doorways it has to come through.
            PresenceBuilder.BuildMonsterBody(go.transform, 2.45f);

            var vitals = go.AddComponent<MonsterVitals>();

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2.4f;
            agent.speed = 2f;
            agent.angularSpeed = 700f;
            agent.acceleration = 30f;
            agent.stoppingDistance = 0.3f;
            agent.autoBraking = false;

            var anim = go.AddComponent<MonsterAnimation>();
            var anso = new SerializedObject(anim);
            anso.FindProperty("animator").objectReferenceValue = go.GetComponentInChildren<Animator>();
            anso.FindProperty("agent").objectReferenceValue = agent;
            anso.ApplyModifiedPropertiesWithoutUndo();

            var ai = go.AddComponent<MonsterAI>();
            var aiso = new SerializedObject(ai);
            aiso.FindProperty("player").objectReferenceValue = player;
            aiso.FindProperty("route").objectReferenceValue = route;
            aiso.ApplyModifiedPropertiesWithoutUndo();

            return vitals;
        }

        /// <summary>
        /// Waypoints round the corridor loop, so patrolling follows the racetrack the
        /// way a night nurse would rather than cutting across the core.
        /// </summary>
        static PatrolRoute BuildPatrolRing(Transform root)
        {
            var go = new GameObject("PatrolRoute");
            go.transform.SetParent(root, false);
            var route = go.AddComponent<PatrolRoute>();

            float rx = CoreHalfW + Corridor * 0.5f;
            float rz = CoreHalfD + Corridor * 0.5f;

            var corners = new List<Vector3>();
            const int perSide = 4;
            for (int i = 0; i < perSide; i++) corners.Add(new Vector3(Mathf.Lerp(-rx,  rx, i / (float)perSide), 0f,  rz));
            for (int i = 0; i < perSide; i++) corners.Add(new Vector3( rx, 0f, Mathf.Lerp( rz, -rz, i / (float)perSide)));
            for (int i = 0; i < perSide; i++) corners.Add(new Vector3(Mathf.Lerp( rx, -rx, i / (float)perSide), 0f, -rz));
            for (int i = 0; i < perSide; i++) corners.Add(new Vector3(-rx, 0f, Mathf.Lerp(-rz,  rz, i / (float)perSide)));

            var transforms = new List<Object>();
            for (int i = 0; i < corners.Count; i++)
            {
                var p = corners[i];
                if (NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas)) p = hit.position;

                var wp = new GameObject($"WP_{i:D2}");
                wp.transform.SetParent(go.transform, false);
                wp.transform.position = p;
                transforms.Add(wp.transform);
            }

            var so = new SerializedObject(route);
            var array = so.FindProperty("points");
            array.arraySize = transforms.Count;
            for (int i = 0; i < transforms.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = transforms[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[Hospital] Patrol ring: {transforms.Count} waypoints round the loop.");
            return route;
        }

        /// <summary>Noise and death, both of which need the HUD canvas to exist.</summary>
        static void WirePlayerLife(GameObject player, VitalsReadout hud)
        {
            player.AddComponent<PlayerNoise>();

            var canvas = hud.transform;
            var blood = UiKit.NewImage("Blood", canvas, new Color(0.35f, 0.02f, 0.02f, 0f));
            UiKit.Stretch(blood.rectTransform);
            blood.transform.SetAsLastSibling();

            var died = UiKit.NewText("DeathText", canvas, 46, TextAnchor.MiddleCenter);
            died.enabled = false;
            UiKit.Place(died.rectTransform, 0.2f, 0.8f, 0.45f, 0.55f);
            died.transform.SetAsLastSibling();

            var life = player.AddComponent<PlayerLife>();
            var so = new SerializedObject(life);
            so.FindProperty("bloodOverlay").objectReferenceValue = blood;
            so.FindProperty("deathText").objectReferenceValue = died;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Five bodies, spread so no two are on the same stretch of corridor.</summary>
        static void PlaceAnchors(Transform root, List<Room> rooms)
        {
            var holder = new GameObject("Anchors").transform;
            holder.SetParent(root, false);

            if (rooms.Count == 0) return;
            int[] pick = { 1, 5, 9, 13, 17 };

            for (int i = 0; i < pick.Length; i++)
            {
                var room = rooms[pick[i] % rooms.Count];
                var pos = room.Centre + new Vector3(0f, 0.18f, 0f);

                var body = new GameObject($"Anchor_{i:D2}");
                body.transform.SetParent(holder, false);
                body.transform.position = pos;

                PresenceBuilder.BuildCorpse(body.transform, i);

                // The interact ray needs something to hit, and the corpse's own
                // colliders were stripped so they cannot bake into the navmesh.
                var col = body.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.center = new Vector3(0f, 0.25f, 0f);
                col.size = new Vector3(1.8f, 0.7f, 1.0f);

                var lightGo = new GameObject("FireLight");
                lightGo.transform.SetParent(body.transform, false);
                var l = lightGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.55f, 0.2f);
                l.intensity = 5f; l.range = 10f; l.enabled = false;

                var anchor = body.AddComponent<AnchorSite>();
                var aso = new SerializedObject(anchor);
                aso.FindProperty("fireLight").objectReferenceValue = l;
                aso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void BuildNavMesh(Transform root)
        {
            var go = new GameObject("NavMesh");
            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();

            if (surface.navMeshData == null)
            {
                Debug.LogWarning("[Hospital] NavMesh bake produced no data.");
                return;
            }
            const string path = "Assets/_Project/Scenes/Hospital_NavMesh.asset";
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            AssetDatabase.SaveAssets();
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path);
            EditorUtility.SetDirty(surface);
        }

        // -------------------------------------------------------------- ui

        static VitalsReadout BuildHud(MonsterVitals monster)
        {
            UiKit.NewCanvas("HUD", out var canvasGo);
            UiKit.EnsureEventSystem();

            var readout = UiKit.NewText("Readout", canvasGo.transform, 20, TextAnchor.UpperLeft);
            UiKit.Place(readout.rectTransform, 0.02f, 0.30f, 0.68f, 0.97f);

            var healthBack = UiKit.NewImage("MonsterHealthBack", canvasGo.transform, new Color(0f, 0f, 0f, 0.5f));
            UiKit.Place(healthBack.rectTransform, 0.35f, 0.65f, 0.90f, 0.93f);
            var healthFill = UiKit.NewImage("Fill", healthBack.transform, new Color(0.75f, 0.2f, 0.2f));
            UiKit.Stretch(healthFill.rectTransform);
            healthFill.type = UnityEngine.UI.Image.Type.Filled;
            healthFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

            var burnBack = UiKit.NewImage("BurnBack", canvasGo.transform, new Color(0f, 0f, 0f, 0.5f));
            UiKit.Place(burnBack.rectTransform, 0.40f, 0.60f, 0.16f, 0.19f);
            var burnFill = UiKit.NewImage("Fill", burnBack.transform, new Color(1f, 0.55f, 0.18f));
            UiKit.Stretch(burnFill.rectTransform);
            burnFill.type = UnityEngine.UI.Image.Type.Filled;
            burnFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

            var prompt = UiKit.NewText("Prompt", canvasGo.transform, 24, TextAnchor.LowerCenter);
            UiKit.Place(prompt.rectTransform, 0.3f, 0.7f, 0.20f, 0.24f);

            var crosshair = UiKit.NewImage("Crosshair", canvasGo.transform, new Color(1f, 1f, 1f, 0.5f));
            crosshair.rectTransform.anchorMin = crosshair.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            crosshair.rectTransform.sizeDelta = new Vector2(4f, 4f);

            var hud = canvasGo.AddComponent<VitalsReadout>();
            var so = new SerializedObject(hud);
            so.FindProperty("monster").objectReferenceValue = monster;
            so.FindProperty("readout").objectReferenceValue = readout;
            so.FindProperty("healthFill").objectReferenceValue = healthFill;
            so.FindProperty("burnFill").objectReferenceValue = burnFill;
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.ApplyModifiedPropertiesWithoutUndo();
            return hud;
        }

        static void BuildPauseMenu()
        {
            UiKit.NewCanvas("PauseCanvas", out var canvasGo);
            canvasGo.GetComponent<Canvas>().sortingOrder = 10;
            UiKit.EnsureEventSystem();

            var rows = UiKit.NewImage("PausePanel", canvasGo.transform, new Color(0.03f, 0.03f, 0.04f, 0.88f));
            rows.raycastTarget = true;
            UiKit.Stretch(rows.rectTransform);

            var heading = UiKit.NewText("Heading", rows.transform, 54, TextAnchor.MiddleLeft);
            heading.text = "PAUSED";
            UiKit.Place(heading.rectTransform, 0.14f, 0.7f, 0.66f, 0.76f);

            var resume = UiKit.NewMenuButton("Resume", rows.transform, "resume", 32, out _);
            UiKit.Place((RectTransform)resume.transform, 0.14f, 0.45f, 0.54f, 0.60f);
            var controls = UiKit.NewMenuButton("Controls", rows.transform, "controls", 32, out _);
            UiKit.Place((RectTransform)controls.transform, 0.14f, 0.45f, 0.47f, 0.53f);
            var toMenu = UiKit.NewMenuButton("ToMenu", rows.transform, "main menu", 32, out _);
            UiKit.Place((RectTransform)toMenu.transform, 0.14f, 0.45f, 0.40f, 0.46f);

            var settings = SettingsPanelBuilder.Build(canvasGo.transform);
            settings.transform.SetAsLastSibling();

            var pause = canvasGo.AddComponent<PauseMenu>();
            var so = new SerializedObject(pause);
            so.FindProperty("rootPanel").objectReferenceValue = rows.gameObject;
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("resumeButton").objectReferenceValue = resume;
            so.FindProperty("controlsButton").objectReferenceValue = controls;
            so.FindProperty("menuButton").objectReferenceValue = toMenu;
            so.FindProperty("menuScene").stringValue = "Menu";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void RegisterScene()
        {
            var wanted = new[]
            {
                "Assets/_Project/Scenes/Menu.unity",
                ScenePath,
                "Assets/_Project/Scenes/Testbed.unity",
            };
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in wanted)
                if (System.IO.File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
