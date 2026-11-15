using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace PulseDesk
{
    /// <summary>
    /// Builds the visuals from code so there are no prefabs to wire or merge.
    /// Text sizes are in millimetres (text objects are scaled by 0.01).
    /// </summary>
    public static class Make
    {
        private const float TextScale = 0.01f;
        private static Material _baseMaterial;

        public static Material NewMaterial(Color color)
        {
            bool glass = color.a < 0.99f;
            Material source = glass ? GlassBase() : SolidBase();
            Material m = new Material(source);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);   // both sides: the shapes are built from code
            if (glass) m.renderQueue = (int)RenderQueue.Transparent - 20;   // always before text
            SetColor(m, color);
            return m;
        }

        private static Material SolidBase()
        {
            if (_baseMaterial == null)
            {
                // Created by the menu PulseDesk > 1. Set up project, so the shader is included in builds.
                _baseMaterial = Resources.Load<Material>("PulseDeskSurface");
            }
            if (_baseMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                _baseMaterial = new Material(shader);
                Debug.LogWarning("PulseDesk: Resources/PulseDeskSurface.mat not found. Run the menu PulseDesk > 1. Set up project before building.");
            }
            return _baseMaterial;
        }

        /// <summary>See-through material for the back panels, so the real desk shows through them.</summary>
        private static Material GlassBase()
        {
            if (_glassMaterial == null) _glassMaterial = Resources.Load<Material>("PulseDeskGlass");
            if (_glassMaterial == null)
            {
                _glassMaterial = new Material(SolidBase());
                ConfigureGlass(_glassMaterial);
            }
            return _glassMaterial;
        }

        /// <summary>Turns a URP Unlit material into an alpha-blended one. Also used by the editor menu.</summary>
        public static void ConfigureGlass(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            // Before text and every other see-through thing, so a panel can never cover its own labels.
            // URP rebuilds the queue from _QueueOffset when it validates a material, so set both.
            m.SetFloat("_QueueOffset", -20f);
            m.renderQueue = (int)RenderQueue.Transparent - 20;
        }

        public static void SetColor(Material m, Color color)
        {
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        }

        public static GameObject Empty(string name, Transform parent, Vector3 localPos, Quaternion localRot)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            return go;
        }

        /// <summary>
        /// A coloured slab with no collider. Flat panels, cards and buttons get rounded corners;
        /// thin bars stay plain boxes. A colour with alpha below 1 makes it glass.
        /// </summary>
        public static Renderer Box(string name, Transform parent, Vector3 localPos, Vector3 size, Color color)
        {
            bool rounded = size.z <= 0.02f && Mathf.Min(size.x, size.y) >= 0.012f;
            GameObject go;
            if (rounded)
            {
                go = new GameObject(name);
                go.AddComponent<MeshFilter>().sharedMesh = RoundedSlab(size, Mathf.Min(0.010f, 0.3f * Mathf.Min(size.x, size.y)));
                go.AddComponent<MeshRenderer>();
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPos;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                Collider c = go.GetComponent<Collider>();
                if (c != null) Object.Destroy(c);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPos;
                go.transform.localScale = size;
            }
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = NewMaterial(color);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        /// <summary>A slab with rounded corners, built at its real size and shared between equal shapes.</summary>
        private static Mesh RoundedSlab(Vector3 size, float radius)
        {
            string key = size.x.ToString("F4") + "/" + size.y.ToString("F4") + "/" + size.z.ToString("F4") + "/" + radius.ToString("F4");
            Mesh mesh;
            if (_slabs.TryGetValue(key, out mesh) && mesh != null) return mesh;

            const int seg = 6;
            float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;
            int ring = 4 * (seg + 1);
            Vector2[] outline = new Vector2[ring];
            for (int c = 0; c < 4; c++)
            {
                float sx = (c == 0 || c == 3) ? 1f : -1f;
                float sy = (c == 0 || c == 1) ? 1f : -1f;
                Vector2 centre = new Vector2(sx * (hx - radius), sy * (hy - radius));
                for (int i = 0; i <= seg; i++)
                {
                    float a = (c * 90f + 90f * i / seg) * Mathf.Deg2Rad;
                    outline[c * (seg + 1) + i] = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                }
            }

            // Vertices: front ring, back ring, front centre, back centre.
            Vector3[] verts = new Vector3[ring * 2 + 2];
            for (int i = 0; i < ring; i++)
            {
                verts[i] = new Vector3(outline[i].x, outline[i].y, -hz);
                verts[ring + i] = new Vector3(outline[i].x, outline[i].y, hz);
            }
            int frontCentre = ring * 2, backCentre = ring * 2 + 1;
            verts[frontCentre] = new Vector3(0f, 0f, -hz);
            verts[backCentre] = new Vector3(0f, 0f, hz);

            int[] tris = new int[ring * 12];
            int t = 0;
            for (int i = 0; i < ring; i++)
            {
                int n = (i + 1) % ring;
                tris[t++] = frontCentre; tris[t++] = n; tris[t++] = i;                 // front
                tris[t++] = backCentre; tris[t++] = ring + i; tris[t++] = ring + n;    // back
                tris[t++] = i; tris[t++] = n; tris[t++] = ring + n;                    // side
                tris[t++] = i; tris[t++] = ring + n; tris[t++] = ring + i;
            }

            mesh = new Mesh();
            mesh.name = "Slab " + key;
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            _slabs[key] = mesh;
            return mesh;
        }

        private static Material _glassMaterial;
        private static readonly System.Collections.Generic.Dictionary<string, Mesh> _slabs =
            new System.Collections.Generic.Dictionary<string, Mesh>();
        /// <summary>World-space text facing -Z (toward the user). size in metres, fontMm in millimetres.</summary>
        public static TextMeshPro Text(string name, Transform parent, Vector3 localPos, Vector2 size, float fontMm,
            Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * TextScale;
            TextMeshPro t = go.AddComponent<TextMeshPro>();
            t.rectTransform.sizeDelta = size / TextScale;
            t.fontSize = fontMm;   // TMP: 10 pt = 1 unit, and this object is scaled by 0.01
            t.color = color;
            t.alignment = align;
#if UNITY_6000_0_OR_NEWER
            t.textWrappingMode = TextWrappingModes.Normal;
#else
            t.enableWordWrapping = true;
#endif
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.richText = false;
            t.text = "";
            return t;
        }

        /// <summary>Lane icon built from small bars so no font glyph or texture is needed.</summary>
        public static void Icon(TaskLane lane, Transform parent, Vector3 localPos, Color color)
        {
            Transform root = Empty("Icon", parent, localPos, Quaternion.identity).transform;
            const float w = 0.0035f;
            const float d = 0.002f;
            if (lane == TaskLane.Urgent)
            {
                Box("Bar", root, new Vector3(0f, 0.003f, 0f), new Vector3(w, 0.011f, d), color);
                Box("Dot", root, new Vector3(0f, -0.007f, 0f), new Vector3(w, w, d), color);
            }
            else if (lane == TaskLane.Next)
            {
                Bar(root, new Vector3(0f, 0.0033f, 0f), 0.011f, 45f, w, d, color);
                Bar(root, new Vector3(0f, -0.0033f, 0f), 0.011f, -45f, w, d, color);
            }
            else
            {
                Bar(root, new Vector3(-0.0045f, -0.0025f, 0f), 0.007f, 45f, w, d, color);
                Bar(root, new Vector3(0.002f, 0.0005f, 0f), 0.014f, -45f, w, d, color);
            }
        }

        private static void Bar(Transform parent, Vector3 pos, float length, float zDegrees, float w, float d, Color color)
        {
            Renderer r = Box("Bar", parent, pos, new Vector3(w, length, d), color);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, zDegrees);
        }
    }
}
