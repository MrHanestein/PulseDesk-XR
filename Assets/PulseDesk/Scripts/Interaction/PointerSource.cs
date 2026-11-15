using UnityEngine;

namespace PulseDesk
{
    public struct PointerState
    {
        public bool valid;        // tracked this frame
        public bool pinching;     // index pinch (or mouse button)
        public bool pinchValid;   // pinchPosition is usable for near grabs
        public Vector3 pinchPosition;
        public bool rayValid;     // ray is usable for far targeting
        public Ray ray;
        public bool pokeValid;    // pokeTip is usable
        public Vector3 pokeTip;
    }

    /// <summary>One or more pointers (hands, or the mouse in the Editor).</summary>
    public abstract class PointerSource : MonoBehaviour
    {
        public abstract int Count { get; }
        public abstract PointerState Get(int index);
    }
}
