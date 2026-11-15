using UnityEngine;

namespace PulseDesk
{
    /// <summary>Anything a hand can hover, press or drag. Needs a collider on this object or a child.</summary>
    public abstract class PinchTarget : MonoBehaviour
    {
        private int _hoverCount;

        public bool Held { get; protected set; }
        public virtual bool Draggable { get { return false; } }

        public void AddHover()
        {
            _hoverCount++;
            if (_hoverCount == 1) OnHoverChanged(true);
        }

        public void RemoveHover()
        {
            _hoverCount = Mathf.Max(0, _hoverCount - 1);
            if (_hoverCount == 0) OnHoverChanged(false);
        }

        protected virtual void OnDisable()
        {
            if (_hoverCount > 0)
            {
                _hoverCount = 0;
                OnHoverChanged(false);
            }
        }

        protected virtual void OnHoverChanged(bool hovered) { }

        /// <summary>Pinch while hovering, or a fingertip poke.</summary>
        public virtual void OnPress() { }

        public virtual void OnGrab() { }
        public virtual void OnDrag(Vector3 worldPosition) { }
        public virtual void OnRelease() { }
    }
}
