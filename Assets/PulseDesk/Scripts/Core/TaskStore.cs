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
        public event Action<TaskItem> TaskRemoved;
        public event Action<TaskItem> TaskChanged;           // due time or sprint length changed
        public event Action Nudge;                           // soft sound: halfway, due soon

        /// <summary>Replace in tests to control "today".</summary>
        public Func<DateTime> Clock = () => DateTime.Now;

        public IReadOnlyList<TaskItem> Tasks { get { return _save.tasks; } }
        public TaskItem Focused { get; private set; }
        public int DayStreak { get { return _save.dayStreak; } }
        public int DoneToday { get { return _save.doneToday; } }
        public bool OnboardingDone { get { return _save.onboardingDone; } }
        public bool IsPaused { get { return _userPaused || _systemPaused; } }
        public bool IsUserPaused { get { return _userPaused; } }
        public float DefaultMinutes { get { return _save.defaultMinutes; } }

        /// <summary>Sprint lengths the - and + buttons step through, in minutes.</summary>
        public static readonly int[] SprintSteps = { 1, 5, 10, 15, 20, 25, 30, 45, 60, 90, 120 };

        /// <summary>On the first launch of a new day: what the last day came to, for the welcome line. Otherwise null.</summary>
        public string NewDayRecap { get; private set; }

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

        private float _nextDueCheck;
        private float _overtime;
        private float _autoNextIn = -1f;
        private string _undoJson;
        private readonly List<TaskItem> _lineScratch = new List<TaskItem>();

        /// <summary>Raised when the whole board was replaced (clear all, undo). Views rebuild their cards.</summary>
        public event Action BoardReset;

        public int FocusMinutesToday { get { return Mathf.FloorToInt(_save.focusSecondsToday / 60f); } }
        public bool CanUndo { get { return !string.IsNullOrEmpty(_undoJson); } }

        /// <summary>Whether the real room dims while a sprint runs. Saved.</summary>
        public bool FocusDim { get { return _save.focusDim; } }

        /// <summary>True while the room should be dimmed: a sprint is running, not paused, time left.</summary>
        public bool FocusActive
        {
            get
            {
                TaskItem t = ActiveSprint();
                return _save.focusDim && t != null && !IsPaused && t.remainingSeconds > 0f;
            }
        }

        public void ToggleFocusDim()
        {
            _save.focusDim = !_save.focusDim;
            Save();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
            AutoFlowTick(Time.unscaledDeltaTime);
            if (Time.unscaledTime >= _nextDueCheck)
            {
                _nextDueCheck = Time.unscaledTime + 1f;
                CheckDue();
            }
        }

        // ---------- flow: the board moves itself forward ----------

        /// <summary>
        /// A few seconds after a sprint is finished, the next card in line starts by itself.
        /// Public so tests can drive time.
        /// </summary>
        public void AutoFlowTick(float dt)
        {
            if (_autoNextIn < 0f) return;
            _autoNextIn -= dt;
            if (_autoNextIn > 0f) return;
            _autoNextIn = -1f;
            if (ActiveSprint() == null) StartNext(false);
        }

        /// <summary>Tasks in a lane in the order they are shown. Next puts the earliest due time first.</summary>
        public void GetLane(TaskLane lane, List<TaskItem> into)
        {
            into.Clear();
            for (int i = 0; i < _save.tasks.Count; i++)
                if (_save.tasks[i].lane == lane) into.Add(_save.tasks[i]);
            if (lane != TaskLane.Next || into.Count < 2) return;

            // Insertion sort keeps equal items in the order they were added.
            for (int i = 1; i < into.Count; i++)
            {
                TaskItem x = into[i];
                long key = x.dueTicks > 0 ? x.dueTicks : long.MaxValue;
                int j = i - 1;
                while (j >= 0 && (into[j].dueTicks > 0 ? into[j].dueTicks : long.MaxValue) > key)
                {
                    into[j + 1] = into[j];
                    j--;
                }
                into[j + 1] = x;
            }
        }

        /// <summary>Starts the first card in Next as the sprint. One tap, no dragging.</summary>
        public bool StartNext(bool sayIfEmpty = true)
        {
            GetLane(TaskLane.Next, _lineScratch);
            if (_lineScratch.Count == 0)
            {
                if (sayIfEmpty) RaiseAlert("Nothing waiting. Pinch New task to add one", false);
                return false;
            }
            Move(_lineScratch[0].id, TaskLane.Urgent);
            return true;
        }

        /// <summary>One-tap finish for the running sprint.</summary>
        public void CompleteSprint()
        {
            TaskItem t = ActiveSprint();
            if (t != null) Move(t.id, TaskLane.Done);
        }

        /// <summary>Gives the running sprint more time without stepping through lengths.</summary>
        public void AddSprintMinutes(int minutes)
        {
            TaskItem t = ActiveSprint();
            if (t == null || minutes <= 0) return;
            t.durationSeconds += minutes * 60f;
            t.remainingSeconds += minutes * 60f;
            float progress = 1f - t.remainingSeconds / Mathf.Max(1f, t.durationSeconds);
            t.alert50 = progress >= 0.5f;
            t.alert90 = progress >= 0.9f;
            t.alertEnd = false;
            _overtime = 0f;
            RaiseAlert("+" + minutes + " min on: " + t.title, false);
            if (TaskChanged != null) TaskChanged(t);
            Save();
        }

        // ---------- undo ----------

        private void Snapshot()
        {
            _undoJson = JsonUtility.ToJson(_save);
        }

        public void ForgetUndo()
        {
            _undoJson = null;
        }

        /// <summary>Puts the cards back as they were before the last remove or clear.</summary>
        public bool Undo()
        {
            if (!CanUndo) return false;
            PulseDeskSave restored = null;
            try { restored = JsonUtility.FromJson<PulseDeskSave>(_undoJson); }
            catch (Exception) { }
            _undoJson = null;
            if (restored == null || restored.tasks == null) return false;

            _save.tasks = restored.tasks;   // cards only: today's count, focus time and streak stay as they are
            _autoNextIn = -1f;
            Focused = FirstOpen();
            if (BoardReset != null) BoardReset();
            if (FocusChanged != null) FocusChanged(Focused);
            if (StatsChanged != null) StatsChanged();
            Save();
            return true;
        }

        /// <summary>Wipes every lane. Undo brings it back.</summary>
        public int ClearAll()
        {
            int n = _save.tasks.Count;
            if (n == 0) return 0;
            Snapshot();
            _save.tasks.Clear();
            _autoNextIn = -1f;
            Focused = null;
            if (BoardReset != null) BoardReset();
            if (FocusChanged != null) FocusChanged(null);
            if (StatsChanged != null) StatsChanged();
            Save();
            return n;
        }

        /// <summary>Raises "in 5 minutes" and "due now" alerts. Public so tests can drive the clock.</summary>
        public void CheckDue()
        {
            DateTime now = Clock();
            for (int i = 0; i < _save.tasks.Count; i++)
            {
                TaskItem t = _save.tasks[i];
                if (t.dueTicks <= 0 || t.lane == TaskLane.Done || t.dueAlerted) continue;
                double minutesLeft = (new DateTime(t.dueTicks) - now).TotalMinutes;
                if (minutesLeft <= 0)
                {
                    t.dueAlerted = true;
                    t.dueSoonAlerted = true;
                    SetFocus(t.id);
                    // Nothing running, or the user paused: the due task takes over by itself.
                    // A running sprint is never interrupted; the card waits at the front of Next.
                    bool takeOver = t.lane == TaskLane.Next && (ActiveSprint() == null || _userPaused);
                    if (takeOver) Move(t.id, TaskLane.Urgent);
                    RaiseAlert((takeOver ? "Due now, started: " : "Due now: ") + t.title, true);
                    if (TaskChanged != null) TaskChanged(t);
                    Save();
                }
                else if (!t.dueSoonAlerted && minutesLeft <= 5.0)
                {
                    t.dueSoonAlerted = true;
                    RaiseAlert("In 5 minutes: " + t.title, false);
                    if (Nudge != null) Nudge();
                    if (TaskChanged != null) TaskChanged(t);
                    Save();
                }
            }
        }

        /// <summary>The open task with the earliest due time, or null.</summary>
        public TaskItem NextDue()
        {
            TaskItem best = null;
            for (int i = 0; i < _save.tasks.Count; i++)
            {
                TaskItem t = _save.tasks[i];
                if (t.dueTicks <= 0 || t.lane == TaskLane.Done) continue;
                if (best == null || t.dueTicks < best.dueTicks) best = t;
            }
            return best;
        }

        public bool IsOverdue(TaskItem t)
        {
            return t != null && t.dueTicks > 0 && t.lane != TaskLane.Done && Clock().Ticks >= t.dueTicks;
        }

        /// <summary>Advances the active sprint. Public so tests can drive time.</summary>
        public void Tick(float dt)
        {
            if (IsPaused) return;
            TaskItem t = ActiveSprint();
            if (t == null) return;
            if (t.remainingSeconds <= 0f)
            {
                // Time is up and the card is still here: remind once a minute until it moves.
                _overtime += dt;
                if (_overtime >= 60f)
                {
                    _overtime = 0f;
                    RaiseAlert("Time is up: " + t.title + ". Pinch Done or +5", true);
                }
                return;
            }
            _overtime = 0f;

            // Time spent in a running sprint is the day's focus time.
            int minutesBefore = FocusMinutesToday;
            _save.focusSecondsToday += Mathf.Min(dt, t.remainingSeconds);
            if (FocusMinutesToday != minutesBefore && StatsChanged != null) StatsChanged();

            t.remainingSeconds = Mathf.Max(0f, t.remainingSeconds - dt);
            float progress = 1f - t.remainingSeconds / Mathf.Max(1f, t.durationSeconds);

            if (!t.alert50 && progress >= 0.5f)
            {
                t.alert50 = true;
                RaiseAlert("Halfway: " + t.title, false);
                if (Nudge != null) Nudge();
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

        public TaskItem Add(string title, string microStep = "", float minutes = 0f, TaskLane lane = TaskLane.Next)
        {
            if (minutes <= 0f) minutes = _save.defaultMinutes;   // 0 = the user's chosen sprint length
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
                if (from == TaskLane.Urgent) _autoNextIn = 3f;   // the next card starts by itself
            }
            else if (to == TaskLane.Urgent)
            {
                RaiseAlert("Sprint started: " + t.title, false);
            }
            Save();
        }

        /// <summary>Gives a task a due time. Alerts fire five minutes before and when it is due.</summary>
        public void SetDue(string id, DateTime due)
        {
            TaskItem t = Get(id);
            if (t == null) return;
            t.dueTicks = due.Ticks;
            t.dueSoonAlerted = false;
            t.dueAlerted = false;
            if (TaskChanged != null) TaskChanged(t);
            Save();
        }

        /// <summary>Deletes a task for good. Does not count as done.</summary>
        public bool Remove(string id)
        {
            TaskItem t = Get(id);
            if (t == null) return false;
            Snapshot();
            bool wasFocused = Focused == t;
            _save.tasks.Remove(t);
            if (wasFocused)
            {
                Focused = FirstOpen();
                if (FocusChanged != null) FocusChanged(Focused);
            }
            if (TaskRemoved != null) TaskRemoved(t);
            Save();
            return true;
        }

        /// <summary>Clears the Done pile. The streak and today's count are kept.</summary>
        public int ClearDone()
        {
            List<TaskItem> done = _save.tasks.FindAll(x => x.lane == TaskLane.Done);
            if (done.Count > 0) Snapshot();
            for (int i = 0; i < done.Count; i++)
            {
                _save.tasks.Remove(done[i]);
                if (TaskRemoved != null) TaskRemoved(done[i]);
            }
            if (done.Count > 0) Save();
            return done.Count;
        }

        /// <summary>
        /// dir = +1 longer, -1 shorter. Changes the running sprint; with no sprint running it
        /// changes the length every not-yet-started card will get.
        /// </summary>
        public void AdjustSprint(int dir)
        {
            TaskItem t = ActiveSprint();
            if (t != null)
            {
                float newDuration = StepMinutes(t.durationSeconds / 60f, dir) * 60f;
                float change = newDuration - t.durationSeconds;
                if (Mathf.Approximately(change, 0f)) return;
                t.durationSeconds = newDuration;
                t.remainingSeconds = Mathf.Max(change > 0f ? 0f : 30f, t.remainingSeconds + change);
                float progress = 1f - t.remainingSeconds / Mathf.Max(1f, t.durationSeconds);
                t.alert50 = progress >= 0.5f;
                t.alert90 = progress >= 0.9f;
                t.alertEnd = t.remainingSeconds <= 0f;
                RaiseAlert("Sprint is now " + Mathf.RoundToInt(newDuration / 60f) + " min", false);
                if (TaskChanged != null) TaskChanged(t);
            }
            else
            {
                float minutes = StepMinutes(_save.defaultMinutes, dir);
                if (Mathf.Approximately(minutes, _save.defaultMinutes)) return;
                _save.defaultMinutes = minutes;
                for (int i = 0; i < _save.tasks.Count; i++)
                {
                    TaskItem x = _save.tasks[i];
                    bool untouched = x.lane == TaskLane.Next && Mathf.Approximately(x.remainingSeconds, x.durationSeconds);
                    if (!untouched) continue;
                    x.durationSeconds = minutes * 60f;
                    x.remainingSeconds = minutes * 60f;
                    if (TaskChanged != null) TaskChanged(x);
                }
                RaiseAlert("New sprints will run " + Mathf.RoundToInt(minutes) + " min", false);
            }
            Save();
        }

        private static float StepMinutes(float current, int dir)
        {
            int[] steps = SprintSteps;
            if (dir > 0)
            {
                for (int i = 0; i < steps.Length; i++)
                    if (steps[i] > current + 0.01f) return steps[i];
                return steps[steps.Length - 1];
            }
            for (int i = steps.Length - 1; i >= 0; i--)
                if (steps[i] < current - 0.01f) return steps[i];
            return steps[0];
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
                    if (_save.defaultMinutes <= 0f) _save.defaultMinutes = 5f;
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
            int lastDone = 0;
            int lastFocus = 0;
            if (_save.lastDoneDate != today)
            {
                lastDone = _save.doneToday;
                _save.tasks.RemoveAll(x => x.lane == TaskLane.Done);
                _save.doneToday = 0;
                if (_save.lastDoneDate != yesterday) _save.dayStreak = 0;
            }
            if (_save.focusDate != today)
            {
                lastFocus = Mathf.FloorToInt(_save.focusSecondsToday / 60f);
                _save.focusDate = today;
                _save.focusSecondsToday = 0f;
            }
            NewDayRecap = lastDone > 0 || lastFocus > 0
                ? "Last time: " + lastDone + " done, " + QuickAdd.FormatMinutes(lastFocus) + " focus"
                : null;
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
            _save.tasks.Add(new TaskItem { id = "onb2", title = "I start by myself", microStep = "When a sprint is done the next card starts. Pinch Done to finish me", lane = TaskLane.Next });
            _save.tasks.Add(new TaskItem { id = "onb3", title = "Add your own", microStep = "Pinch \"New task\". Try: Call the bank at 3pm", lane = TaskLane.Next });
        }
    }
}
