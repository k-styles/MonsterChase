using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using MonsterChase.Monster;
using MonsterChase.Player;
using MonsterChase.Ritual;
using MonsterChase.UI;

namespace MonsterChase.EditorTools
{
    /// <summary>
    /// A grey box with the numbers showing. No map, no art -- just the stagger/heal
    /// loop and five anchors, so the arc can be tuned before anything is dressed up.
    ///
    /// Generated, like every scene in this project: change this script, not the scene.
    /// </summary>
    public static class BuildTestbed
    {
        const string ScenePath = "Assets/_Project/Scenes/Testbed.unity";

        [MenuItem("MonsterChase/Build Testbed Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");

            BuildLighting();
            BuildFloor();

            var ritualGo = new GameObject("RitualState");
            ritualGo.AddComponent<RitualState>();

            var player = BuildPlayer(new Vector3(0f, 0.1f, -9f));
            var monster = BuildMonster(new Vector3(0f, 0f, 6f));
            BuildAnchors(5, 11f);

            var hud = BuildHud(monster);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"[Testbed] Built {ScenePath}. " +
                      "WASD move, Shift run, Ctrl crouch, LMB shoot, R reload, " +
                      "hold F at a body to burn it, ` toggles the readout.");
        }

        static void BuildLighting()
        {
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 0.55f;
            sun.color = new Color(0.72f, 0.76f, 0.85f);
            sunGo.transform.rotation = Quaternion.Euler(52f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.17f, 0.2f);
        }

        static void BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = Vector3.one * 4f;
            floor.GetComponent<MeshRenderer>().sharedMaterial = Grey(new Color(0.3f, 0.31f, 0.33f));
        }

        static GameObject BuildPlayer(Vector3 position)
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = position;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.65f, 0f);

            var camGo = new GameObject("MainCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.02f;
            cam.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();

            var controller = player.AddComponent<FirstPersonController>();
            var cso = new SerializedObject(controller);
            cso.FindProperty("cameraPivot").objectReferenceValue = pivot.transform;
            cso.ApplyModifiedPropertiesWithoutUndo();

            var gun = player.AddComponent<Gun>();
            var audio = player.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            var gso = new SerializedObject(gun);
            gso.FindProperty("sourceCamera").objectReferenceValue = cam;
            gso.FindProperty("fireAudio").objectReferenceValue = audio;
            gso.ApplyModifiedPropertiesWithoutUndo();

            return player;
        }

        static MonsterVitals BuildMonster(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Monster";
            go.transform.position = position + Vector3.up * 1.3f;
            go.transform.localScale = new Vector3(1.1f, 1.3f, 1.1f);
            go.GetComponent<MeshRenderer>().sharedMaterial = Grey(new Color(0.55f, 0.18f, 0.18f));

            var vitals = go.AddComponent<MonsterVitals>();
            go.AddComponent<MonsterStaggerTell>();
            return vitals;
        }

        static void BuildAnchors(int count, float radius)
        {
            var root = new GameObject("Anchors").transform;
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f;
                var pos = new Vector3(Mathf.Cos(a) * radius, 0.15f, Mathf.Sin(a) * radius);

                // A flattened capsule reads as a body on the floor well enough to tune against.
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = $"Anchor_{i:D2}";
                body.transform.SetParent(root, false);
                body.transform.position = pos;
                body.transform.rotation = Quaternion.Euler(90f, a * Mathf.Rad2Deg, 0f);
                body.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
                body.GetComponent<MeshRenderer>().sharedMaterial = Grey(new Color(0.42f, 0.2f, 0.22f));
                Object.DestroyImmediate(body.GetComponent<Collider>());

                var light = new GameObject("FireLight");
                light.transform.SetParent(body.transform, false);
                var l = light.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.55f, 0.2f);
                l.intensity = 4f;
                l.range = 9f;
                l.enabled = false;

                var anchor = body.AddComponent<AnchorSite>();
                var aso = new SerializedObject(anchor);
                aso.FindProperty("fireLight").objectReferenceValue = l;
                aso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static VitalsReadout BuildHud(MonsterVitals monster)
        {
            var canvasGo = new GameObject("HUD", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var readout = NewText("Readout", canvasGo.transform, 22, TextAnchor.UpperLeft);
            readout.color = new Color(0.85f, 0.87f, 0.9f);
            Place(readout.rectTransform, 0.02f, 0.30f, 0.68f, 0.97f);

            var healthBack = NewImage("MonsterHealthBack", canvasGo.transform, new Color(0f, 0f, 0f, 0.5f));
            Place(healthBack.rectTransform, 0.35f, 0.65f, 0.90f, 0.93f);
            var healthFill = NewImage("Fill", healthBack.transform, new Color(0.75f, 0.2f, 0.2f));
            Stretch(healthFill.rectTransform);
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;

            var burnBack = NewImage("BurnBack", canvasGo.transform, new Color(0f, 0f, 0f, 0.5f));
            Place(burnBack.rectTransform, 0.40f, 0.60f, 0.16f, 0.19f);
            var burnFill = NewImage("Fill", burnBack.transform, new Color(1f, 0.55f, 0.18f));
            Stretch(burnFill.rectTransform);
            burnFill.type = Image.Type.Filled;
            burnFill.fillMethod = Image.FillMethod.Horizontal;

            var prompt = NewText("Prompt", canvasGo.transform, 24, TextAnchor.LowerCenter);
            prompt.color = new Color(1f, 1f, 1f, 0.75f);
            Place(prompt.rectTransform, 0.3f, 0.7f, 0.20f, 0.24f);

            var crosshair = NewImage("Crosshair", canvasGo.transform, new Color(1f, 1f, 1f, 0.55f));
            crosshair.rectTransform.anchorMin = crosshair.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            crosshair.rectTransform.sizeDelta = new Vector2(4f, 4f);
            crosshair.rectTransform.anchoredPosition = Vector2.zero;

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

        // ------------------------------------------------------------------ helpers

        static Material Grey(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader) { name = $"M_{ColorUtility.ToHtmlStringRGB(c)}" };
            m.color = c;
            System.IO.Directory.CreateDirectory("Assets/_Project/Materials");
            var path = $"Assets/_Project/Materials/{m.name}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null) AssetDatabase.CreateAsset(m, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        static Text NewText(string name, Transform parent, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static Image NewImage(string name, Transform parent, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        static void Place(RectTransform r, float xMin, float xMax, float yMin, float yMax)
        {
            r.anchorMin = new Vector2(xMin, yMin);
            r.anchorMax = new Vector2(xMax, yMax);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
