using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PulseDesk
{
    /// <summary>
    /// The single source of truth for tasks. No Meta SDK types in here.
    /// UI listens to the events; input code calls the commands.
    /// </summary>
    public class TaskStore : MonoBehaviour
    {
        public event Action<TaskItem> TaskAdded;
        public event Action<TaskItem, TaskLane> TaskMoved;   // item, lane it came from
        public event Action<TaskItem> FocusChanged;          // may be null
        public event Action<string, bool> Alert;             // message, urgent
        public event Action StatsChanged;

        /// <summary>Replace in tests to control "today".</summary>
        public Func<DateTime> Clock = () => DateTime.Now;

        public IReadOnlyList<TaskItem> Tasks { get { return _save.tasks; } }
        public TaskItem Focused { get; private set; }
        public int DayStreak { get { return _save.dayStreak; } }
        public int DoneToday { get { return _save.doneToday; } }
        public bool OnboardingDone { get { return _save.onboardingDone; } }
        public bool IsPaused { get { return _userPaused || _systemPaused; } }
        public bool IsUserPaused { get { return _userPaused; } }

        private const string DateFormat = "yyyy-MM-dd";
        private PulseDeskSave _save = new PulseDeskSave();
        private bool _userPaused;
        private bool _systemPaused;
        private bool _initialized;
        private string _savePath;

        // Loads in Awake so every other script can read Tasks in its Start.
        private void Awake()
        {
            if (!_initialized) Initialize(null);
        }

        /// <summary>Called by Awake. Tests call it directly with a temp path.</summary>
        public void Initialize(string savePath)
        {
            _initialized = true;
            _savePath = string.IsNullOrEmpty(savePath)
                ? Path.Combine(Application.persistentDataPath, "pulsedesk.json")
                : savePath;
            Load();
            if (_save.tasks.Count == 0 && !_save.onboardingDone) SeedOnboarding();
            Focused = FirstOpen();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Advances the active sprint. Public so tests can drive time.</summary>
        public void Tick(float dt)
        {
            if (IsPaused) return;
            TaskItem t = ActiveSprint();
            if (t == null || t.remainingSeconds <= 0f) return;

            t.remainingSeconds = Mathf.Max(0f, t.remainingSeconds - dt);
            float progress = 1f - t.remainingSeconds / Mathf.Max(1f, t.durationSeconds);

            if (!t.alert50 && progress >= 0.5f)
            {
                t.alert50 = true;
                RaiseAlert("Halfway: " + t.title, false);
            }
            if (!t.alert90 && progress >= 0.9f)
            {
                t.alert90 = true;
                RaiseAlert("Nearly out of time: " + t.title, true);
            }
            if (!t.alertEnd && t.remainingSeconds <= 0f)
            {
                t.alertEnd = true;
                RaiseAlert("Time is up: " + t.title, true);
            }
        }

        // ---------- queries ----------

        public TaskItem Get(string id)
        {
            for (int i = 0; i < _save.tasks.Count; i++)
                if (_save.tasks[i].id == id) return _save.tasks[i];
            return null;
        }

        public TaskItem ActiveSprint()
        {
            for (int i = 0; i < _save.tasks.Count; i++)
                if (_save.tasks[i].lane == TaskLane.Urgent) return _save.tasks[i];
            return null;
        }

        public int CountIn(TaskLane lane)
        {
            int n = 0;
            for (int i = 0; i < _save.tasks.Count; i++)
                if (_save.tasks[i].lane == lane) n++;
            return n;
        }

        private TaskItem FirstOpen()
        {
            for (int i = 0; i < _save.tasks.Count; i++)
                if (_save.tasks[i].lane != TaskLane.Done) return _save.tasks[i];
            return null;
        }

        // ---------- commands ----------

        public TaskItem Add(string title, string microStep = "", float minutes = 5f, TaskLane lane = TaskLane.Next)
        {
            var t = new TaskItem
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 8),
                title = title,
                microStep = microStep,
                lane = TaskLane.Next,
                durationSeconds = minutes * 60f,
                remainingSeconds = minutes * 60f
            };
            _save.tasks.Add(t);
            if (TaskAdded != null) TaskAdded(t);
            if (lane != TaskLane.Next) Move(t.id, lane);
            if (Focused == null) SetFocus(t.id);
            Save();
            return t;
        }

        public void Move(string id, TaskLane to)
        {
            TaskItem t = Get(id);
            if (t == null || t.lane == to) return;

            if (to == TaskLane.Urgent)
            {
                // Only one sprint at a time: the current one steps back to Next.
                TaskItem current = ActiveSprint();
                if (current != null)
                {
                    current.lane = TaskLane.Next;
                    if (TaskMoved != null) TaskMoved(current, TaskLane.Urgent);
                }
                if (t.remainingSeconds <= 0f)
                {
                    t.remainingSeconds = t.durationSeconds;
                    t.alert50 = false;
                    t.alert90 = false;
                    t.alertEnd = false;
                }
                _userPaused = false;
            }

            TaskLane from = t.lane;
            t.lane = to;
            if (TaskMoved != null) TaskMoved(t, from);

            if (to == TaskLane.Done)
            {
                RegisterCompletion();
                if (Focused == t) CycleFocus(1);
            }
            else if (to == TaskLane.Urgent)
            {
                RaiseAlert("Sprint started: " + t.title, false);
            }
            Save();
        }

        public void MoveFocused(TaskLane to)
        {
            if (Focused != null) Move(Focused.id, to);
        }

        public void SetFocus(string id)
        {
            TaskItem t = Get(id);
            if (t == null || t == Focused || t.lane == TaskLane.Done) return;
            Focused = t;
            if (FocusChanged != null) FocusChanged(Focused);
        }

        /// <summary>dir = +1 next, -1 previous, among tasks that are not Done.</summary>
        public void CycleFocus(int dir)
        {
            List<TaskItem> open = _save.tasks.FindAll(x => x.lane != TaskLane.Done);
            if (open.Count == 0)
            {
                Focused = null;
                if (FocusChanged != null) FocusChanged(null);
                return;
            }
            int i = Focused == null ? -1 : open.IndexOf(Focused);
            i = i < 0 ? 0 : ((i + dir) % open.Count + open.Count) % open.Count;
            Focused = open[i];
            if (FocusChanged != null) FocusChanged(Focused);
        }

        public void ToggleUserPause()
        {
            if (ActiveSprint() == null)
            {
                RaiseAlert("No sprint running. Move a card to URGENT", false);
                return;
            }
            _userPaused = !_userPaused;
            RaiseAlert(_userPaused ? "Sprint paused" : "Sprint running", false);
        }

        /// <summary>Called when the system menu opens/closes or the headset comes off.</summary>
        public void SetSystemPaused(bool paused)
        {
            _systemPaused = paused;
            if (paused) Save();
        }

        public void MarkOnboardingDone()
        {
            _save.onboardingDone = true;
            Save();
        }

        /// <summary>True while any starter card is still open.</summary>
        public bool OnboardingCardsOpen()
        {
            for (int i = 0; i < _save.tasks.Count; i++)
            {
                TaskItem t = _save.tasks[i];
                if (t.lane != TaskLane.Done && t.id != null && t.id.StartsWith("onb")) return true;
            }
            return false;
        }

        // ---------- streak ----------

        private void RegisterCompletion()
        {
            string today = Clock().ToString(DateFormat);
            string yesterday = Clock().AddDays(-1).ToString(DateFormat);

            if (_save.lastDoneDate == today)
            {
                _save.doneToday++;
            }
            else
            {
                _save.dayStreak = _save.lastDoneDate == yesterday ? _save.dayStreak + 1 : 1;
                _save.doneToday = 1;
            }
            _save.lastDoneDate = today;
            if (StatsChanged != null) StatsChanged();
            RaiseAlert("Done. " + _save.doneToday + " today, day " + _save.dayStreak + " of your streak", false);
        }

        private void RaiseAlert(string message, bool urgent)
        {
            if (Alert != null) Alert(message, urgent);
        }

        // ---------- persistence ----------

        private void Load()
        {
            _save = new PulseDeskSave();
            try
            {
                if (File.Exists(_savePath))
                {
                    PulseDeskSave loaded = JsonUtility.FromJson<PulseDeskSave>(File.ReadAllText(_savePath));
                    if (loaded != null) _save = loaded;
                    if (_save.tasks == null) _save.tasks = new List<TaskItem>();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("PulseDesk: could not read save, starting fresh. " + e.Message);
                _save = new PulseDeskSave();
            }

            // New day: clear yesterday's Done pile, reset today's count, break the streak if a day was missed.
            string today = Clock().ToString(DateFormat);
            string yesterday = Clock().AddDays(-1).ToString(DateFormat);
            if (_save.lastDoneDate != today)
            {
                _save.tasks.RemoveAll(x => x.lane == TaskLane.Done);
                _save.doneToday = 0;
                if (_save.lastDoneDate != yesterday) _save.dayStreak = 0;
            }
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(_savePath)) return;
            try
            {
                File.WriteAllText(_savePath, JsonUtility.ToJson(_save));
            }
            catch (Exception e)
            {
                Debug.LogWarning("PulseDesk: could not save. " + e.Message);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void SeedOnboarding()
        {
            _save.tasks.Add(new TaskItem { id = "onb1", title = "Pick me up", microStep = "Pinch this card and drop it on DONE", lane = TaskLane.Urgent, durationSeconds = 180f, remainingSeconds = 180f });
            _save.tasks.Add(new TaskItem { id = "onb2", title = "Start a sprint", microStep = "Drag me to URGENT, or swipe your thumb left", lane = TaskLane.Next });
            _save.tasks.Add(new TaskItem { id = "onb3", title = "Make it small", microStep = "Pinch \"Break it down\" and pick a goal", lane = TaskLane.Next });
        }
    }
}
