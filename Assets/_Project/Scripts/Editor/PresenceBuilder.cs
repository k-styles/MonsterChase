using UnityEngine;
using UnityEditor;

namespace MonsterChase.EditorTools
{
    /// <summary>
    /// The monster's body and the corpses, both from bought packs. Ported from the
    /// QuietHouse version, because getting a bought FBX to stand at the right height
    /// with its animations actually playing took three goes there and should not need
    /// a fourth here.
    /// </summary>
    public static class PresenceBuilder
    {
        public const string DemonRoot = "Assets/Demon Horror Creature with Weapon";
        public const string NpcRoot = "Assets/npc_casual_set_00";

        /// <summary>
        /// Instantiates the demon under <paramref name="parent"/>, scaled to
        /// <paramref name="targetHeight"/>. Returns its Animator, or null if the pack
        /// is missing and a capsule stood in.
        /// </summary>
        public static Animator BuildMonsterBody(Transform parent, float targetHeight)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{DemonRoot}/Prefabs/Demon_default.prefab");
            if (prefab == null)
            {
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = "Body";
                capsule.transform.SetParent(parent, false);
                capsule.transform.localPosition = new Vector3(0f, targetHeight * 0.5f, 0f);
                capsule.transform.localScale = new Vector3(0.9f, targetHeight * 0.5f, 0.9f);
                Object.DestroyImmediate(capsule.GetComponent<Collider>());
                Debug.LogWarning("[MonsterChase] Demon pack missing; the monster is a capsule.");
                return null;
            }

            var body = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            body.name = "Body";
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;

            // Measured, not guessed: scale until it actually stands at the target.
            body.transform.localScale = Vector3.one;
            float measured = MeasureHeight(body);
            if (measured > 0.0001f)
            {
                body.transform.localScale = Vector3.one * (targetHeight / measured);
                Debug.Log($"[MonsterChase] Demon measured {measured:F2}m; scaled to {targetHeight}m.");
            }

            var animator = body.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                // An Animator with no avatar plays nothing, and the avatar lives on the
                // source FBX rather than the prefab.
                if (animator.avatar == null)
                {
                    var avatar = FindAvatar(DemonRoot);
                    if (avatar != null) animator.avatar = avatar;
                    else Debug.LogWarning("[MonsterChase] No avatar for the demon; it will not animate.");
                }
                if (animator.runtimeAnimatorController == null)
                {
                    var c = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        $"{DemonRoot}/DemoSceneAssets/DemoAnimCont.controller");
                    if (c != null) animator.runtimeAnimatorController = c;
                }
            }

            // Its colliders would fight the NavMeshAgent and bake into the navmesh.
            foreach (var c in body.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

            StripEmission(body);
            return animator;
        }

        /// <summary>
        /// One of the pack's characters, laid out on the floor as a corpse. Index picks
        /// which, so five bodies are five different people rather than one five times.
        /// </summary>
        public static GameObject BuildCorpse(Transform parent, int index)
        {
            var guids = AssetDatabase.FindAssets("character t:Prefab", new[] { $"{NpcRoot}/Prefabs" });
            if (guids.Length == 0)
            {
                Debug.LogWarning("[MonsterChase] NPC pack missing; corpses are capsules.");
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.transform.SetParent(parent, false);
                capsule.transform.localScale = new Vector3(0.5f, 0.85f, 0.5f);
                return capsule;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[index % guids.Length]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = $"Corpse_{index:D2}";

            // Face down, rolled slightly, so a row of them does not look like a queue.
            go.transform.localRotation = Quaternion.Euler(90f, index * 53f, index * 11f);

            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var a in go.GetComponentsInChildren<Animator>()) Object.DestroyImmediate(a);
            return go;
        }

        /// <summary>
        /// Swaps any self-lit material for an unlit copy. The demon's weapon carries a
        /// bright emissive ball, which in a dark ward is a glowing dot that follows you
        /// around and gives the monster away from across the map.
        /// </summary>
        static void StripEmission(GameObject body)
        {
            foreach (var renderer in body.GetComponentsInChildren<Renderer>())
            {
                var mats = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !m.HasProperty("_EmissionColor")) continue;
                    if (m.GetColor("_EmissionColor").maxColorComponent <= 0.002f) continue;
                    mats[i] = DarkCopy(m);
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = mats;
            }
        }

        static Material DarkCopy(Material source)
        {
            const string dir = "Assets/_Project/Materials";
            System.IO.Directory.CreateDirectory(dir);
            var path = $"{dir}/{source.name}_Unlit.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var copy = new Material(source) { name = source.name + "_Unlit" };
            copy.SetColor("_EmissionColor", Color.black);
            copy.DisableKeyword("_EMISSION");
            copy.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            AssetDatabase.CreateAsset(copy, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        public static float MeasureHeight(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0f;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.size.y;
        }

        public static Avatar FindAvatar(string folder)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (sub is Avatar avatar && avatar.isValid) return avatar;
            }
            return null;
        }
    }
}
