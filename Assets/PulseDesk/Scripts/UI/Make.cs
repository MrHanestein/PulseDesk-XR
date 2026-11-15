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
            Material m = new Material(_baseMaterial);
            SetColor(m, color);
            return m;
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

        /// <summary>A coloured box with no collider. Never parent anything to it (it is scaled).</summary>
        public static Renderer Box(string name, Transform parent, Vector3 localPos, Vector3 size, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = NewMaterial(color);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

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
