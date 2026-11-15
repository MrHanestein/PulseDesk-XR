using UnityEngine;

namespace PulseDesk
{
    /// <summary>One way of finding a desk surface in front of the user. Tried in order by DockPlacer.</summary>
    public abstract class SurfaceFinder : MonoBehaviour
    {
        /// <summary>Longest time DockPlacer keeps asking this finder.</summary>
        public abstract float Timeout { get; }

        /// <summary>False once there is no point asking again (for example the room has loaded and has no table).</summary>
        public abstract bool KeepTrying { get; }

        public abstract bool TryFind(Vector3 eye, Vector3 forwardFlat, out Vector3 point);
    }
}
