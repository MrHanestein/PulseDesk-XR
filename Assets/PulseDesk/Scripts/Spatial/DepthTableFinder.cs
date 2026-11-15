// Compiled only when PULSEDESK_DEPTH is set. Needs the Meta MR Utility Kit package (v81+)
// for EnvironmentRaycastManager. Works in rooms that were never scanned.
#if PULSEDESK_DEPTH
using Meta.XR;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace PulseDesk
{
    /// <summary>Finds a roughly horizontal surface below eye level using live depth data.</summary>
    public class DepthTableFinder : SurfaceFinder
    {
        private static readonly float[] SamplePitches = { 38f, 30f, 48f, 22f, 58f };
        private EnvironmentRaycastManager _raycaster;

        public override float Timeout { get { return 3.0f; } }
        public override bool KeepTrying { get { return EnvironmentRaycastManager.IsSupported; } }

        public override bool TryFind(Vector3 eye, Vector3 forwardFlat, out Vector3 point)
        {
            point = default(Vector3);
            if (!EnvironmentRaycastManager.IsSupported) return false;
            if (_raycaster == null) _raycaster = FindFirstObjectByType<EnvironmentRaycastManager>();
            if (_raycaster == null) _raycaster = gameObject.AddComponent<EnvironmentRaycastManager>();

            Vector3 right = Vector3.Cross(Vector3.up, forwardFlat);
            for (int i = 0; i < SamplePitches.Length; i++)
            {
                // Tilt down about the user's own right axis.
                Vector3 dir = Quaternion.AngleAxis(SamplePitches[i], right) * forwardFlat;
                EnvironmentRaycastHit hit;
                if (!_raycaster.Raycast(new Ray(eye, dir), out hit, 1.5f)) continue;
                if (hit.normal.y < 0.8f) continue;                    // not a horizontal surface
                float drop = eye.y - hit.point.y;
                if (drop < 0.15f || drop > 1.0f) continue;            // not plausibly a desk or lap tray
                point = hit.point;
                return true;
            }
            return false;
        }
    }
}
#endif
