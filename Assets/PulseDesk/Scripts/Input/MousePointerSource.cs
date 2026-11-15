using UnityEngine;

namespace PulseDesk
{
    /// <summary>Editor-only pointer: the mouse is a far ray, left button is the pinch.</summary>
    public class MousePointerSource : PointerSource
    {
        private Camera _camera;

        public override int Count { get { return 1; } }

        public override PointerState Get(int index)
        {
            PointerState s = new PointerState();
            if (!Application.isEditor || !DeskInput.HasMouse) return s;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return s;

            Vector2 m = DeskInput.MousePosition;
            if (m.x < 0f || m.y < 0f || m.x > Screen.width || m.y > Screen.height) return s;

            s.valid = true;
            s.pinching = DeskInput.MouseHeld;
            s.rayValid = true;
            s.ray = _camera.ScreenPointToRay(new Vector3(m.x, m.y, 0f));
            return s;
        }
    }
}
