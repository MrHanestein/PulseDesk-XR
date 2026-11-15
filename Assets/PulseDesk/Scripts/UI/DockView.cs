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
        public Action clearOrUndo;
        public Func<string> clearLabel;   // "Undo", "Clear done", "Clear all" or "Clear"
        public Action recenter;
    }

    /// <summary>
    /// The dock on the desk, built from code: base plate, three tilted lanes, banner, day summary,
    /// the sprint controls and one row of buttons. Compact mode leaves only the banner and the
    /// running sprint, to sit beside real work without covering it.
    /// Local +Z points away from the user.
    /// </summary>
    public class DockView : MonoBehaviour
    {
        private const float LaneSpacing = 0.18f;
        private static readonly Quaternion Lean = Quaternion.Euler(40f, 0f, 0f);       // top leans away from the user
        private static readonly Quaternion ButtonLean = Quaternion.Euler(55f, 0f, 0f);

        public DockLane[] Lanes { get; private set; }
        public DockButton RoutinesButton { get; private set; }
        public DockButton ClearButton { get; private set; }
        public bool Compact { get; private set; }

        /// <summary>Raised after compact mode changes, so cards can be laid out again.</summary>
        public event Action CompactChanged;

        private TaskStore _store;
        private DockActions _actions;
        private TextMeshPro _banner;
        private TextMeshPro _summary;
        private GameObject _plate;
        private GameObject _rows;
        private GameObject _mainRow;
        private GameObject _pickerRow;
        private DockButton _leftButton;     // "-" with no sprint, "Done" during one
        private DockButton _timerButton;    // "Start 5m" with no sprint, the countdown during one
        private DockButton _rightButton;    // "+" with no sprint, "+5" during one
        private DockButton _modeButton;     // "Compact" / "Expand"
        private float _bannerUntil;
        private bool _bannerUrgent;
        private string _shownTimer = "";
        private string _shownIdle = "";
        private string _shownClear = "";
        private float _nextSlowCheck;
        private const int PipCount = 10;
        private Renderer[] _pips;
        private int _shownPips = -1;
        private TextMeshPro _glance;
        private string _shownGlance = "";
        private Transform _bar;
        private const float BarWidth = 0.150f;
        private readonly System.Collections.Generic.List<TaskItem> _glanceScratch = new System.Collections.Generic.List<TaskItem>();

        public static DockView Build(Transform root, TaskStore store, DockActions actions)
        {
            DockView v = root.gameObject.AddComponent<DockView>();
            v._store = store;
            v._actions = actions;
            v.BuildAll(root);
            store.StatsChanged += v.RefreshSummary;
            store.TaskAdded += v.OnTasksChanged;
            store.TaskRemoved += v.OnTasksChanged;
            store.TaskMoved += v.OnTaskMoved;
            store.BoardReset += v.RefreshSummary;
            v.RefreshSummary();
            return v;
        }

        private void OnDestroy()
        {
            if (_store == null) return;
            _store.StatsChanged -= RefreshSummary;
            _store.TaskAdded -= OnTasksChanged;
            _store.TaskRemoved -= OnTasksChanged;
            _store.TaskMoved -= OnTaskMoved;
            _store.BoardReset -= RefreshSummary;
        }

        private void OnTasksChanged(TaskItem item) { RefreshSummary(); }
        private void OnTaskMoved(TaskItem item, TaskLane from) { RefreshSummary(); }

        public DockLane Lane(TaskLane lane)
        {
            for (int i = 0; i < Lanes.Length; i++)
                if (Lanes[i].lane == lane) return Lanes[i];
            return Lanes[0];
        }

        private void BuildAll(Transform root)
        {
            _plate = Make.Box("Plate", root, new Vector3(0f, 0.004f, 0.03f), new Vector3(0.58f, 0.008f, 0.27f), Theme.Plate).gameObject;

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
                if (order[i] == TaskLane.Urgent)
                {
                    // In the sprint lane the rule under the heading is the progress bar: it drains as time runs out.
                    Make.Box("Track", go.transform, new Vector3(0f, 0.116f, 0.0282f), new Vector3(BarWidth, 0.005f, 0.002f), Theme.Button);
                    _bar = Make.Box("Bar", go.transform, new Vector3(0f, 0.116f, 0.0272f), new Vector3(BarWidth, 0.005f, 0.002f), accent).transform;
                }
                else
                {
                    Make.Box("Rule", go.transform, new Vector3(0f, 0.117f, 0.0275f), new Vector3(0.150f, 0.0015f, 0.002f), accent);
                }
                Make.Icon(order[i], go.transform, new Vector3(-0.062f, 0.130f, 0.0265f), accent);
                TextMeshPro label = Make.Text("Label", go.transform, new Vector3(-0.012f, 0.130f, 0.0272f),
                    new Vector2(0.08f, 0.016f), 9f, accent, TextAlignmentOptions.Left);
                label.text = Theme.Label(order[i]);
                label.fontStyle = FontStyles.Bold;
                lane.badge = Make.Text("Badge", go.transform, new Vector3(0.055f, 0.130f, 0.0272f),
                    new Vector2(0.04f, 0.016f), 7.5f, Theme.TextDim, TextAlignmentOptions.Right);
                Lanes[i] = lane;

                if (order[i] == TaskLane.Done)
                {
                    // One pip lights for each task finished today: the day's progress at a glance.
                    _pips = new Renderer[PipCount];
                    for (int p = 0; p < PipCount; p++)
                    {
                        float px = (p - (PipCount - 1) * 0.5f) * 0.0145f;
                        _pips[p] = Make.Box("Pip", go.transform, new Vector3(px, 0.106f, 0.0272f), new Vector3(0.009f, 0.005f, 0.002f), Theme.PipOff);
                    }
                }

                if (order[i] == TaskLane.Urgent)
                {
                    // Three buttons that change with the state, so the common action is always one tap:
                    //   nothing running:  [-]    [Start 5m]  [+]     set the length, start the next card
                    //   sprint running:   [Done] [2:57]      [+5]    finish, pause/resume, more time
                    Vector3 at = new Vector3(0f, 0.078f, 0.02f);
                    _leftButton = DockButton.Create("Left", go.transform, at + new Vector3(-0.056f, 0f, 0f),
                        new Vector2(0.044f, 0.045f), "-", 9f, OnLeft);
                    _timerButton = DockButton.Create("Timer", go.transform, at,
                        new Vector2(0.062f, 0.045f), "", 10f, OnTimer);
                    _rightButton = DockButton.Create("Right", go.transform, at + new Vector3(0.056f, 0f, 0f),
                        new Vector2(0.044f, 0.045f), "+", 9f, OnRight);

                    // Compact mode only: one line under the sprint that answers "what else is there" at a glance.
                    _glance = Make.Text("Glance", go.transform, new Vector3(0f, -0.062f, 0.0f), new Vector2(0.34f, 0.022f),
                        8.5f, Theme.TextMain, TextAlignmentOptions.Center);
                    _glance.gameObject.SetActive(false);
                }
            }

            // Banner, day summary and the compact switch along the back edge. Never head-locked.
            Transform top = Make.Empty("Top", root, new Vector3(0f, 0.05f, 0f), Lean).transform;
            Make.Box("BannerBack", top, new Vector3(0f, 0.174f, 0.03f), new Vector3(0.545f, 0.038f, 0.004f), Theme.Panel);
            _banner = Make.Text("Banner", top, new Vector3(-0.118f, 0.174f, 0.0272f), new Vector2(0.285f, 0.032f),
                10f, Theme.TextMain, TextAlignmentOptions.Left);
            _summary = Make.Text("Summary", top, new Vector3(0.108f, 0.174f, 0.0272f), new Vector2(0.155f, 0.032f),
                8f, Theme.Done, TextAlignmentOptions.Right);
            _modeButton = DockButton.Create("Mode", top, new Vector3(0.232f, 0.174f, 0.024f),
                new Vector2(0.07f, 0.034f), "Compact", 8.5f, () => SetCompact(!Compact));

            // One row of buttons at the front edge. Never a grid.
            _rows = Make.Empty("Buttons", root, new Vector3(0f, 0.024f, -0.085f), ButtonLean);
            Transform row = _rows.transform;

            _mainRow = Make.Empty("Main", row, Vector3.zero, Quaternion.identity);
            string[] labels = { "New task", "Routines", "Clear", "Move dock" };
            Action[] presses = { _actions.newTask, OpenPicker, _actions.clearOrUndo, _actions.recenter };
            for (int i = 0; i < labels.Length; i++)
            {
                DockButton b = DockButton.Create("Main_" + i, _mainRow.transform, new Vector3(RowX(i, labels.Length, 0.13f, 0.006f), 0f, 0f),
                    new Vector2(0.13f, 0.04f), labels[i], 8f, presses[i]);
                if (i == 1) RoutinesButton = b;
                if (i == 2) ClearButton = b;
            }

            // Routines: each adds three timed steps to Next. The "+" says it adds, it does not do.
            _pickerRow = Make.Empty("Picker", row, Vector3.zero, Quaternion.identity);
            int n = GoalPresets.Titles.Length;
            for (int i = 0; i <= n; i++)
            {
                float x = RowX(i, n + 1, 0.105f, 0.006f);
                if (i < n)
                {
                    string goal = GoalPresets.Titles[i];
                    DockButton.Create("Routine_" + i, _pickerRow.transform, new Vector3(x, 0f, 0f),
                        new Vector2(0.105f, 0.04f), "+ " + goal, 6.5f,
                        () => { ClosePicker(); if (_actions.routine != null) _actions.routine(goal); });
                }
                else
                {
                    DockButton b = DockButton.Create("Close", _pickerRow.transform, new Vector3(x, 0f, 0f),
                        new Vector2(0.105f, 0.04f), "Back", 6.5f, ClosePicker);
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

        // ---------- sprint controls ----------

        private void OnLeft()
        {
            if (_store.ActiveSprint() != null) _store.CompleteSprint();
            else _store.AdjustSprint(-1);
        }

        private void OnTimer()
        {
            if (_store.ActiveSprint() != null) _store.ToggleUserPause();
            else _store.StartNext();
        }

        private void OnRight()
        {
            if (_store.ActiveSprint() != null) _store.AddSprintMinutes(5);
            else _store.AdjustSprint(1);
        }

        // ---------- compact mode ----------

        /// <summary>
        /// Compact: only the banner and the running sprint stay, so the dock can sit by a laptop.
        /// Full: the whole board.
        /// </summary>
        public void SetCompact(bool compact)
        {
            if (Compact == compact) return;
            Compact = compact;
            _plate.SetActive(!compact);
            _rows.SetActive(!compact);
            for (int i = 0; i < Lanes.Length; i++)
            {
                bool hide = compact && Lanes[i].lane != TaskLane.Urgent;
                Lanes[i].hidden = hide;
                Lanes[i].gameObject.SetActive(!hide);
            }
            _modeButton.SetLabel(compact ? "Expand" : "Compact");
            _glance.gameObject.SetActive(compact);
            _shownGlance = "";
            if (!compact) ClosePicker();
            if (CompactChanged != null) CompactChanged();
            ShowBanner(compact ? "Compact. Only your sprint stays in view" : "Full board", false);
        }

        public void OpenPicker()
        {
            if (Compact) SetCompact(false);
            _mainRow.SetActive(false);
            _pickerRow.SetActive(true);
            ShowBanner("A routine adds 3 timed steps and starts the first", false);
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

        /// <summary>The day at a glance: what is open, what is finished, and time spent in sprints.</summary>
        private void RefreshSummary()
        {
            if (_summary == null || _store == null) return;
            int open = _store.Tasks.Count - _store.CountIn(TaskLane.Done);
            _summary.text = open + " open | " + _store.DoneToday + " done | " + _store.FocusMinutesToday + "m focus";

            int lit = Mathf.Min(PipCount, _store.DoneToday);
            if (_pips != null && lit != _shownPips)
            {
                _shownPips = lit;
                for (int p = 0; p < _pips.Length; p++)
                    Make.SetColor(_pips[p].sharedMaterial, p < lit ? Theme.Done : Theme.PipOff);
            }
        }

        private void Update()
        {
            if (_store == null) return;

            TaskItem sprint = _store.ActiveSprint();
            int seconds = sprint == null ? -1 : Mathf.CeilToInt(sprint.remainingSeconds);
            bool paused = _store.IsPaused;

            // Sprint controls: rebuilt only when what they show changes.
            string timer = sprint == null
                ? "Start " + QuickAdd.FormatMinutes(_store.DefaultMinutes)
                : (paused ? "II " : "") + TaskCardView.FormatTime(seconds);
            if (timer != _shownTimer)
            {
                bool wasSprint = _shownTimer.Length > 0 && !_shownTimer.StartsWith("Start");
                _shownTimer = timer;
                _timerButton.SetLabel(timer);
                if (sprint == null) _timerButton.SetLabelColor(Theme.Done);
                else _timerButton.SetLabelColor(paused ? Theme.TextDim : (seconds <= 60 ? Theme.Urgent : Theme.TextMain));
                if (wasSprint != (sprint != null) || _leftButton.LabelText.Length == 0)
                {
                    _leftButton.SetLabel(sprint != null ? "Done" : "-");
                    _leftButton.SetLabelColor(sprint != null ? Theme.Done : Theme.TextMain);
                    _rightButton.SetLabel(sprint != null ? "+5" : "+");
                }
            }

            // Progress bar: full at the start of a sprint, empty when time is up, anchored on the left.
            float left = sprint == null ? 1f : Mathf.Clamp01(sprint.remainingSeconds / Mathf.Max(1f, sprint.durationSeconds));
            _bar.localScale = new Vector3(Mathf.Max(0.0005f, BarWidth * left), 0.005f, 0.002f);
            _bar.localPosition = new Vector3(-BarWidth * 0.5f * (1f - left), 0.116f, 0.0272f);

            // Last minute and time-up: the timer pulses so it is noticed from the corner of the eye.
            bool pulse = sprint != null && !paused && seconds <= 60;
            float scale = pulse ? 1f + 0.07f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * (seconds <= 0 ? 6f : 3f))) : 1f;
            _timerButton.transform.localScale = new Vector3(scale, scale, 1f);

            // Time is up: bring the whole board back so the next step is one tap away.
            if (Compact && sprint != null && seconds <= 0) SetCompact(false);

            if (Time.unscaledTime < _nextSlowCheck) return;
            _nextSlowCheck = Time.unscaledTime + 0.25f;

            if (Compact)
            {
                _store.GetLane(TaskLane.Next, _glanceScratch);
                int waiting = _glanceScratch.Count;
                string glance;
                if (waiting == 0) glance = "Nothing waiting  |  " + _store.DoneToday + " done today";
                else
                {
                    TaskItem next = _glanceScratch[0];
                    string when = next.dueTicks > 0 ? QuickAdd.FormatDue(new DateTime(next.dueTicks), _store.Clock()) + " " : "";
                    glance = "Then: " + when + next.title + (waiting > 1 ? "  +" + (waiting - 1) + " more" : "") + "  |  " + _store.DoneToday + " done";
                }
                if (glance != _shownGlance)
                {
                    _shownGlance = glance;
                    _glance.text = glance;
                }
            }

            // The Clear button says what it will do right now.
            string clear = _actions.clearLabel != null ? _actions.clearLabel() : "Clear";
            if (clear != _shownClear)
            {
                _shownClear = clear;
                ClearButton.SetLabel(clear);
                ClearButton.SetLabelColor(clear == "Undo" ? Theme.Next : (clear == "Clear" ? Theme.TextDim : Theme.TextMain));
            }

            // When no alert is showing, the banner gives one line: the sprint, or what is due next.
            if (Time.unscaledTime > _bannerUntil)
            {
                string idle;
                bool late = false;
                TaskItem due = _store.NextDue();
                if (sprint != null && paused) idle = "Paused. Pinch the timer to resume";
                else if (sprint != null && seconds <= 0) { idle = "Time is up: " + sprint.title; late = true; }
                else if (due != null && _store.IsOverdue(due) && due != sprint) { idle = "Overdue: " + due.title; late = true; }
                else if (sprint != null) idle = "Now: " + sprint.title;
                else if (due != null) idle = "Next up " + QuickAdd.FormatDue(new DateTime(due.dueTicks), _store.Clock()) + "  " + due.title;
                else if (_store.Tasks.Count == _store.CountIn(TaskLane.Done)) idle = "All clear. Pinch New task to add one";
                else idle = "Pinch Start to begin the next card";

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
