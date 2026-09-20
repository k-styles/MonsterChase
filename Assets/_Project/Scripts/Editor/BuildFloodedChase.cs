using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using MonsterChase.Hiding;
using MonsterChase.Monster;
using MonsterChase.Ritual;
using MonsterChase.Systems;

namespace MonsterChase.EditorTools
{
    /// <summary>
    /// The chase, out in the flooded village.
    ///
    /// Built by opening the pack's own scene and saving it under our name, rather than
    /// rebuilding the environment: the art, the terrain and the water are theirs and
    /// there is no reason to reproduce any of it. Everything we add goes under a single
    /// "Chase" root so a rebuild can strip exactly what it added and leave the village
    /// untouched.
    ///
    /// The terrain is 1024m square, which is far too much map. The play area is a box
    /// around the village centre, and the navmesh is baked only inside it, so the
    /// monster cannot path off into a kilometre of empty field.
    /// </summary>
    public static class BuildFloodedChase
    {
        const string SourceScene = "Assets/Flooded_Grounds/Scenes/Scene_A.unity";
        const string ScenePath = "Assets/_Project/Scenes/FloodedChase.unity";
        const string ChaseRoot = "Chase";

        static readonly Vector3 PlayCentre = new Vector3(512f, 0f, 512f);
        const float PlaySize = 190f;

        [MenuItem("MonsterChase/Build Flooded Chase Scene")]
        public static void Build()
        {
            if (!System.IO.File.Exists(SourceScene))
            {
                Debug.LogError($"[Flooded] {SourceScene} is missing. Import the Flooded Grounds pack.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);

            // Everything we add lives under one root, and a rebuild removes only that.
            var existing = GameObject.Find(ChaseRoot);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(ChaseRoot).transform;

            // The pack's own camera would fight ours for MainCamera.
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (!cam.transform.IsChildOf(root)) cam.gameObject.SetActive(false);

            float groundY = SampleGround(PlayCentre);
            var centre = new Vector3(PlayCentre.x, groundY, PlayCentre.z);

            var surface = BakeNavMesh(root, centre);
            if (surface == null) return;

            // Without this, burning bodies counts for nothing and the monster can
            // never be killed. The test caught its absence; do not drop it again.
            var ritualGo = new GameObject("RitualState");
            ritualGo.transform.SetParent(root, false);
            ritualGo.AddComponent<RitualState>();

            var player = BuildHospital.BuildPlayer(PlaceOn(centre + new Vector3(-55f, 0f, -55f)));
            player.transform.SetParent(root, true);

            var anchors = PlaceAnchors(root, centre);
            var patrol = BuildPatrolRing(root, centre);
            var monster = BuildHospital.BuildMonster(PlaceOn(centre + new Vector3(45f, 0f, 45f)),
                                                    player.transform, patrol);
            monster.transform.SetParent(root, true);
            WireCreatureVoice(monster.gameObject, player.transform);

            var hud = BuildHospital.BuildHud(monster);
            hud.transform.SetParent(root, false);
            BuildHospital.BuildPauseMenu();
            BuildHospital.WirePlayerLife(player, hud);
            BuildHospital.BuildImpacts();
            WireBreath(player, hud);
            UiKit.EnsureEventSystem();

            int hides = PlaceHidingSpots(root, centre);
            DressWithBlood(root, anchors);

            var ammoPoints = new List<Vector3>();
            var ammoOffsets = new[]
            {
                new Vector3(-30f, 0f,  48f), new Vector3( 50f, 0f,  25f),
                new Vector3( 28f, 0f, -48f), new Vector3(-52f, 0f, -18f),
                new Vector3(  5f, 0f,   8f), new Vector3(-12f, 0f, -58f),
            };
            foreach (var o in ammoOffsets)
            {
                var want = PlaceOn(centre + o, 0.05f);
                if (NavMesh.SamplePosition(want, out var hit, 10f, NavMesh.AllAreas))
                    ammoPoints.Add(hit.position);
            }
            int boxes = BuildHospital.ScatterAmmo(root, ammoPoints);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScenes();

            Debug.Log($"[Flooded] Built {ScenePath}. Play area {PlaySize}m around the village, " +
                      $"{anchors.Count} bodies to burn, {hides} places to hide, {boxes} ammo boxes, " +
                      $"patrol ring of {patrol.transform.childCount}. Hold SPACE to hold your breath.");
        }

        // ------------------------------------------------------------- terrain

        /// <summary>Terrain height at an xz, so nothing is buried or left floating.</summary>
        static float SampleGround(Vector3 at)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain != null) return terrain.SampleHeight(at) + terrain.transform.position.y;

            return Physics.Raycast(new Vector3(at.x, 500f, at.z), Vector3.down, out var hit, 1000f)
                ? hit.point.y : 0f;
        }

        static Vector3 PlaceOn(Vector3 at, float lift = 0.2f)
        {
            var p = at; p.y = SampleGround(at) + lift; return p;
        }

        static NavMeshSurface BakeNavMesh(Transform root, Vector3 centre)
        {
            var go = new GameObject("NavMesh");
            go.transform.SetParent(root, false);
            go.transform.position = centre;

            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Volume;
            surface.center = Vector3.zero;
            surface.size = new Vector3(PlaySize, 60f, PlaySize);
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();

            if (surface.navMeshData == null)
            {
                Debug.LogError("[Flooded] NavMesh bake produced nothing; the monster could not move.");
                return null;
            }

            const string path = "Assets/_Project/Scenes/FloodedChase_NavMesh.asset";
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            AssetDatabase.SaveAssets();
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            EditorUtility.SetDirty(surface);

            var tri = NavMesh.CalculateTriangulation();
            Debug.Log($"[Flooded] NavMesh baked over {PlaySize}m: {tri.vertices.Length} verts.");
            return surface;
        }

        // --------------------------------------------------------------- cast

        static List<AnchorSite> PlaceAnchors(Transform root, Vector3 centre)
        {
            var holder = new GameObject("Anchors").transform;
            holder.SetParent(root, false);
            var list = new List<AnchorSite>();

            // Spread round the play area so no two share a sightline.
            var offsets = new[]
            {
                new Vector3(-60f, 0f,  20f), new Vector3( 15f, 0f,  65f),
                new Vector3( 62f, 0f, -10f), new Vector3( 10f, 0f, -62f),
                new Vector3(-35f, 0f, -45f),
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                var want = PlaceOn(centre + offsets[i], 0.1f);
                if (NavMesh.SamplePosition(want, out var hit, 12f, NavMesh.AllAreas)) want = hit.position;

                var body = new GameObject($"Anchor_{i:D2}");
                body.transform.SetParent(holder, false);
                body.transform.position = want;

                PresenceBuilder.BuildCorpse(body.transform, i);

                var col = body.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.center = new Vector3(0f, 0.25f, 0f);
                col.size = new Vector3(1.8f, 0.8f, 1.0f);

                var lightGo = new GameObject("FireLight");
                lightGo.transform.SetParent(body.transform, false);
                lightGo.transform.localPosition = Vector3.up * 0.6f;
                var l = lightGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.55f, 0.2f);
                l.intensity = 6f; l.range = 14f; l.enabled = false;

                var anchor = body.AddComponent<AnchorSite>();
                var so = new SerializedObject(anchor);
                so.FindProperty("fireLight").objectReferenceValue = l;
                so.FindProperty("fire").objectReferenceValue = BuildHospital.AttachFire(body.transform);
                so.ApplyModifiedPropertiesWithoutUndo();
                list.Add(anchor);
            }
            return list;
        }

        static PatrolRoute BuildPatrolRing(Transform root, Vector3 centre)
        {
            var go = new GameObject("PatrolRoute");
            go.transform.SetParent(root, false);
            var route = go.AddComponent<PatrolRoute>();

            var points = new List<Object>();
            const int count = 12;
            float radius = PlaySize * 0.34f;

            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                var want = PlaceOn(centre + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));

                // Drop any waypoint the bake could not reach, rather than sending it
                // walking at a point it can never stand on.
                if (!NavMesh.SamplePosition(want, out var hit, 15f, NavMesh.AllAreas)) continue;

                var wp = new GameObject($"WP_{i:D2}");
                wp.transform.SetParent(go.transform, false);
                wp.transform.position = hit.position;
                points.Add(wp.transform);
            }

            var so = new SerializedObject(route);
            var array = so.FindProperty("points");
            array.arraySize = points.Count;
            for (int i = 0; i < points.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[Flooded] Patrol ring: {points.Count} of {count} waypoints landed on the navmesh.");
            return route;
        }

        static void WireCreatureVoice(GameObject monster, Transform player)
        {
            var mouth = monster.AddComponent<AudioSource>();
            mouth.playOnAwake = false;
            mouth.spatialBlend = 1f;
            mouth.rolloffMode = AudioRolloffMode.Linear;
            mouth.minDistance = 5f;
            mouth.maxDistance = 70f;

            var voice = monster.AddComponent<CreatureVoice>();
            var so = new SerializedObject(voice);
            so.FindProperty("ai").objectReferenceValue = monster.GetComponent<MonsterAI>();
            so.FindProperty("mouth").objectReferenceValue = mouth;
            so.FindProperty("player").objectReferenceValue = player;
            BuildHospital.FillClips(so.FindProperty("roars"), "sfx_roar_1", "sfx_roar_2", "sfx_roar_3");
            BuildHospital.FillClips(so.FindProperty("snarls"), "sfx_snarl_1", "sfx_snarl_2", "sfx_snarl_3");
            BuildHospital.FillClips(so.FindProperty("noticeSounds"), "cre_screech_1", "cre_screech_2");
            so.ApplyModifiedPropertiesWithoutUndo();

            // Always-on growl: the only dependable way to tell where it is while you are
            // face down in the water with your breath held.
            var constant = monster.AddComponent<AudioSource>();
            constant.clip = BuildHospital.FindClip("cre_growl_constant");
            constant.loop = true;
            constant.playOnAwake = true;
            constant.spatialBlend = 1f;
            constant.rolloffMode = AudioRolloffMode.Linear;
            constant.minDistance = 4f;
            constant.maxDistance = 40f;
            constant.volume = 0.6f;
        }

        static void WireBreath(GameObject player, MonsterChase.UI.VitalsReadout hud)
        {
            var breathAudio = player.AddComponent<AudioSource>();
            breathAudio.clip = BuildHospital.FindClip("sfx_breath_loop");
            breathAudio.loop = true;
            breathAudio.playOnAwake = true;
            breathAudio.spatialBlend = 0f;
            breathAudio.volume = 0f;

            var breath = player.AddComponent<PlayerBreath>();
            var so = new SerializedObject(breath);
            so.FindProperty("controller").objectReferenceValue =
                player.GetComponent<MonsterChase.Player.FirstPersonController>();
            so.FindProperty("breathing").objectReferenceValue = breathAudio;
            so.FindProperty("gaspClip").objectReferenceValue = BuildHospital.FindClip("sfx_gasp");
            so.ApplyModifiedPropertiesWithoutUndo();

            var meter = UiKit.NewImage("BreathBack", hud.transform, new Color(0f, 0f, 0f, 0.5f));
            UiKit.Place(meter.rectTransform, 0.40f, 0.60f, 0.10f, 0.13f);
            var fill = UiKit.NewImage("Fill", meter.transform, new Color(0.62f, 0.78f, 0.86f));
            UiKit.Stretch(fill.rectTransform);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

            var readout = hud.gameObject.AddComponent<MonsterChase.UI.BreathMeter>();
            var bso = new SerializedObject(readout);
            bso.FindProperty("breath").objectReferenceValue = breath;
            bso.FindProperty("fill").objectReferenceValue = fill;
            bso.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Out here there are no beds, so hiding is behind and under the village's own
        /// cover. Points are sampled on the navmesh near scattered offsets so a spot is
        /// never inside a wall.
        /// </summary>
        static int PlaceHidingSpots(Transform root, Vector3 centre)
        {
            var holder = new GameObject("HidingSpots").transform;
            holder.SetParent(root, false);

            int made = 0;
            var rng = new System.Random(11);
            for (int i = 0; i < 18; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 12f + (float)rng.NextDouble() * (PlaySize * 0.42f);
                var want = PlaceOn(centre + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                if (!NavMesh.SamplePosition(want, out var hit, 8f, NavMesh.AllAreas)) continue;

                var go = new GameObject($"Hide_{made:D2}");
                go.transform.SetParent(holder, false);
                go.transform.position = hit.position;

                var trigger = go.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(1.6f, 1.4f, 1.6f);
                trigger.center = Vector3.up * 0.7f;

                var hide = new GameObject("HideAnchor");
                hide.transform.SetParent(go.transform, false);
                hide.transform.localPosition = new Vector3(0f, 0.35f, 0f);

                var exit = new GameObject("ExitAnchor");
                exit.transform.SetParent(go.transform, false);
                exit.transform.localPosition = new Vector3(1.2f, 0.1f, 0f);

                var spot = go.AddComponent<HidingSpot>();
                var so = new SerializedObject(spot);
                so.FindProperty("hideAnchor").objectReferenceValue = hide.transform;
                so.FindProperty("exitAnchor").objectReferenceValue = exit.transform;
                so.FindProperty("enterPrompt").stringValue = "get down and hide";
                so.ApplyModifiedPropertiesWithoutUndo();
                made++;
            }
            return made;
        }

        static void DressWithBlood(Transform root, List<AnchorSite> anchors)
        {
            var pool = BuildHospital.FindPrefab("VFX_Splat_01_Floor_Rot");
            if (pool == null) return;

            var holder = new GameObject("Blood").transform;
            holder.SetParent(root, false);
            foreach (var a in anchors)
                BuildHospital.Spawn(pool, holder, a.transform.position + Vector3.up * 0.03f,
                                    Random.Range(0f, 360f));
        }

        static void RegisterScenes()
        {
            var wanted = new[]
            {
                "Assets/_Project/Scenes/Menu.unity",
                ScenePath,
                "Assets/_Project/Scenes/Hospital.unity",
                "Assets/_Project/Scenes/Testbed.unity",
            };
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in wanted)
                if (System.IO.File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
