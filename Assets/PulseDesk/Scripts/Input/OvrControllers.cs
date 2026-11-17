// Compiled only when PULSEDESK_META is set. Needs the Meta XR Core SDK.
#if PULSEDESK_META
using UnityEngine;

namespace PulseDesk
{
    /// <summary>
    /// Touch controllers, for people who prefer them. Each controller is a pointer: the ray comes out
    /// of the controller and the trigger is the pinch, so every card and button works the same way.
    /// Buttons: A finishes the sprint (or starts the next card), B switches compact mode,
    /// X types a task, Y pauses. A short buzz confirms each press. Hands stay the primary input.
    /// </summary>
    public class OvrControllers : PointerSource
    {
        private OVRCameraRig _rig;
        private float _nextFind;
        private readonly bool[] _wasPressed = new bool[2];
        private readonly float[] _buzzUntil = new float[2];

        public override int Count { get { return 2; } }   // 0 = left, 1 = right

        private static OVRInput.Controller Which(int index)
        {
            return index == 0 ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        }

        public override PointerState Get(int index)
        {
            PointerState s = new PointerState();
            OVRInput.Controller c = Which(index);
            if (!OVRInput.IsControllerConnected(c)) return s;
            // Controllers put down while hands are in use must not hold a ray of their own.
            if ((OVRInput.GetActiveController() & OVRInput.Controller.Touch) == 0) return s;
            Transform anchor = Anchor(index);
            if (anchor == null) return s;

            bool pressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, c);
            if (pressed && !_wasPressed[index]) Buzz(index);
            _wasPressed[index] = pressed;

            s.valid = true;
            s.pinching = pressed;
            s.rayValid = true;
            s.ray = new Ray(anchor.position, anchor.forward);
            // The tip of the controller grabs and pokes up close, like a fingertip.
            Vector3 tip = anchor.position + anchor.forward * 0.04f;
            s.pinchValid = true;
            s.pinchPosition = tip;
            s.pokeValid = true;
            s.pokeTip = tip;
            return s;
        }

        private Transform Anchor(int index)
        {
            if (_rig == null && Time.unscaledTime >= _nextFind)
            {
                _nextFind = Time.unscaledTime + 1f;
                _rig = FindAnyObjectByType<OVRCameraRig>();
            }
            if (_rig == null) return null;
            Transform t = index == 0 ? _rig.leftControllerAnchor : _rig.rightControllerAnchor;
            if (t == null) t = index == 0 ? _rig.leftHandAnchor : _rig.rightHandAnchor;
            return t;
        }

        private void Buzz(int index)
        {
            OVRInput.SetControllerVibration(1f, 0.25f, Which(index));
            _buzzUntil[index] = Time.unscaledTime + 0.05f;
        }

        private void Update()
        {
            for (int i = 0; i < 2; i++)
            {
                if (_buzzUntil[i] > 0f && Time.unscaledTime > _buzzUntil[i])
                {
                    _buzzUntil[i] = 0f;
                    OVRInput.SetControllerVibration(0f, 0f, Which(i));
                }
            }

            PulseDeskApp app = PulseDeskApp.Instance;
            if (app == null || app.Store == null) return;
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                Buzz(1);
                if (app.Store.ActiveSprint() != null) app.Store.CompleteSprint();
                else app.Store.StartNext();
            }
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch)) { Buzz(1); app.ToggleCompact(); }
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch)) { Buzz(0); app.NewTask(); }
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch)) { Buzz(0); app.Store.ToggleUserPause(); }
        }
    }
}
#endif
