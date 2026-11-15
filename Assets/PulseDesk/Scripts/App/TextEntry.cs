using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PulseDesk
{
    /// <summary>
    /// One line of typed text. On the headset this opens the system keyboard, which works with
    /// hands and needs no network. In the Editor and the simulator it reads the PC keyboard
    /// (Enter to finish, Esc to cancel).
    /// </summary>
    public class TextEntry : MonoBehaviour
    {
        public const int MaxLength = 80;

        public bool Active { get; private set; }

        private TouchScreenKeyboard _keyboard;
        private Action<string> _preview;
        private Action<string> _done;
        private Action _cancelled;
        private string _typed = "";
        private bool _pcMode;
        private float _nextPreview;

        /// <summary>Returns false when there is no way to type here; the caller should offer presets.</summary>
        public bool Begin(string hint, Action<string> preview, Action<string> done, Action cancelled)
        {
            if (Active) return true;
            _preview = preview;
            _done = done;
            _cancelled = cancelled;
            _typed = "";

            if (TouchScreenKeyboard.isSupported)
            {
                _keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, true, false, false, false, hint, MaxLength);
                if (_keyboard != null)
                {
                    Active = true;
                    _pcMode = false;
                    return true;
                }
            }

#if ENABLE_INPUT_SYSTEM && (UNITY_EDITOR || UNITY_STANDALONE)
            if (Keyboard.current != null)
            {
                Keyboard.current.onTextInput += OnChar;
                Active = true;
                _pcMode = true;
                _nextPreview = 0f;
                return true;
            }
#endif
            return false;
        }

        private void Update()
        {
            if (!Active) return;

            if (!_pcMode)
            {
                if (_keyboard == null) { Finish(false); return; }
                TouchScreenKeyboard.Status status = _keyboard.status;
                if (status == TouchScreenKeyboard.Status.Visible) return;
                _typed = _keyboard.text ?? "";
                Finish(status == TouchScreenKeyboard.Status.Done);
                return;
            }

#if ENABLE_INPUT_SYSTEM && (UNITY_EDITOR || UNITY_STANDALONE)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Finish(false); return; }
#endif
            // Keep the preview line alive while the user thinks.
            if (Time.unscaledTime >= _nextPreview) ShowPreview();
        }

        private void OnChar(char c)
        {
            if (!Active || !_pcMode) return;
            if (c == '\n' || c == '\r') { Finish(true); return; }
            if (c == '\b')
            {
                if (_typed.Length > 0) _typed = _typed.Substring(0, _typed.Length - 1);
            }
            else if (!char.IsControl(c) && _typed.Length < MaxLength)
            {
                _typed += c;
            }
            ShowPreview();
        }

        private void ShowPreview()
        {
            _nextPreview = Time.unscaledTime + 2f;
            if (_preview != null) _preview(_typed);
        }

        private void Finish(bool accepted)
        {
#if ENABLE_INPUT_SYSTEM && (UNITY_EDITOR || UNITY_STANDALONE)
            if (_pcMode && Keyboard.current != null) Keyboard.current.onTextInput -= OnChar;
#endif
            Active = false;
            _keyboard = null;
            string text = (_typed ?? "").Trim();
            Action<string> done = _done;
            Action cancelled = _cancelled;
            _done = null;
            _cancelled = null;
            _preview = null;
            if (accepted && text.Length > 0) { if (done != null) done(text); }
            else if (cancelled != null) cancelled();
        }

        private void OnDisable()
        {
            if (Active) Finish(false);
        }
    }
}
