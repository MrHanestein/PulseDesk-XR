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
            // Smoothed ray and pinch state, so the ray does not shake and a pinch does not flicker.
            public bool rayInit;
            public Vector3 rayOrigin;
            public Vector3 rayDir;
            public bool pinching;
            public int frame = -1;
            public PointerState cached;
        }

        // Pinch starts when the SDK reports a pinch and only ends when the fingers clearly open.
        private const float PinchHold = 0.55f;
        // Ray smoothing: fast while aiming, much slower while the fingers close, so pinching
        // does not drag the ray off the key you were aiming at.
        private const float FollowAiming = 22f;
        private const float FollowPinching = 6f;

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
            if (h == null)
            {
                for (int i = 0; i < _hands.Count; i++)
                    if (_hands[i].left == wantLeft) { _hands[i].rayInit = false; _hands[i].pinching = false; }
                return s;
            }
            if (h.frame == Time.frameCount) return h.cached;   // one update per frame
            h.frame = Time.frameCount;

            s.valid = true;
            // The palm-up pinch that opens the system menu is not a pinch for the app.
            float strength = h.hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
            if (h.hand.IsSystemGestureInProgress) h.pinching = false;
            else if (!h.pinching) h.pinching = h.hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            else h.pinching = strength >= PinchHold || h.hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            s.pinching = h.pinching;

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
                if (!h.rayInit)
                {
                    h.rayInit = true;
                    h.rayOrigin = origin;
                    h.rayDir = direction.normalized;
                }
                else
                {
                    float follow = (h.pinching || strength > 0.25f) ? FollowPinching : FollowAiming;
                    float t = 1f - Mathf.Exp(-follow * Time.unscaledDeltaTime);
                    h.rayOrigin = Vector3.Lerp(h.rayOrigin, origin, t);
                    h.rayDir = Vector3.Slerp(h.rayDir, direction.normalized, t).normalized;
                }
                s.rayValid = true;
                s.ray = new Ray(h.rayOrigin, h.rayDir);
            }
            else
            {
                h.rayInit = false;
            }
            h.cached = s;
            return s;
        }

        private static void CacheTips(Hand h)
        {
            if (h.indexTip != null || h.skeleton == null || !h.skeleton.IsInitialized) return;
            IList<OVRBone> bones = h.skeleton.Bones;
            if (bones == null) return;
            // Compare bone ids, never their names: the enum reuses the same numbers for the legacy hand,
            // the OpenXR hand and the body skeletons, so ToString() can return the wrong name.
            OVRSkeleton.SkeletonType type = h.skeleton.GetSkeletonType();
            bool xr = type == OVRSkeleton.SkeletonType.XRHandLeft || type == OVRSkeleton.SkeletonType.XRHandRight;
            OVRSkeleton.BoneId indexId = xr ? OVRSkeleton.BoneId.XRHand_IndexTip : OVRSkeleton.BoneId.Hand_IndexTip;
            OVRSkeleton.BoneId thumbId = xr ? OVRSkeleton.BoneId.XRHand_ThumbTip : OVRSkeleton.BoneId.Hand_ThumbTip;
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].Id == indexId) h.indexTip = bones[i].Transform;
                else if (bones[i].Id == thumbId) h.thumbTip = bones[i].Transform;
            }
        }
    }
}
#endif
