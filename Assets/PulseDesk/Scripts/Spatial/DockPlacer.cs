using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace PulseDesk
{
    /// <summary>
    /// Puts the dock on the desk. Tries each SurfaceFinder in order (room scan, live depth),
    /// then falls back to a fixed seated position, so it always ends with a dock in reach.
    /// </summary>
    public class DockPlacer : MonoBehaviour
    {
        public float minReach = 0.35f;
        public float maxReach = 0.60f;
        public float fallbackForward = 0.50f;
        public float fallbackDrop = 0.35f;

        /// <summary>Set by the MRUK finder when world lock is keeping content stable.</summary>
        public static bool WorldLockActive;

        public Transform Dock { get; private set; }
        public string LastMethod { get; private set; }
        public event Action<Transform> DockPlaced;
        public event Action<GameObject> AnchorCreated;

        private readonly List<SurfaceFinder> _finders = new List<SurfaceFinder>();
        private Transform _head;
        private GameObject _anchorRoot;
        private Coroutine _routine;

        public void AddFinder(SurfaceFinder finder)
        {
            if (finder != null) _finders.Add(finder);
        }

        public void Begin(Transform head)
        {
            _head = head;
            Recenter();
        }

        /// <summary>Re-runs placement from where the user is now. Used by "Move dock here".</summary>
        public void Recenter()
        {
            if (_head == null) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PlaceRoutine());
        }

        private IEnumerator PlaceRoutine()
        {
            // Head tracking can take a few frames to start; do not place at the floor origin.
            float waited = 0f;
            while (XRSettings.isDeviceActive && _head.localPosition.sqrMagnitude < 0.0001f && waited < 2f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;

            WaitForSeconds step = new WaitForSeconds(0.2f);
            for (int i = 0; i < _finders.Count; i++)
            {
                SurfaceFinder finder = _finders[i];
                if (finder == null || !finder.isActiveAndEnabled) continue;
                float t = 0f;
                while (t <= finder.Timeout)
                {
                    Vector3 point = default(Vector3);
                    bool found = false;
                    try
                    {
                        found = finder.TryFind(_head.position, ForwardFlat(), out point);
                    }
                    catch (Exception e)
                    {
                        // A finder must never be able to block placement.
                        Debug.LogWarning("PulseDesk: " + finder.GetType().Name + " failed: " + e.Message);
                        break;
                    }
                    if (found)
                    {
                        LastMethod = finder.GetType().Name;
                        Place(point);
                        yield break;
                    }
                    if (!finder.KeepTrying) break;
                    t += 0.2f;
                    yield return step;
                }
            }

            // Nothing found: fixed seated position. In the Editor without a headset the camera
            // looks straight ahead, so keep the dock higher to stay in view.
            float drop = XRSettings.isDeviceActive ? fallbackDrop : 0.16f;
            LastMethod = "Fixed position";
            Place(_head.position + ForwardFlat() * fallbackForward + Vector3.down * drop);
        }

        private Vector3 ForwardFlat()
        {
            Vector3 f = Vector3.ProjectOnPlane(_head.forward, Vector3.up);
            if (f.sqrMagnitude < 0.01f)
            {
                // Looking straight down: the head's "up" now points forward.
                f = Vector3.ProjectOnPlane(_head.up, Vector3.up);
            }
            if (f.sqrMagnitude < 0.0001f) f = Vector3.forward;
            return f.normalized;
        }

        private void Place(Vector3 surfacePoint)
        {
            Vector3 eye = _head.position;

            // Keep the dock inside seated reach, at the height we found.
            Vector3 flat = new Vector3(surfacePoint.x - eye.x, 0f, surfacePoint.z - eye.z);
            Vector3 flatDir = flat.sqrMagnitude > 0.0001f ? flat.normalized : ForwardFlat();
            float dist = Mathf.Clamp(flat.magnitude, minReach, maxReach);
            Vector3 pos = new Vector3(eye.x + flatDir.x * dist, surfacePoint.y + 0.005f, eye.z + flatDir.z * dist);
            Quaternion rot = Quaternion.LookRotation(flatDir, Vector3.up);   // dock +Z points away from the user

            if (Dock == null) Dock = new GameObject("DeskDock").transform;

            // A fresh anchor object each time: a spatial anchor owns its transform once created.
            Dock.SetParent(null, true);
            if (_anchorRoot != null) Destroy(_anchorRoot);
            _anchorRoot = new GameObject("DockAnchor");
            _anchorRoot.transform.SetPositionAndRotation(pos, rot);
            Dock.SetParent(_anchorRoot.transform, false);
            Dock.localPosition = Vector3.zero;
            Dock.localRotation = Quaternion.identity;

            if (AnchorCreated != null) AnchorCreated(_anchorRoot);
            Debug.Log("PulseDesk: dock placed by " + LastMethod + " at " + pos);
            if (DockPlaced != null) DockPlaced(Dock);
        }
    }
}
