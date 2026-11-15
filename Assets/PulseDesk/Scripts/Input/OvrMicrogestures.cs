// Compiled only when PULSEDESK_META and PULSEDESK_MICROGESTURES are set.
// Needs Meta XR Core SDK v74 or newer (thumb microgestures).
#if PULSEDESK_META && PULSEDESK_MICROGESTURES
using UnityEngine;

namespace PulseDesk
{
    /// <summary>
    /// Thumb microgestures with the hand at rest:
    /// swipe left = Urgent, right = Done, forward/backward = change focus, tap = pause.
    /// Polls OVRHand directly, so there are no event-source objects to wire.
    /// </summary>
    public class OvrMicrogestures : MonoBehaviour
    {
        private TaskStore _store;
        private AudioFeedback _sfx;
        private OVRHand[] _hands = new OVRHand[0];
        private float _nextScan;
        private float _lastGesture;

        public void Init(TaskStore store, AudioFeedback sfx)
        {
            _store = store;
            _sfx = sfx;
        }

        private void Update()
        {
            if (_store == null) return;
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 1f;
                _hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
            }
            if (Time.unscaledTime - _lastGesture < 0.25f) return;

            for (int i = 0; i < _hands.Length; i++)
            {
                OVRHand hand = _hands[i];
                if (hand == null || !hand.isActiveAndEnabled || !hand.IsTracked) continue;
                if (Handle(hand.GetMicrogestureType()))
                {
                    _lastGesture = Time.unscaledTime;
                    return;
                }
            }
        }

        private bool Handle(OVRHand.MicrogestureType gesture)
        {
            switch (gesture)
            {
                case OVRHand.MicrogestureType.SwipeLeft: _store.MoveFocused(TaskLane.Urgent); break;
                case OVRHand.MicrogestureType.SwipeRight: _store.MoveFocused(TaskLane.Done); break;
                case OVRHand.MicrogestureType.SwipeForward: _store.CycleFocus(-1); break;
                case OVRHand.MicrogestureType.SwipeBackward: _store.CycleFocus(1); break;
                case OVRHand.MicrogestureType.ThumbTap: _store.ToggleUserPause(); break;
                default: return false;
            }
            if (_sfx != null) _sfx.Tick();
            return true;
        }
    }
}
#endif
