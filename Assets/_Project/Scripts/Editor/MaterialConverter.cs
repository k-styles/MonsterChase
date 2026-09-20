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

        [MenuItem("MonsterChase/Convert Pack Materials to URP")]
        public static void Convert()
        {
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp == null)
            {
                Debug.LogError("[Convert] URP/Lit shader not found. Is the URP package installed?");
                return;
            }

            int converted = 0, skipped = 0;
            var touched = new List<string>();

            foreach (var guid in AssetDatabase.FindAssets("t:Material"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/")) continue;

                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || m.shader == null) continue;

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
            Debug.Log($"[Convert] {converted} materials moved to URP/Lit, {skipped} already fine. " +
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
                    n.StartsWith("Legacy Shaders/") || n.Contains("HDRP") || n == "Hidden/InternalErrorShader")
                {
                    if (bad < 8) Debug.LogWarning($"[Audit] {n}  <-  {path}");
                    bad++;
                }
            }
            Debug.Log($"[Audit] {bad} materials would render wrong under URP.");
        }
    }
}
