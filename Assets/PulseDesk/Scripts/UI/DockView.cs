using System;
using TMPro;
using UnityEngine;

namespace PulseDesk
{
    /// <summary>What the dock's buttons do. Filled in by PulseDeskApp.</summary>
    public class DockActions
    {
        public Action newTask;
        public Action<string> routine;
        public Action removeFocused;
        public Action clearDone;
        public Action recenter;
    }

    /// <summary>
    /// The dock on the desk, built from code: base plate, three tilted lanes, banner, streak,
    /// the sprint timer with its - and + buttons, and one row of buttons.
    /// Local +Z points away from the user.
    /// </summary>
    public class DockView : MonoBehaviour
    {
        private const float LaneSpacing = 0.18f;
        private static readonly Quaternion Lean = Quaternion.Euler(40f, 0f, 0f);       // top leans away from the user
        private static readonly Quaternion ButtonLean = Quaternion.Euler(55f, 0f, 0f);

        public DockLane[] Lanes { get; private set; }
        public DockButton RoutinesButton { get; private set; }
        public DockButton RemoveButton { get; private set; }

        private TaskStore _store;
        private TextMeshPro _banner;
        private TextMeshPro _streak;
        private DockButton _timerButton;
        private GameObject _mainRow;
        private GameObject _pickerRow;
        private float _bannerUntil;
        private bool _bannerUrgent;
        private int _shownSeconds = int.MinValue;
        private bool _shownPaused;
        private int _shownDefault = -1;
        private string _shownIdle = "";
        private float _nextIdleCheck;

        public static DockView Build(Transform root, TaskStore store, DockActions actions)
        {
            DockView v = root.gameObject.AddComponent<DockView>();
            v._store = store;
            v.BuildAll(root, actions);
            store.StatsChanged += v.RefreshStreak;
            store.TaskAdded += v.OnTasksChanged;
            store.TaskRemoved += v.OnTasksChanged;
            store.TaskMoved += v.OnTaskMoved;
            v.RefreshStreak();
            return v;
        }

        private void OnDestroy()
        {
            if (_store == null) return;
            _store.StatsChanged -= RefreshStreak;
            _store.TaskAdded -= OnTasksChanged;
            _store.TaskRemoved -= OnTasksChanged;
            _store.TaskMoved -= OnTaskMoved;
        }

        private void OnTasksChanged(TaskItem item) { RefreshStreak(); }
        private void OnTaskMoved(TaskItem item, TaskLane from) { RefreshStreak(); }

        public DockLane Lane(TaskLane lane)
        {
            for (int i = 0; i < Lanes.Length; i++)
                if (Lanes[i].lane == lane) return Lanes[i];
            return Lanes[0];
        }

        private void BuildAll(Transform root, DockActions actions)
        {
            Make.Box("Plate", root, new Vector3(0f, 0.004f, 0.03f), new Vector3(0.58f, 0.008f, 0.27f), Theme.Plate);

            // Lanes: Next on the left, Urgent in the middle, Done on the right.
            TaskLane[] order = { TaskLane.Next, TaskLane.Urgent, TaskLane.Done };
            Lanes = new DockLane[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                float x = (i - 1) * LaneSpacing;
                GameObject go = Make.Empty("Lane_" + order[i], root, new Vector3(x, 0.05f, 0f), Lean);
                DockLane lane = go.AddComponent<DockLane>();
                lane.lane = order[i];
                lane.slot = go.transform;
                Color accent = Theme.Accent(order[i]);

                Make.Box("Panel", go.transform, new Vector3(0f, 0.05f, 0.03f), new Vector3(0.165f, 0.20f, 0.004f), Theme.Panel);
                Make.Box("Rule", go.transform, new Vector3(0f, 0.117f, 0.0275f), new Vector3(0.150f, 0.0015f, 0.002f), accent);
                Make.Icon(order[i], go.transform, new Vector3(-0.062f, 0.130f, 0.0265f), accent);
                TextMeshPro label = Make.Text("Label", go.transform, new Vector3(-0.012f, 0.130f, 0.0272f),
                    new Vector2(0.08f, 0.016f), 9f, accent, TextAlignmentOptions.Left);
                label.text = Theme.Label(order[i]);
                label.fontStyle = FontStyles.Bold;
                lane.badge = Make.Text("Badge", go.transform, new Vector3(0.055f, 0.130f, 0.0272f),
                    new Vector2(0.04f, 0.016f), 7.5f, Theme.TextDim, TextAlignmentOptions.Right);
                Lanes[i] = lane;

                if (order[i] == TaskLane.Urgent)
                {
                    // The timer doubles as the pause / resume button. - and + change the sprint length.
                    Vector3 at = new Vector3(0f, 0.078f, 0.02f);
                    DockButton.Create("Shorter", go.transform, at + new Vector3(-0.058f, 0f, 0f),
                        new Vector2(0.036f, 0.045f), "-", 15f, () => { if (_store != null) _store.AdjustSprint(-1); });
                    _timerButton = DockButton.Create("Timer", go.transform, at,
                        new Vector2(0.072f, 0.045f), "", 12f, TogglePause);
                    DockButton.Create("Longer", go.transform, at + new Vector3(0.058f, 0f, 0f),
                        new Vector2(0.036f, 0.045f), "+", 15f, () => { if (_store != null) _store.AdjustSprint(1); });
                }
            }

            // Banner and day summary along the back edge. Status lives on the dock, never head-locked.
            Transform top = Make.Empty("Top", root, new Vector3(0f, 0.05f, 0f), Lean).transform;
            // The banner is the main guidance line, so it is sized to be read at arm's length.
            Make.Box("BannerBack", top, new Vector3(0f, 0.174f, 0.03f), new Vector3(0.545f, 0.038f, 0.004f), Theme.Panel);
            _banner = Make.Text("Banner", top, new Vector3(-0.075f, 0.174f, 0.0272f), new Vector2(0.37f, 0.032f),
                10f, Theme.TextMain, TextAlignmentOptions.Left);
            _streak = Make.Text("Streak", top, new Vector3(0.19f, 0.174f, 0.0272f), new Vector2(0.15f, 0.032f),
                7.5f, Theme.Done, TextAlignmentOptions.Right);

            // One row of buttons at the front edge. Never a grid.
            Transform row = Make.Empty("Buttons", root, new Vector3(0f, 0.024f, -0.085f), ButtonLean).transform;
            float w = 0.105f;
            float gap = 0.006f;

            _mainRow = Make.Empty("Main", row, Vector3.zero, Quaternion.identity);
            string[] labels = { "New task", "Routines", "Remove card", "Clear done", "Move dock" };
            Action[] presses = { actions.newTask, OpenPicker, actions.removeFocused, actions.clearDone, actions.recenter };
            for (int i = 0; i < labels.Length; i++)
            {
                DockButton b = DockButton.Create("Main_" + i, _mainRow.transform, new Vector3(RowX(i, labels.Length, w, gap), 0f, 0f),
                    new Vector2(w, 0.04f), labels[i], 7f, presses[i]);
                if (i == 1) RoutinesButton = b;
                if (i == 2) RemoveButton = b;
            }

            // Routines: each adds three ready-made steps to Next. The "+" says it adds, it does not do.
            _pickerRow = Make.Empty("Picker", row, Vector3.zero, Quaternion.identity);
            int n = GoalPresets.Titles.Length;
            for (int i = 0; i <= n; i++)
            {
                float x = RowX(i, n + 1, w, gap);
                if (i < n)
                {
                    string goal = GoalPresets.Titles[i];
                    DockButton.Create("Routine_" + i, _pickerRow.transform, new Vector3(x, 0f, 0f),
                        new Vector2(w, 0.04f), "+ " + goal, 6.5f,
                        () => { ClosePicker(); if (actions.routine != null) actions.routine(goal); });
                }
                else
                {
                    DockButton b = DockButton.Create("Close", _pickerRow.transform, new Vector3(x, 0f, 0f),
                        new Vector2(w, 0.04f), "Back", 6.5f, ClosePicker);
                    b.SetLabelColor(Theme.TextDim);
                }
            }
            _pickerRow.SetActive(false);
        }

        private static float RowX(int index, int count, float width, float gap)
        {
            float total = count * width + (count - 1) * gap;
            return -total * 0.5f + width * 0.5f + index * (width + gap);
        }

        private void TogglePause()
        {
            if (_store != null) _store.ToggleUserPause();
        }

        public void OpenPicker()
        {
            _mainRow.SetActive(false);
            _pickerRow.SetActive(true);
            ShowBanner("A routine adds 3 ready-made steps to NEXT", false);
        }

        public void ClosePicker()
        {
            _pickerRow.SetActive(false);
            _mainRow.SetActive(true);
        }

        public void ShowBanner(string message, bool urgent)
        {
            _bannerUntil = Time.unscaledTime + (urgent ? 8f : 5f);
            _bannerUrgent = urgent;
            _banner.color = urgent ? Theme.Urgent : Theme.TextMain;
            _banner.text = (urgent ? "! " : "") + message;
            _shownIdle = "";
        }

        /// <summary>The day at a glance: what is open, what is finished, and the streak.</summary>
        private void RefreshStreak()
        {
            if (_streak == null || _store == null) return;
            int open = _store.Tasks.Count - _store.CountIn(TaskLane.Done);
            string text = open + " open  |  " + _store.DoneToday + " done";
            if (_store.DayStreak > 0) text += "  |  day " + _store.DayStreak;
            _streak.text = text;
        }

        private void Update()
        {
            if (_store == null) return;

            // Timer label: rebuilt only when the visible second, the pause state or the default length changes.
            TaskItem sprint = _store.ActiveSprint();
            int seconds = sprint == null ? -1 : Mathf.CeilToInt(sprint.remainingSeconds);
            bool paused = _store.IsPaused;
            int defaultMinutes = Mathf.RoundToInt(_store.DefaultMinutes);
            if (seconds != _shownSeconds || paused != _shownPaused || defaultMinutes != _shownDefault)
            {
                _shownSeconds = seconds;
                _shownPaused = paused;
                _shownDefault = defaultMinutes;
                if (sprint == null)
                {
                    _timerButton.SetLabel(defaultMinutes + " min");
                    _timerButton.SetLabelColor(Theme.TextDim);
                }
                else
                {
                    _timerButton.SetLabel(TaskCardView.FormatTime(seconds));
                    _timerButton.SetLabelColor(paused ? Theme.TextDim : (seconds <= 30 ? Theme.Urgent : Theme.TextMain));
                }
            }

            // When no alert is showing, the banner gives one line: the sprint, or what is due next.
            if (Time.unscaledTime > _bannerUntil && Time.unscaledTime >= _nextIdleCheck)
            {
                _nextIdleCheck = Time.unscaledTime + 0.5f;
                string idle;
                bool late = false;
                TaskItem due = _store.NextDue();
                if (sprint != null && paused) idle = "Paused. Pinch the timer to resume";
                else if (sprint != null && seconds <= 0) { idle = "Time is up: " + sprint.title; late = true; }
                else if (due != null && _store.IsOverdue(due)) { idle = "Overdue: " + due.title; late = true; }
                else if (sprint != null) idle = "Sprint: " + sprint.title;
                else if (due != null) idle = "Next up " + QuickAdd.FormatDue(new DateTime(due.dueTicks), _store.Clock()) + "  " + due.title;
                else if (_store.Tasks.Count == _store.CountIn(TaskLane.Done)) idle = "All clear. Pinch \"New task\" to add one";
                else idle = "Drag a card to URGENT to start a sprint";

                if (idle != _shownIdle || _bannerUrgent)
                {
                    _shownIdle = idle;
                    _bannerUrgent = false;
                    _banner.color = late ? Theme.Urgent : Theme.TextDim;
                    _banner.text = idle;
                }
            }
        }
    }
}
