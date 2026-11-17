// Compiled only when PULSEDESK_META is set. Needs the Meta XR Core SDK passthrough layer.
#if PULSEDESK_META
using UnityEngine;

namespace PulseDesk
{
    /// <summary>
    /// While a sprint runs, the real room fades toward grey and a little darker, so the desk and the
    /// task stand out. Pausing, running out of time or finishing brings the colour back. Uses the
    /// passthrough colour controls, so nothing is drawn over the room.
    /// </summary>
    public class FocusRoom : MonoBehaviour
    {
        private const float Saturation = -0.8f;
        private const float Brightness = -0.18f;
        private const float FadeSeconds = 1.6f;

        private TaskStore _store;
        private OVRPassthroughLayer _layer;
        private float _t;
        private float _applied = -1f;
        private float _nextFind;

        public void Init(TaskStore store)
        {
            _store = store;
        }

        private void Update()
        {
            if (_store == null) return;
            if (_layer == null)
            {
                if (Time.unscaledTime < _nextFind) return;
                _nextFind = Time.unscaledTime + 2f;
                _layer = FindAnyObjectByType<OVRPassthroughLayer>();
                if (_layer == null) return;
            }

            float target = _store.FocusActive ? 1f : 0f;
            _t = Mathf.MoveTowards(_t, target, Time.unscaledDeltaTime / FadeSeconds);
            if (Mathf.Abs(_t - _applied) < 0.01f && !(_t == 0f && _applied != 0f)) return;
            _applied = _t;

            float k = _t * _t * (3f - 2f * _t);   // smooth start and end
            try
            {
                if (k <= 0.001f) _layer.DisableColorMap();
                else _layer.SetBrightnessContrastSaturation(Brightness * k, 0f, Saturation * k);
            }
            catch (System.Exception e)
            {
                // A colour map is a nicety; never let it break the app.
                Debug.LogWarning("PulseDesk: passthrough colour change failed, turning it off. " + e.Message);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (_layer != null)
            {
                try { _layer.DisableColorMap(); } catch (System.Exception) { }
            }
        }
    }
}
#endif
