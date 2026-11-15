using System;
using TMPro;
using UnityEngine;

namespace PulseDesk
{
    /// <summary>
    /// The dock on the desk, built from code: base plate, three tilted lanes, banner, streak,
    /// the sprint timer and one row of buttons. Local +Z points away from the user.
    /// </summary>
    public class DockView : MonoBehaviour
    {
        private const float LaneSpacing = 0.18f;
        private static readonly Quaternion Lean = Quaternion.Euler(40f, 0f, 0f);       // top leans away from the user
        private static readonly Quaternion ButtonLean = Quaternion.Euler(55f, 0f, 0f);

        public DockLane[] Lanes { get; private set; }
        public DockButton BreakDownButton { get; private set; }

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
        private string _shownIdle = "";

        public static DockView Build(Transform root, TaskStore store, Action onRecenter, Action<string> onGoal, Action onNewTask)
        {
            DockView v = root.gameObject.AddComponent<DockView>();
            v._store = store;
            v.BuildAll(root, onRecenter, onGoal, onNewTask);
            store.StatsChanged += v.RefreshStreak;
            v.RefreshStreak();
            return v;
        }

        private void OnDestroy()
        {
            if (_store != null) _store.StatsChanged -= RefreshStreak;
        }

        public DockLane Lane(TaskLane lane)
        {
            for (int i = 0; i < Lanes.Length; i++)
                if (Lanes[i].lane == lane) return Lanes[i];
            return Lanes[0];
        }

        private void BuildAll(Transform root, Action onRecenter, Action<string> onGoal, Action onNewTask)
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
                    // The timer doubles as the pause / resume button.
                    _timerButton = DockButton.Create("Timer", go.transform, new Vector3(0f, 0.078f, 0.02f),
                        new Vector2(0.14f, 0.045f), "", 15f, store_TogglePause);
                }
            }

            // Banner and streak along the back edge. Status lives on the dock, never head-locked.
            Transform top = Make.Empty("Top", root, new Vector3(0f, 0.05f, 0f), Lean).transform;
            // The banner is the main guidance line, so it is sized to be read at arm's length.
            Make.Box("BannerBack", top, new Vector3(0f, 0.174f, 0.03f), new Vector3(0.545f, 0.038f, 0.004f), Theme.Panel);
            _banner = Make.Text("Banner", top, new Vector3(-0.07f, 0.174f, 0.0272f), new Vector2(0.38f, 0.032f),
                10f, Theme.TextMain, TextAlignmentOptions.Left);
            _streak = Make.Text("Streak", top, new Vector3(0.195f, 0.174f, 0.0272f), new Vector2(0.14f, 0.032f),
                8f, Theme.Done, TextAlignmentOptions.Right);

            // One row of buttons at the front edge. Never a grid.
            Transform row = Make.Empty("Buttons", root, new Vector3(0f, 0.024f, -0.085f), ButtonLean).transform;
            _mainRow = Make.Empty("Main", row, Vector3.zero, Quaternion.identity);
            DockButton.Create("NewTask", _mainRow.transform, new Vector3(-0.175f, 0f, 0f),
                new Vector2(0.16f, 0.04f), "New task", 8f, onNewTask);
            BreakDownButton = DockButton.Create("BreakDown", _mainRow.transform, new Vector3(0f, 0f, 0f),
                new Vector2(0.16f, 0.04f), "Break it down", 8f, OpenPicker);
            DockButton.Create("MoveDock", _mainRow.transform, new Vector3(0.175f, 0f, 0f),
                new Vector2(0.16f, 0.04f), "Move dock here", 8f, onRecenter);

            _pickerRow = Make.Empty("Picker", row, Vector3.zero, Quaternion.identity);
            int n = GoalPresets.Titles.Length;
            float w = 0.105f;
            float gap = 0.006f;
            float total = (n + 1) * w + n * gap;
            for (int i = 0; i <= n; i++)
            {
                float x = -total * 0.5f + w * 0.5f + i * (w + gap);
                if (i < n)
                {
                    string goal = GoalPresets.Titles[i];
                    DockButton.Create("Goal_" + i, _pickerRow.transform, new Vector3(x, 0f, 0f),
                        new Vector2(w, 0.04f), goal, 6.5f, () => { ClosePicker(); if (onGoal != null) onGoal(goal); });
                }
                else
                {
                    DockButton b = DockButton.Create("Close", _pickerRow.transform, new Vector3(x, 0f, 0f),
                        new Vector2(w, 0.04f), "Close", 6.5f, ClosePicker);
                    b.SetLabelColor(Theme.TextDim);
                }
            }
            _pickerRow.SetActive(false);
        }

        private void store_TogglePause()
        {
            if (_store != null) _store.ToggleUserPause();
        }

        public void OpenPicker()
        {
            _mainRow.SetActive(false);
            _pickerRow.SetActive(true);
            ShowBanner("Pick a goal to break into three steps", false);
        }

        public void ClosePicker()
        {
            _pickerRow.SetActive(false);
            _mainRow.SetActive(true);
        }

        public void ShowBanner(string message, bool urgent)
        {
            _bannerUntil = Time.unscaledTime + (urgent ? 7f : 5f);
            _bannerUrgent = urgent;
            _banner.color = urgent ? Theme.Urgent : Theme.TextMain;
            _banner.text = (urgent ? "! " : "") + message;
            _shownIdle = "";
        }

        private void RefreshStreak()
        {
            if (_streak == null || _store == null) return;
            _streak.text = _store.DayStreak > 0
                ? "Day " + _store.DayStreak + "  |  " + _store.DoneToday + " today"
                : "No streak yet";
        }

        private void Update()
        {
            if (_store == null) return;

            // Timer label: rebuilt only when the visible second or the pause state changes.
            TaskItem sprint = _store.ActiveSprint();
            int seconds = sprint == null ? -1 : Mathf.CeilToInt(sprint.remainingSeconds);
            bool paused = _store.IsPaused;
            if (seconds != _shownSeconds || paused != _shownPaused)
            {
                _shownSeconds = seconds;
                _shownPaused = paused;
                if (sprint == null)
                {
                    _timerButton.SetLabel("No sprint");
                    _timerButton.SetLabelColor(Theme.TextDim);
                }
                else
                {
                    string time = TaskCardView.FormatTime(seconds);
                    _timerButton.SetLabel(paused ? "Paused " + time : time);
                    _timerButton.SetLabelColor(paused ? Theme.TextDim : (seconds <= 30 ? Theme.Urgent : Theme.TextMain));
                }
            }
            // The timer text is small when it reads "Drop a card here".
            // (Font size is fixed; TMP ellipsis keeps it inside the button.)

            // When no alert is showing, the banner gives one line of guidance.
            if (Time.unscaledTime > _bannerUntil)
            {
                string idle;
                if (_store.Tasks.Count == _store.CountIn(TaskLane.Done)) idle = "Pinch \"New task\" to add your own";
                else if (sprint == null) idle = "Drag a card to URGENT to start a sprint";
                else if (paused) idle = "Paused. Pinch the timer to resume";
                else idle = "Sprint: " + sprint.title;
                if (idle != _shownIdle || _bannerUrgent)
                {
                    _shownIdle = idle;
                    _bannerUrgent = false;
                    _banner.color = Theme.TextDim;
                    _banner.text = idle;
                }
            }
        }
    }
}
