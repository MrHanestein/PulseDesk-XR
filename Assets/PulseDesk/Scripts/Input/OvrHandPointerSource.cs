// Compiled only when the scripting define PULSEDESK_META is set
// (menu: PulseDesk > 2. Detect Meta SDK features). Needs the Meta XR Core SDK.
#if PULSEDESK_META
using System.Collections.Generic;
using UnityEngine;

namespace PulseDesk
{
    /// <summary>
    /// Reads Meta hand tracking: index pinch, the system pointer pose for far rays,
    /// and fingertip bones for near grabs and pokes. Finds the hands in the scene by itself.
    /// </summary>
    public class OvrHandPointerSource : PointerSource
    {
        private class Hand
        {
            public OVRHand hand;
            public OVRSkeleton skeleton;
            public Transform indexTip;
            public Transform thumbTip;
            public bool left;
        }

        private readonly List<Hand> _hands = new List<Hand>();
        private Transform _trackingSpace;
        private float _nextScan;

        public override int Count { get { return 2; } }   // 0 = left, 1 = right

        private void Update()
        {
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + 1f;
            Scan();
        }

        private void Scan()
        {
            OVRHand[] all = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (Known(all[i])) continue;
                Hand h = new Hand();
                h.hand = all[i];
                h.skeleton = all[i].GetComponent<OVRSkeleton>();
                // OVRHand reports which hand it is through its skeleton data provider interface.
                string side = ((OVRSkeleton.IOVRSkeletonDataProvider)all[i]).GetSkeletonType().ToString();
                h.left = side.ToLowerInvariant().Contains("left");
                _hands.Add(h);
            }
            if (_trackingSpace == null)
            {
                OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
                if (rig != null) _trackingSpace = rig.trackingSpace;
            }
        }

        private bool Known(OVRHand hand)
        {
            for (int i = 0; i < _hands.Count; i++)
                if (_hands[i].hand == hand) return true;
            return false;
        }

        public override PointerState Get(int index)
        {
            PointerState s = new PointerState();
            bool wantLeft = index == 0;

            // A scene can hold more than one OVRHand per side; use the first one that is tracked.
            Hand h = null;
            for (int i = 0; i < _hands.Count; i++)
            {
                Hand c = _hands[i];
                if (c.hand == null || c.left != wantLeft) continue;
                if (!c.hand.isActiveAndEnabled || !c.hand.IsTracked) continue;
                h = c;
                break;
            }
            if (h == null) return s;

            s.valid = true;
            s.pinching = h.hand.GetFingerIsPinching(OVRHand.HandFinger.Index);

            CacheTips(h);
            if (h.indexTip != null && h.thumbTip != null)
            {
                s.pinchValid = true;
                s.pinchPosition = (h.indexTip.position + h.thumbTip.position) * 0.5f;
                s.pokeValid = true;
                s.pokeTip = h.indexTip.position;
            }

            if (h.hand.IsPointerPoseValid)
            {
                Transform pose = h.hand.PointerPose;
                Vector3 origin;
                Vector3 direction;
                if (pose.parent == null && _trackingSpace != null)
                {
                    // Pointer pose is expressed in tracking space when it has no parent.
                    origin = _trackingSpace.TransformPoint(pose.localPosition);
                    direction = _trackingSpace.rotation * (pose.localRotation * Vector3.forward);
                }
                else
                {
                    origin = pose.position;
                    direction = pose.forward;
                }
                s.rayValid = true;
                s.ray = new Ray(origin, direction);
            }
            return s;
        }

        private static void CacheTips(Hand h)
        {
            if (h.indexTip != null || h.skeleton == null || !h.skeleton.IsInitialized) return;
            IList<OVRBone> bones = h.skeleton.Bones;
            if (bones == null) return;
            for (int i = 0; i < bones.Count; i++)
            {
                // Matches both the legacy (Hand_IndexTip) and OpenXR (XRHand_IndexTip) skeletons.
                string id = bones[i].Id.ToString();
                if (id.EndsWith("IndexTip")) h.indexTip = bones[i].Transform;
                else if (id.EndsWith("ThumbTip")) h.thumbTip = bones[i].Transform;
            }
        }
    }
}
#endif
