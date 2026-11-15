using System;
using TMPro;
using UnityEngine;

namespace PulseDesk
{
    /// <summary>A pinch/poke button. Hover brightens the fill; press dips the contents briefly.</summary>
    public class DockButton : PinchTarget
    {
        public Action Pressed;

        private Renderer _body;
        private TextMeshPro _label;
        private Transform _contents;
        private float _lastPress = -10f;
        private float _dip;
        private bool _hovered;

        public static DockButton Create(string name, Transform parent, Vector3 localPos, Vector2 size, string label,
            float fontMm, Action pressed)
        {
            GameObject go = Make.Empty(name, parent, localPos, Quaternion.identity);
            DockButton b = go.AddComponent<DockButton>();
            b.Pressed = pressed;

            BoxCollider col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 0.02f);
            col.isTrigger = true;

            b._contents = Make.Empty("Contents", go.transform, Vector3.zero, Quaternion.identity).transform;
            b._body = Make.Box("Body", b._contents, Vector3.zero, new Vector3(size.x, size.y, 0.008f), Theme.Button);
            b._label = Make.Text("Label", b._contents, new Vector3(0f, 0f, -0.0048f),
                new Vector2(size.x - 0.008f, size.y - 0.004f), fontMm, Theme.TextMain, TextAlignmentOptions.Center);
            b._label.text = label;
            return b;
        }

        public void SetLabel(string text)
        {
            if (_label != null) _label.text = text;
        }

        public string LabelText { get { return _label != null ? _label.text : ""; } }

        public void SetLabelColor(Color color)
        {
            if (_label != null) _label.color = color;
        }

        protected override void OnHoverChanged(bool hovered)
        {
            _hovered = hovered;
            if (_body == null) return;
            Make.SetColor(_body.sharedMaterial, hovered ? Theme.ButtonHover : Theme.Button);
        }

        public override void OnPress()
        {
            // One press per half second, however it arrived (pinch and poke can both fire).
            if (Time.unscaledTime - _lastPress < 0.5f) return;
            _lastPress = Time.unscaledTime;
            _dip = 1f;
            if (Pressed != null) Pressed();
        }

        private void Update()
        {
            if (_dip <= 0f && _contents.localScale.x >= 0.999f) return;
            _dip = Mathf.Max(0f, _dip - Time.unscaledDeltaTime / 0.18f);
            float s = 1f - 0.08f * _dip;
            _contents.localScale = new Vector3(s, s, 1f);
        }
    }
}
