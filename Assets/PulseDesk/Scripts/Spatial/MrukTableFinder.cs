// Compiled only when PULSEDESK_MRUK is set. Needs the Meta MR Utility Kit package
// and an MR Utility Kit building block in the scene.
#if PULSEDESK_MRUK
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace PulseDesk
{
    /// <summary>Finds a TABLE surface from the user's room scan, if they have one.</summary>
    public class MrukTableFinder : SurfaceFinder
    {
        private static readonly float[] SampleDistances = { 0.50f, 0.45f, 0.55f, 0.40f, 0.60f, 0.35f };
        private bool _sceneLoaded;
        private bool _registered;

        public override float Timeout { get { return 2.5f; } }

        // Keep waiting only while a room might still load.
        public override bool KeepTrying { get { return MRUK.Instance != null && !_sceneLoaded; } }

        public override bool TryFind(Vector3 eye, Vector3 forwardFlat, out Vector3 point)
        {
            point = default(Vector3);
            if (MRUK.Instance == null) return false;
            if (!_registered)
            {
                _registered = true;
                MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
            }
            DockPlacer.WorldLockActive = MRUK.Instance.IsWorldLockActive;
            if (!_sceneLoaded) return false;

            MRUKRoom room = MRUK.Instance.GetCurrentRoom();
            if (room == null) return false;

            for (int i = 0; i < SampleDistances.Length; i++)
            {
                Vector3 origin = eye + forwardFlat * SampleDistances[i];
                RaycastHit hit;
                MRUKAnchor anchor;
                if (!room.Raycast(new Ray(origin, Vector3.down), 1.2f, out hit, out anchor)) continue;
                if (anchor == null || !anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE)) continue;
                if (hit.normal.y < 0.9f || hit.distance < 0.15f) continue;
                point = hit.point;
                return true;
            }
            return false;
        }

        private void OnSceneLoaded()
        {
            _sceneLoaded = true;
        }
    }
}
#endif
