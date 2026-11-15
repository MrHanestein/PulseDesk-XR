using System.Collections.Generic;
using UnityEngine;

namespace PulseDesk
{
    /// <summary>Creates a card per task and keeps every lane's stack laid out.</summary>
    public class CardManager : MonoBehaviour
    {
        private const float DropRadius = 0.13f;

        private TaskStore _store;
        private DockView _dock;
        private readonly Dictionary<string, TaskCardView> _cards = new Dictionary<string, TaskCardView>();
        private readonly List<TaskItem> _scratch = new List<TaskItem>();

        /// <summary>Where the next new card appears before it glides to its lane (world position).</summary>
        public Vector3? SpawnPoint;

        public void Init(TaskStore store, DockView dock)
        {
            _store = store;
            _dock = dock;
            _store.TaskAdded += OnAdded;
            _store.TaskMoved += OnMoved;
            _store.FocusChanged += OnFocus;
            _store.TaskRemoved += OnRemoved;
            _store.TaskChanged += OnChanged;

            for (int i = 0; i < _store.Tasks.Count; i++) CreateCard(_store.Tasks[i]);
            Relayout();
            foreach (TaskCardView c in _cards.Values) c.SnapToSlot();
            OnFocus(_store.Focused);
        }

        private void OnDestroy()
        {
            if (_store == null) return;
            _store.TaskAdded -= OnAdded;
            _store.TaskMoved -= OnMoved;
            _store.FocusChanged -= OnFocus;
            _store.TaskRemoved -= OnRemoved;
            _store.TaskChanged -= OnChanged;
        }

        private void OnRemoved(TaskItem item)
        {
            TaskCardView v;
            if (_cards.TryGetValue(item.id, out v))
            {
                _cards.Remove(item.id);
                if (v != null) Destroy(v.gameObject);
            }
            Relayout();
        }

        private void OnChanged(TaskItem item)
        {
            TaskCardView v;
            if (_cards.TryGetValue(item.id, out v)) v.Refresh();
        }

        private TaskCardView CreateCard(TaskItem item)
        {
            TaskCardView v = TaskCardView.Create(item, _store, this, _dock.transform);
            _cards[item.id] = v;
            return v;
        }

        private void OnAdded(TaskItem item)
        {
            TaskCardView v = CreateCard(item);
            DockLane lane = _dock.Lane(item.lane);
            v.transform.position = SpawnPoint.HasValue ? SpawnPoint.Value : lane.slot.position;
            v.transform.rotation = lane.slot.rotation;
            Relayout();
        }

        private void OnMoved(TaskItem item, TaskLane from)
        {
            TaskCardView v;
            if (_cards.TryGetValue(item.id, out v)) v.Refresh();
            Relayout();
        }

        private void OnFocus(TaskItem focused)
        {
            foreach (KeyValuePair<string, TaskCardView> pair in _cards)
                pair.Value.SetFocused(focused != null && pair.Key == focused.id);
            Relayout();
        }

        /// <summary>Called by a card when the hand lets go of it.</summary>
        public void HandleDrop(TaskCardView card)
        {
            // Dropping a card on "Remove card" deletes it.
            if (_dock.RemoveButton != null
                && Vector3.Distance(card.transform.position, _dock.RemoveButton.transform.position) < 0.07f)
            {
                _store.Remove(card.TaskId);
                return;
            }

            DockLane nearest = null;
            float best = DropRadius;
            for (int i = 0; i < _dock.Lanes.Length; i++)
            {
                DockLane lane = _dock.Lanes[i];
                // Compare against the middle of the lane's stack area, not just the front slot.
                Vector3 centre = lane.slot.TransformPoint(new Vector3(0f, 0.04f, 0f));
                float d = Vector3.Distance(card.transform.position, centre);
                if (d < best) { best = d; nearest = lane; }
            }
            if (nearest != null) _store.Move(card.TaskId, nearest.lane);
            Relayout();   // dropped in mid-air or on its own lane: it glides home
        }

        public void Relayout()
        {
            if (_dock == null) return;
            for (int l = 0; l < _dock.Lanes.Length; l++)
            {
                DockLane lane = _dock.Lanes[l];
                _scratch.Clear();
                for (int i = 0; i < _store.Tasks.Count; i++)
                    if (_store.Tasks[i].lane == lane.lane) _scratch.Add(_store.Tasks[i]);

                int count = _scratch.Count;
                int visible = Mathf.Min(count, DockLane.MaxVisible);
                int start = 0;
                if (lane.lane == TaskLane.Done)
                {
                    start = count - visible;                       // most recent finishes
                }
                else if (_store.Focused != null)
                {
                    int f = _scratch.IndexOf(_store.Focused);
                    if (f >= visible) start = f - visible + 1;     // keep the focused card in view
                }

                for (int i = 0; i < count; i++)
                {
                    TaskCardView v;
                    if (!_cards.TryGetValue(_scratch[i].id, out v)) continue;
                    bool show = i >= start && i < start + visible;
                    if (!show && !v.Held)
                    {
                        if (v.gameObject.activeSelf) v.gameObject.SetActive(false);
                        continue;
                    }
                    if (!v.gameObject.activeSelf)
                    {
                        v.gameObject.SetActive(true);
                        v.GoTo(lane.slot, lane.StackOffset(0));
                        v.SnapToSlot();
                    }
                    // Done shows the newest in front; other lanes show the first in front.
                    int indexFromFront = lane.lane == TaskLane.Done ? (start + visible - 1 - i) : (i - start);
                    indexFromFront = Mathf.Clamp(indexFromFront, 0, DockLane.MaxVisible - 1);
                    v.GoTo(lane.slot, lane.StackOffset(indexFromFront));
                }
                lane.SetHiddenCount(count - visible);
            }
        }
    }
}
