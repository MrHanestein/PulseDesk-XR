// Compiled only when PULSEDESK_META is set. Needs the Meta XR Core SDK.
#if PULSEDESK_META
using UnityEngine;

namespace PulseDesk
{
    /// <summary>Pauses the sprint when the system menu opens or the headset comes off, and anchors the dock.</summary>
    public class OvrSystemHooks : MonoBehaviour
    {
        private TaskStore _store;

        public void Init(TaskStore store, DockPlacer placer)
        {
            _store = store;
            placer.AnchorCreated += OnAnchorCreated;
        }

        private void OnEnable()
        {
            OVRManager.InputFocusLost += OnLost;
            OVRManager.InputFocusAcquired += OnBack;
            OVRManager.HMDUnmounted += OnLost;
            OVRManager.HMDMounted += OnBack;
        }

        private void OnDisable()
        {
            OVRManager.InputFocusLost -= OnLost;
            OVRManager.InputFocusAcquired -= OnBack;
            OVRManager.HMDUnmounted -= OnLost;
            OVRManager.HMDMounted -= OnBack;
        }

        // Coming back never overrides a pause the user chose: the store keeps the two flags apart.
        private void OnLost() { if (_store != null) _store.SetSystemPaused(true); }
        private void OnBack() { if (_store != null) _store.SetSystemPaused(false); }

        private void OnAnchorCreated(GameObject anchorRoot)
        {
            // If MRUK world lock is already keeping the room stable, a second anchor would fight it.
            if (DockPlacer.WorldLockActive) return;
            if (Application.isEditor) return;
            anchorRoot.AddComponent<OVRSpatialAnchor>();
        }
    }
}
#endif
