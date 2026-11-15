using TMPro;
using UnityEngine;

namespace PulseDesk
{
    /// <summary>A task card you can pick up. Glides to its lane slot whenever it is not held.</summary>
    public class TaskCardView : PinchTarget
    {
        public const float Width = 0.14f;
        public const float Height = 0.08f;
        public const float Thickness = 0.005f;

        public string TaskId { get; private set; }

        private TaskItem _item;
        private TaskStore _store;
        private CardManager _manager;
        private Renderer _body;
        private Renderer _strip;
        private TextMeshPro _title;
        private TextMeshPro _step;
        private TextMeshPro _laneLabel;
        private TextMeshPro _timer;
        private Transform _contents;
        private Transform _slot;
        private Vector3 _offset;
        private bool _hovered;
        private bool _focused;
        private int _shownSeconds = -1;
        private bool _shownOverdue;
        private TaskLane _shownLane = (TaskLane)(-1);

        public override bool Draggable { get { return true; } }

        public static TaskCardView Create(TaskItem item, TaskStore store, CardManager manager, Transform parent)
        {
            GameObject go = Make.Empty("Card_" + item.id, parent, Vector3.zero, Quaternion.identity);
            TaskCardView v = go.AddComponent<TaskCardView>();
            v._item = item;
            v._store = store;
            v._manager = manager;
            v.TaskId = item.id;

            BoxCollider col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(Width, Height, 0.012f);
            col.isTrigger = true;

            float z = -(Thickness * 0.5f + 0.0006f);
            v._contents = Make.Empty("Contents", go.transform, Vector3.zero, Quaternion.identity).transform;
            v._body = Make.Box("Body", v._contents, Vector3.zero, new Vector3(Width, Height, Thickness), Theme.CardBody);
            // A slim accent bar set inside the rounded edge.
            v._strip = Make.Box("Strip", v._contents, new Vector3(-Width * 0.5f + 0.0065f, 0f, -(Thickness * 0.5f + 0.0004f)),
                new Vector3(0.004f, Height - 0.022f, 0.0008f), Theme.Next);

            v._title = Make.Text("Title", v._contents, new Vector3(0.006f, 0.021f, z), new Vector2(0.118f, 0.032f),
                8.5f, Theme.TextMain, TextAlignmentOptions.TopLeft);
            // One line only: stacked cards show just their title strip, so a wrapped title would be cut in half.
#if UNITY_6000_0_OR_NEWER
            v._title.textWrappingMode = TextWrappingModes.NoWrap;
#else
            v._title.enableWordWrapping = false;
#endif
            v._title.overflowMode = TextOverflowModes.Truncate;
            v._title.richText = true;   // the due time is coloured inside the title
            v._step = Make.Text("Step", v._contents, new Vector3(0.006f, -0.010f, z), new Vector2(0.118f, 0.026f),
                7f, Theme.TextDim, TextAlignmentOptions.TopLeft);
            v._laneLabel = Make.Text("Lane", v._contents, new Vector3(-0.030f, -0.031f, z), new Vector2(0.046f, 0.012f),
                6.5f, Theme.Next, TextAlignmentOptions.Left);
            v._timer = Make.Text("Timer", v._contents, new Vector3(0.030f, -0.031f, z), new Vector2(0.070f, 0.012f),
                6.5f, Theme.TextDim, TextAlignmentOptions.Right);

            v.Refresh();
            return v;
        }

        /// <summary>Re-reads title, lane colours and timer from the task.</summary>
        public void Refresh()
        {
            // The due time leads the title, so it is still visible when the card is stacked behind others.
            if (_item.dueTicks > 0 && _item.lane != TaskLane.Done && _store != null)
            {
                bool late = _store.IsOverdue(_item);
                string when = QuickAdd.FormatDue(new System.DateTime(_item.dueTicks), _store.Clock());
                _title.text = "<color=" + (late ? "#F2383F" : "#FCC934") + ">" + (late ? "late " : "") + when + "</color>  " + _item.title;
            }
            else
            {
                _title.text = _item.title;
            }
            _step.text = _item.microStep ?? "";
            if (_shownLane != _item.lane)
            {
                _shownLane = _item.lane;
                Color accent = Theme.Accent(_item.lane);
                Make.SetColor(_strip.sharedMaterial, accent);
                _laneLabel.color = accent;
                _laneLabel.text = Theme.Label(_item.lane);
            }
            _shownSeconds = -1;
            ApplyLook();
        }

        public void SetFocused(bool focused)
        {
            if (_focused == focused) return;
            _focused = focused;
            ApplyLook();
        }

        public void GoTo(Transform slot, Vector3 localOffset)
        {
            _slot = slot;
            _offset = localOffset;
        }

        public void SnapToSlot()
        {
            if (_slot == null) return;
            transform.position = _slot.TransformPoint(_offset);
            transform.rotation = _slot.rotation;
        }

        protected override void OnHoverChanged(bool hovered)
        {
            _hovered = hovered;
            if (hovered && _store != null) _store.SetFocus(TaskId);
            ApplyLook();
        }

        public override void OnGrab()
        {
            Held = true;
            ApplyLook();
        }

        public override void OnDrag(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        public override void OnRelease()
        {
            Held = false;
            ApplyLook();
            if (_manager != null) _manager.HandleDrop(this);
        }

        private void ApplyLook()
        {
            if (_body == null || _contents == null) return;
            // Hover and focus brighten the fill. No outlines, nothing reflows.
            bool bright = _hovered || Held || _focused;
            Make.SetColor(_body.sharedMaterial, bright ? Theme.CardHover : Theme.CardBody);
            float s = Held ? 1.06f : (_focused ? 1.03f : 1f);
            _contents.localScale = new Vector3(s, s, 1f);
        }

        private void Update()
        {
            if (!Held && _slot != null)
            {
                float k = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
                transform.position = Vector3.Lerp(transform.position, _slot.TransformPoint(_offset), k);
                transform.rotation = Quaternion.Slerp(transform.rotation, _slot.rotation, k);
            }

            // Rebuild the timer text only when the visible second changes.
            int seconds = _item.lane == TaskLane.Done ? -2 : Mathf.CeilToInt(_item.remainingSeconds);
            bool overdue = _store != null && _store.IsOverdue(_item);
            if (seconds != _shownSeconds || overdue != _shownOverdue)
            {
                _shownSeconds = seconds;
                _shownOverdue = overdue;
                // A waiting card with a due time shows when it is due; otherwise the sprint time.
                bool showDue = _item.dueTicks > 0 && _item.lane == TaskLane.Next;
                if (seconds < 0) _timer.text = "";
                else if (showDue) _timer.text = (overdue ? "late " : "due ") + QuickAdd.FormatDue(new System.DateTime(_item.dueTicks), _store.Clock());
                else _timer.text = FormatTime(seconds);
                _timer.color = overdue ? Theme.Urgent : (showDue ? Theme.TextMain : Theme.TextDim);
            }
        }

        public static string FormatTime(int seconds)
        {
            if (seconds < 0) seconds = 0;
            return (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }
    }
}
