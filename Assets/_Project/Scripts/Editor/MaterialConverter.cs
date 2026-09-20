using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MonsterChase.EditorTools
{
    /// <summary>
    /// Moves materials off the built-in Standard shader onto URP/Lit.
    ///
    /// Every bought pack in this project was authored for the Built-in pipeline. Under
    /// URP their shader does not exist, so Unity draws them magenta. This remaps the
    /// slots that changed name between the two and leaves the rest alone.
    ///
    /// Run it again after importing any new pack. It is idempotent -- a material
    /// already on URP/Lit is skipped.
    /// </summary>
    public static class MaterialConverter
    {
        // Standard -> URP/Lit, only where the property name actually changed.
        static readonly (string from, string to)[] TextureSlots =
        {
            ("_MainTex", "_BaseMap"),
        };

        static readonly (string from, string to)[] ColorSlots =
        {
            ("_Color", "_BaseColor"),
        };

        /// <summary>
        /// Materials a ParticleSystemRenderer draws with. They must never go to
        /// URP/Lit: a lit opaque shader on a particle draws a solid block instead of a
        /// soft sprite, which is exactly what happened to the falling leaves.
        /// </summary>
        static HashSet<Material> ParticleMaterials()
        {
            var set = new HashSet<Material>();

            void Collect(GameObject go)
            {
                foreach (var pr in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    if (pr.sharedMaterial != null) set.Add(pr.sharedMaterial);
                    if (pr.trailMaterial != null) set.Add(pr.trailMaterial);
                }
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/")) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null) Collect(go);
            }

            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                Collect(ps.gameObject);

            return set;
        }

        /// <summary>
        /// Unity's Tree Creator shaders do not exist under URP, so every tree on a
        /// terrain built with them renders magenta. Bark becomes an opaque URP/Lit;
        /// leaves become URP/Lit with alpha clipping, which is the nearest honest
        /// equivalent -- the billboard crossfade and wind of the original are lost.
        /// </summary>
        [MenuItem("MonsterChase/Repair Tree Materials")]
        public static void RepairTrees()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) { Debug.LogError("[Trees] URP/Lit not found."); return; }

            int bark = 0, leaves = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/")) continue;
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || m.shader == null) continue;

                var n = m.shader.name;
                if (!n.Contains("Tree Creator") && !n.Contains("Tree Soft Occlusion")) continue;

                bool isLeaf = n.Contains("Leaves");
                var tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                var col = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                float cutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f;

                m.shader = lit;
                if (tex != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);

                if (isLeaf)
                {
                    // Leaves are a texture with holes in it. Without alpha clipping
                    // every leaf card renders as a solid rectangle.
                    if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
                    if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", Mathf.Max(0.35f, cutoff));
                    m.EnableKeyword("_ALPHATEST_ON");
                    m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    // Leaf cards are visible from behind.
                    if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
                    leaves++;
                }
                else bark++;

                EditorUtility.SetDirty(m);
            }

            // Tree Creator stores its optimised bark/leaf materials as sub-assets inside
            // the tree prefab, so a scan of standalone .mat files never sees them. Those
            // are the ones that were still magenta. Walk the terrain's prototypes.
            foreach (var terrain in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            {
                if (terrain.terrainData == null) continue;
                foreach (var proto in terrain.terrainData.treePrototypes)
                {
                    if (proto.prefab == null) continue;
                    foreach (var r in proto.prefab.GetComponentsInChildren<Renderer>(true))
                        foreach (var m in r.sharedMaterials)
                        {
                            if (m == null || m.shader == null) continue;
                            var sn = m.shader.name;
                            if (!sn.Contains("Tree Creator") && !sn.Contains("Tree Soft Occlusion")) continue;

                            bool leaf = sn.Contains("Leaves");
                            ConvertTreeMaterial(m, lit, leaf);
                            if (leaf) leaves++; else bark++;
                        }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Trees] {bark} bark and {leaves} leaf materials moved to URP/Lit.");
        }

        static void ConvertTreeMaterial(Material m, Shader lit, bool isLeaf)
        {
            var tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            var col = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            float cutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f;

            m.shader = lit;
            if (tex != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);

            if (isLeaf)
            {
                if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
                if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", Mathf.Max(0.35f, cutoff));
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);   // leaf cards are two-sided
            }
            EditorUtility.SetDirty(m);
        }

        [MenuItem("MonsterChase/Repair Particle Materials")]
        public static void RepairParticles()
        {
            var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null)
            {
                Debug.LogError("[Repair] URP Particles/Unlit shader not found.");
                return;
            }

            int fixedUp = 0;
            foreach (var m in ParticleMaterials())
            {
                if (m == null || m.shader == null) continue;
                if (m.shader.name != "Universal Render Pipeline/Lit"
                    && m.shader.name != "Standard"
                    && !m.shader.name.StartsWith("Legacy Shaders/")) continue;

                var tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap")
                        : m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                var col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor")
                        : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;

                m.shader = particleShader;
                if (tex != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);

                // Soft and additive-friendly, which is what foliage and smoke want.
                if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);   // transparent
                if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);       // alpha
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                EditorUtility.SetDirty(m);
                fixedUp++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Repair] {fixedUp} particle materials moved to URP/Particles/Unlit.");
        }

        [MenuItem("MonsterChase/Convert Pack Materials to URP")]
        public static void Convert()
        {
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp == null)
            {
                Debug.LogError("[Convert] URP/Lit shader not found. Is the URP package installed?");
                return;
            }

            // Anything a particle system draws with is handled separately -- sending it
            // to URP/Lit is what turned the falling leaves into solid blocks.
            var particleMats = ParticleMaterials();

            int converted = 0, skipped = 0;
            var touched = new List<string>();

            foreach (var guid in AssetDatabase.FindAssets("t:Material"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/")) continue;

                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || m.shader == null) continue;
                if (particleMats.Contains(m)) { skipped++; continue; }

                var name = m.shader.name;
                bool builtIn = name == "Standard" || name == "Standard (Specular setup)"
                            || name.StartsWith("Legacy Shaders/");
                if (!builtIn) { skipped++; continue; }

                // Read the old values before the shader swap drops them.
                var carriedTextures = new Dictionary<string, (Texture tex, Vector2 scale, Vector2 offset)>();
                foreach (var (from, to) in TextureSlots)
                    if (m.HasProperty(from) && m.GetTexture(from) != null)
                        carriedTextures[to] = (m.GetTexture(from), m.GetTextureScale(from), m.GetTextureOffset(from));

                var carriedColors = new Dictionary<string, Color>();
                foreach (var (from, to) in ColorSlots)
                    if (m.HasProperty(from)) carriedColors[to] = m.GetColor(from);

                float smoothness = m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : 0.5f;
                float metallic = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;

                m.shader = urp;

                foreach (var kv in carriedTextures)
                {
                    if (!m.HasProperty(kv.Key)) continue;
                    m.SetTexture(kv.Key, kv.Value.tex);
                    m.SetTextureScale(kv.Key, kv.Value.scale);
                    m.SetTextureOffset(kv.Key, kv.Value.offset);
                }
                foreach (var kv in carriedColors)
                    if (m.HasProperty(kv.Key)) m.SetColor(kv.Key, kv.Value);

                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);

                EditorUtility.SetDirty(m);
                converted++;
                if (touched.Count < 6) touched.Add(System.IO.Path.GetFileName(path));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RepairParticles();
            RepairTrees();
            Debug.Log($"[Convert] {converted} materials moved to URP/Lit, {skipped} left alone. " +
                      (touched.Count > 0 ? "e.g. " + string.Join(", ", touched) : ""));
        }

        /// <summary>Reports anything still on a shader URP cannot draw.</summary>
        [MenuItem("MonsterChase/Audit Materials")]
        public static void Audit()
        {
            int bad = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/")) continue;
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || m.shader == null) { bad++; continue; }

                var n = m.shader.name;
                if (n == "Standard" || n == "Standard (Specular setup)" ||
                    n.StartsWith("Legacy Shaders/") || n.Contains("HDRP") ||
                    n.Contains("Tree Creator") || n.Contains("Tree Soft Occlusion") ||
                    n == "Hidden/InternalErrorShader" || !m.shader.isSupported)
                {
                    if (bad < 8) Debug.LogWarning($"[Audit] {n}  <-  {path}");
                    bad++;
                }
            }
            Debug.Log($"[Audit] {bad} materials would render wrong under URP.");
        }
    }
}
