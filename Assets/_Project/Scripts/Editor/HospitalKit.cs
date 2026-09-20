using UnityEngine;
using UnityEditor;

namespace MonsterChase.EditorTools
{
    /// <summary>Primitive geometry helpers. Boxes, walls with door gaps, materials.</summary>
    public static class HospitalKit
    {
        public const float WallHeight = 3.0f;
        public const float WallThick = 0.15f;
        public const float DoorWidth = 1.3f;    // code minimum is 1.2m for a stretcher
        public const float DoorHeight = 2.1f;

        public static GameObject Box(Transform parent, string name, Vector3 centre,
                                     Vector3 size, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            go.transform.localScale = size;
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>
        /// A straight wall from a to b, optionally with a doorway gap centred at
        /// <paramref name="doorAt"/> (0..1 along the run). Built as up to three boxes:
        /// the two sides and the lintel over the opening.
        /// </summary>
        public static void Wall(Transform parent, string name, Vector2 a, Vector2 b,
                                Material mat, float doorAt = -1f)
        {
            var dir = b - a;
            float length = dir.magnitude;
            if (length < 0.01f) return;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var mid = (a + b) * 0.5f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(mid.x, 0f, mid.y);
            root.transform.rotation = Quaternion.Euler(0f, -angle, 0f);

            if (doorAt < 0f)
            {
                Box(root.transform, "Span",
                    new Vector3(0f, WallHeight * 0.5f, 0f),
                    new Vector3(length, WallHeight, WallThick), mat);
                root.transform.position = new Vector3(mid.x, 0f, mid.y);
                foreach (Transform c in root.transform) c.localPosition = new Vector3(0f, WallHeight * 0.5f, 0f);
                return;
            }

            float doorCentre = Mathf.Clamp(doorAt, 0f, 1f) * length - length * 0.5f;
            float leftEnd = doorCentre - DoorWidth * 0.5f;
            float rightStart = doorCentre + DoorWidth * 0.5f;

            float leftLen = leftEnd + length * 0.5f;
            if (leftLen > 0.05f)
                Local(root.transform, "Left", new Vector3(-length * 0.5f + leftLen * 0.5f, WallHeight * 0.5f, 0f),
                      new Vector3(leftLen, WallHeight, WallThick), mat);

            float rightLen = length * 0.5f - rightStart;
            if (rightLen > 0.05f)
                Local(root.transform, "Right", new Vector3(rightStart + rightLen * 0.5f, WallHeight * 0.5f, 0f),
                      new Vector3(rightLen, WallHeight, WallThick), mat);

            float lintel = WallHeight - DoorHeight;
            if (lintel > 0.05f)
                Local(root.transform, "Lintel", new Vector3(doorCentre, DoorHeight + lintel * 0.5f, 0f),
                      new Vector3(DoorWidth, lintel, WallThick), mat);
        }

        static void Local(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        public static Material Mat(string name, Color c, float smoothness = 0.1f)
        {
            System.IO.Directory.CreateDirectory("Assets/_Project/Materials");
            var path = $"Assets/_Project/Materials/M_{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) { existing.color = c; return existing; }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader) { name = $"M_{name}" };
            m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
