// Compiled only when PULSEDESK_MRUK is set. Needs the Meta MR Utility Kit package
// and an MR Utility Kit building block in the scene.
#if PULSEDESK_MRUK
using System;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace PulseDesk
{
    /// <summary>
    /// Finds a TABLE surface from the user's room scan, if they have one.
    /// The scan is loaded here, not by MRUK on startup, so that a user with no scan is never
    /// pushed into Space Setup and never gets a sample room standing in for their own.
    /// </summary>
    public class MrukTableFinder : SurfaceFinder
    {
        private static readonly float[] SampleDistances = { 0.50f, 0.45f, 0.55f, 0.40f, 0.60f, 0.35f };
        private bool _sceneLoaded;
        private bool _loadFinished;
        private bool _started;

        public override float Timeout { get { return 4f; } }

        // Keep waiting only while a room might still load.
        public override bool KeepTrying { get { return MRUK.Instance != null && !_sceneLoaded && !_loadFinished; } }

        public override bool TryFind(Vector3 eye, Vector3 forwardFlat, out Vector3 point)
        {
            point = default(Vector3);
            if (MRUK.Instance == null) return false;
            if (!_started)
            {
                _started = true;
                MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
                if (!Application.isEditor) LoadRoom();
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

        private async void LoadRoom()
        {
            try
            {
                // false: use a scan if there is one, never start room capture.
                await MRUK.Instance.LoadSceneFromDevice(requestSceneCaptureIfNoDataFound: false);
            }
            catch (Exception e)
            {
                Debug.LogWarning("PulseDesk: room scan could not be loaded. " + e.Message);
            }
            _loadFinished = true;
        }

        private void OnSceneLoaded()
        {
            _sceneLoaded = true;
            // The scan can arrive after the dock already fell back to the fixed position
            // (for example while the permission dialog was open). Move it onto the desk now.
            if (PulseDeskApp.Instance != null) PulseDeskApp.Instance.RecenterIfFallback();
        }
    }
}
#endif
