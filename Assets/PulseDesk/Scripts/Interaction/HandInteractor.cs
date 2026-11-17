using System.Collections.Generic;
using UnityEngine;

namespace PulseDesk
{
    /// <summary>
    /// Self-contained hand interaction: near pinch-grab, far ray pinch, fingertip poke.
    /// It only needs PointerSource data, so it runs with real hands, the simulator or a mouse.
    /// </summary>
    public class HandInteractor : MonoBehaviour
    {
        private const float NearRadius = 0.03f;
        private const float PokeRadius = 0.008f;
        private const float PokeRelease = 0.02f;   // the tip must move this far away before the same key fires again
        private const float RayRadius = 0.012f;
        private const float RayLength = 1.2f;
        private const float IdleRayLength = 0.3f;  // ray shown when it points at nothing, so it never blinks out
        private const float HoverGrace = 0.15f;    // keep a hover through a short tracking wobble
        private const float AimMemory = 0.1f;    // a pinch counts for what was aimed at just before it

        private class Pointer
        {
            public PinchTarget hover;
            public float hoverSeen;
            public PinchTarget previousHover;
            public float hoverChanged;
            public PinchTarget grabbed;
            public PinchTarget poked;
            public bool wasPinching;
            public bool near;
            public float distance;
            public Vector3 offset;
            public LineRenderer line;
        }

        private readonly List<PointerSource> _sources = new List<PointerSource>();
        private readonly List<Pointer> _pointers = new List<Pointer>();
        private static readonly Collider[] Overlaps = new Collider[24];
        private static readonly RaycastHit[] Hits = new RaycastHit[24];

        public void AddSource(PointerSource source)
        {
            if (source != null) _sources.Add(source);
        }

        private void Update()
        {
            int k = 0;
            for (int s = 0; s < _sources.Count; s++)
            {
                PointerSource source = _sources[s];
                if (source == null || !source.isActiveAndEnabled) continue;
                int n = source.Count;
                for (int i = 0; i < n; i++)
                {
                    Step(GetPointer(k), source.Get(i));
                    k++;
                }
            }
        }

        private Pointer GetPointer(int index)
        {
            while (_pointers.Count <= index)
            {
                Pointer p = new Pointer();
                GameObject go = new GameObject("PointerRay");
                go.transform.SetParent(transform, false);
                p.line = go.AddComponent<LineRenderer>();
                p.line.useWorldSpace = true;
                p.line.positionCount = 2;
                p.line.startWidth = 0.003f;
                p.line.endWidth = 0.0015f;
                p.line.sharedMaterial = Make.NewMaterial(Theme.TextMain);
                p.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                p.line.enabled = false;
                _pointers.Add(p);
            }
            return _pointers[index];
        }

        private void Step(Pointer p, PointerState s)
        {
            if (!s.valid)
            {
                if (p.grabbed != null) { p.grabbed.OnRelease(); p.grabbed = null; }
                SetHover(p, null);
                p.poked = null;
                p.wasPinching = false;
                p.line.enabled = false;
                return;
            }

            bool pinchDown = s.pinching && !p.wasPinching;
            p.wasPinching = s.pinching;
            Vector3 rayEnd = Vector3.zero;
            bool showRay = false;

            if (p.grabbed != null)
            {
                bool lost = p.near ? !s.pinchValid : !s.rayValid;
                if (s.pinching && !lost && p.grabbed.isActiveAndEnabled)
                {
                    Vector3 anchor = p.near ? s.pinchPosition : s.ray.GetPoint(p.distance);
                    p.grabbed.OnDrag(anchor + p.offset);
                    if (!p.near) { showRay = true; rayEnd = anchor; }
                }
                else
                {
                    p.grabbed.OnRelease();
                    p.grabbed = null;
                }
            }
            else
            {
                PinchTarget target = null;
                bool near = false;
                float distance = 0f;
                Vector3 point = Vector3.zero;

                if (s.pinchValid)
                {
                    target = FindNear(s.pinchPosition, NearRadius);
                    near = target != null;
                    point = s.pinchPosition;
                }
                if (target == null && s.rayValid)
                {
                    target = FindFar(s.ray, out distance, out point);
                    showRay = true;
                    if (target != null)
                    {
                        rayEnd = point;
                        p.distance = distance;
                        p.hoverSeen = Time.unscaledTime;
                    }
                    else if (p.hover != null && p.hover.isActiveAndEnabled && p.distance > 0f
                             && Time.unscaledTime - p.hoverSeen < HoverGrace)
                    {
                        // A frame or two of tracking wobble must not drop the key under the ray.
                        target = p.hover;
                        distance = p.distance;
                        point = s.ray.GetPoint(distance);
                        rayEnd = point;
                    }
                    else
                    {
                        rayEnd = s.ray.GetPoint(IdleRayLength);
                    }
                }
                else if (near)
                {
                    p.distance = 0f;
                    p.hoverSeen = Time.unscaledTime;
                }

                SetHover(p, target);

                // Closing the fingers moves the hand a little. If the ray slid onto another key in the
                // last moment, press the one that was aimed at.
                if (pinchDown && !near && p.previousHover != null && p.previousHover.isActiveAndEnabled
                    && Time.unscaledTime - p.hoverChanged < AimMemory)
                {
                    target = p.previousHover;
                    SetHover(p, target);
                }

                if (pinchDown && target != null && !target.Held)
                {
                    if (target.Draggable)
                    {
                        p.grabbed = target;
                        p.near = near;
                        p.distance = distance;
                        p.offset = target.transform.position - point;
                        SetHover(p, null);
                        target.OnGrab();
                    }
                    else
                    {
                        target.OnPress();
                    }
                }

                // Fingertip poke: fires once when the tip enters a non-draggable target.
                if (s.pokeValid)
                {
                    // Fires when the tip touches a key, and is ready again only after the tip has
                    // clearly left it, so a finger resting on a key does not type it twice.
                    if (p.poked != null && FindNear(s.pokeTip, PokeRelease) != p.poked) p.poked = null;
                    if (p.poked == null)
                    {
                        PinchTarget poke = FindNear(s.pokeTip, PokeRadius);
                        if (poke != null)
                        {
                            p.poked = poke;
                            if (!poke.Draggable) poke.OnPress();
                        }
                    }
                }
                else
                {
                    p.poked = null;
                }
            }

            p.line.enabled = showRay;
            if (showRay)
            {
                p.line.SetPosition(0, s.ray.origin);
                p.line.SetPosition(1, rayEnd);
            }
        }

        private static void SetHover(Pointer p, PinchTarget target)
        {
            if (p.hover == target) return;
            if (p.hover != null) p.hover.RemoveHover();
            if (p.hover != null)
            {
                p.previousHover = p.hover;
                p.hoverChanged = Time.unscaledTime;
            }
            p.hover = target;
            if (target != null) target.AddHover();
        }

        private static PinchTarget FindNear(Vector3 position, float radius)
        {
            int n = Physics.OverlapSphereNonAlloc(position, radius, Overlaps, ~0, QueryTriggerInteraction.Collide);
            PinchTarget best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                PinchTarget t = Overlaps[i].GetComponentInParent<PinchTarget>();
                if (t == null || !t.isActiveAndEnabled) continue;
                float sqr = (Overlaps[i].ClosestPoint(position) - position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = t; }
            }
            return best;
        }

        private static PinchTarget FindFar(Ray ray, out float distance, out Vector3 point)
        {
            int n = Physics.SphereCastNonAlloc(ray, RayRadius, Hits, RayLength, ~0, QueryTriggerInteraction.Collide);
            PinchTarget best = null;
            distance = float.MaxValue;
            point = ray.origin;
            for (int i = 0; i < n; i++)
            {
                PinchTarget t = Hits[i].collider.GetComponentInParent<PinchTarget>();
                if (t == null || !t.isActiveAndEnabled) continue;
                if (Hits[i].distance < distance)
                {
                    distance = Hits[i].distance;
                    best = t;
                }
            }
            if (best == null) distance = 0f;
            else point = ray.GetPoint(distance);
            return best;
        }
    }
}
