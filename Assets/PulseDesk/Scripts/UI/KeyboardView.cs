using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PulseDesk
{
    /// <summary>
    /// A keyboard on the dock, poked or pinched like every other button, so typing works with
    /// bare hands on any headset and needs no system service. A physical keyboard also types
    /// into it (Editor, or a Bluetooth keyboard). Built from code.
    /// </summary>
    public class KeyboardView : MonoBehaviour
    {
        public const int MaxLength = 80;

        private const float KeyW = 0.042f;
        private const float KeyH = 0.036f;
        private const float Gap = 0.004f;
        private static readonly float[] RowY = { 0.125f, 0.085f, 0.045f, 0.005f, -0.035f };

        public bool Open { get; private set; }
        public string Text { get { return _text; } }

        private string _text = "";
        private Action<string> _done;
        private Action _cancelled;

        public static KeyboardView Build(Transform dockRoot)
        {
            // Same lean as the lanes, in the space the lanes use, so it sits where the eyes already are.
            GameObject go = Make.Empty("Keyboard", dockRoot, new Vector3(0f, 0.05f, 0f), Quaternion.Euler(40f, 0f, 0f));
            KeyboardView k = go.AddComponent<KeyboardView>();
            k.BuildKeys();
            go.SetActive(false);
            return k;
        }

        private void BuildKeys()
        {
            Make.Box("Back", transform, new Vector3(0f, 0.045f, 0.006f), new Vector3(0.50f, 0.215f, 0.004f), Theme.Panel);

            string[] rows = { "1234567890", "qwertyuiop", "asdfghjkl", "zxcvbnm:.," };
            for (int r = 0; r < rows.Length; r++)
            {
                int count = r == 2 ? 10 : rows[r].Length;   // the third row ends with Del
                for (int i = 0; i < count; i++)
                {
                    float x = RowX(i, count);
                    if (r == 2 && i == 9)
                    {
                        Key("Del", x, RowY[r], KeyW, "Del", 7.5f, Backspace).SetLabelColor(Theme.Next);
                        continue;
                    }
                    string ch = rows[r][i].ToString();
                    Key("Key_" + ch, x, RowY[r], KeyW, ch, 10f, () => Type(ch));
                }
            }

            // Bottom row: Cancel | in 15 min | space | for 25 min | Add. The two middle shortcuts write
            // the phrases the board understands, so a due time or a sprint length is one tap.
            float[] widths = { 0.07f, 0.085f, 0.13f, 0.085f, 0.07f };
            float total = 0f;
            for (int i = 0; i < widths.Length; i++) total += widths[i];
            total += Gap * (widths.Length - 1);
            float left = -total * 0.5f;
            string[] labels = { "Cancel", "in 15 min", "space", "for 25 min", "Add" };
            Action[] presses = { Cancel, () => Phrase("in 15 min"), () => Type(" "), () => Phrase("for 25 min"), Submit };
            for (int i = 0; i < widths.Length; i++)
            {
                float x = left + widths[i] * 0.5f;
                DockButton b = Key("Bottom_" + i, x, RowY[4], widths[i], labels[i], 7f, presses[i]);
                if (i == 0) b.SetLabelColor(Theme.TextDim);
                if (i == 4) b.SetLabelColor(Theme.Done);
                if (i == 1 || i == 3) b.SetLabelColor(Theme.Next);
                left += widths[i] + Gap;
            }
        }

        private DockButton Key(string name, float x, float y, float width, string label, float font, Action press)
        {
            DockButton b = DockButton.Create(name, transform, new Vector3(x, y, 0f), new Vector2(width, KeyH), label, font, press);
            b.Debounce = 0.12f;   // a typed double letter must not be swallowed
            return b;
        }

        private static float RowX(int index, int count)
        {
            float total = count * KeyW + (count - 1) * Gap;
            return -total * 0.5f + KeyW * 0.5f + index * (KeyW + Gap);
        }

        // ---------- use ----------

        public void Show(Action<string> done, Action cancelled)
        {
            _text = "";
            _done = done;
            _cancelled = cancelled;
            Open = true;
            gameObject.SetActive(true);
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null) Keyboard.current.onTextInput += OnPhysicalChar;
#endif
        }

        /// <summary>Adds text as if typed. Also used by the automated test.</summary>
        public void Type(string s)
        {
            if (!Open || string.IsNullOrEmpty(s)) return;
            if (_text.Length + s.Length > MaxLength) s = s.Substring(0, Math.Max(0, MaxLength - _text.Length));
            _text += s;
        }

        private void Phrase(string phrase)
        {
            if (_text.Length > 0 && !_text.EndsWith(" ")) Type(" ");
            Type(phrase);
        }

        public void Backspace()
        {
            if (Open && _text.Length > 0) _text = _text.Substring(0, _text.Length - 1);
        }

        public void Submit()
        {
            string text = _text.Trim();
            if (text.Length == 0) return;   // nothing typed: stay open
            Action<string> done = _done;
            Close();
            if (done != null) done(text);
        }

        public void Cancel()
        {
            Action cancelled = _cancelled;
            Close();
            if (cancelled != null) cancelled();
        }

        private void Close()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null) Keyboard.current.onTextInput -= OnPhysicalChar;
#endif
            Open = false;
            _done = null;
            _cancelled = null;
            gameObject.SetActive(false);
        }

        private void OnPhysicalChar(char c)
        {
            if (c == '\n' || c == '\r') Submit();
            else if (c == '\b') Backspace();
            else if (!char.IsControl(c)) Type(c.ToString());
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Open && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Cancel();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null) Keyboard.current.onTextInput -= OnPhysicalChar;
#endif
        }
    }
}
