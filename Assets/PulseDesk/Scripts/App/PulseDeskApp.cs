using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PulseDesk
{
    /// <summary>
    /// The only component you add to the scene. It creates the store, the hand input, the dock
    /// and the cards at runtime, so there are no prefabs or Inspector references to wire.
    /// Meta-specific parts are switched on by scripting defines (menu: PulseDesk).
    /// </summary>
    public class PulseDeskApp : MonoBehaviour
    {
        public static PulseDeskApp Instance { get; private set; }

        public TaskStore Store { get; private set; }

        private AudioFeedback _sfx;
        private HandInteractor _interactor;
        private DockPlacer _placer;
        private DockView _dock;
        private CardManager _cards;
        private Transform _head;
        private TextEntry _text;

        private void Awake()
        {
            Instance = this;
            Store = GetComponent<TaskStore>();
            if (Store == null) Store = gameObject.AddComponent<TaskStore>();
            _sfx = gameObject.AddComponent<AudioFeedback>();
            _placer = gameObject.AddComponent<DockPlacer>();
            _text = gameObject.AddComponent<TextEntry>();

            _interactor = gameObject.AddComponent<HandInteractor>();
            _interactor.AddSource(gameObject.AddComponent<MousePointerSource>());

#if PULSEDESK_META
            _interactor.AddSource(gameObject.AddComponent<OvrHandPointerSource>());
            gameObject.AddComponent<OvrSystemHooks>().Init(Store, _placer);
#endif
#if PULSEDESK_META && PULSEDESK_MICROGESTURES
            gameObject.AddComponent<OvrMicrogestures>().Init(Store, _sfx);
#endif
#if PULSEDESK_MRUK
            _placer.AddFinder(gameObject.AddComponent<MrukTableFinder>());
#endif
#if PULSEDESK_DEPTH
            _placer.AddFinder(gameObject.AddComponent<DepthTableFinder>());
#endif
        }

        private void Start()
        {
            _head = FindHead();
            Store.Alert += OnAlert;
            Store.TaskMoved += OnMoved;
            Store.FocusChanged += OnFocusChanged;
            Store.TaskRemoved += OnRemoved;
            Store.Nudge += _sfx.Nudge;
            _placer.DockPlaced += OnDockPlaced;
            _placer.Begin(_head);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (Store != null)
            {
                Store.Alert -= OnAlert;
                Store.TaskMoved -= OnMoved;
                Store.FocusChanged -= OnFocusChanged;
                Store.TaskRemoved -= OnRemoved;
                if (_sfx != null) Store.Nudge -= _sfx.Nudge;
            }
        }

        private static Transform FindHead()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                // Editor desk test with an empty scene.
                GameObject go = new GameObject("PulseDesk Test Camera");
                go.tag = "MainCamera";
                go.transform.position = new Vector3(0f, 1.1f, 0f);
                cam = go.AddComponent<Camera>();
                cam.nearClipPlane = 0.05f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.35f, 0.33f, 0.30f);
                go.AddComponent<AudioListener>();
            }
            return cam.transform;
        }

        private void OnDockPlaced(Transform dockRoot)
        {
            if (_dock == null)
            {
                DockActions actions = new DockActions();
                actions.newTask = NewTask;
                actions.routine = SubmitGoal;
                actions.clearOrUndo = ClearOrUndo;
                actions.clearLabel = ClearLabel;
                actions.recenter = Recenter;
                _dock = DockView.Build(dockRoot, Store, actions);
                _cards = gameObject.AddComponent<CardManager>();
                _cards.Init(Store, _dock);
                _dock.CompactChanged += _cards.Relayout;
                if (Store.OnboardingDone) _dock.ShowBanner(WelcomeBack(), false);
                else _dock.ShowBanner("No controllers needed. Pinch a card to pick it up", false);
            }
            else
            {
                _dock.ShowBanner("Dock moved to where you are looking", false);
                _sfx.SprintStart();
            }
            StartCoroutine(Pop(dockRoot));
        }

        /// <summary>A short grow-in so a re-placed dock is noticed even when it lands in the same spot.</summary>
        private static IEnumerator Pop(Transform target)
        {
            float t = 0f;
            while (t < 0.25f && target != null)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.25f);
                target.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, 1f - (1f - k) * (1f - k));
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }

        private string WelcomeBack()
        {
            int open = Store.Tasks.Count - Store.CountIn(TaskLane.Done);
            TaskItem due = Store.NextDue();
            if (due != null && Store.IsOverdue(due)) return "Welcome back. Overdue: " + due.title;
            if (Store.NewDayRecap != null)
                return (Store.DayStreak > 1 ? Store.DayStreak + " day streak. " : "New day. ") + Store.NewDayRecap;
            string streak = Store.DayStreak > 1 ? "Day " + Store.DayStreak + " in a row. " : "Welcome back. ";
            if (open == 0) return streak + "Nothing waiting";
            return streak + open + (open == 1 ? " task" : " tasks") + " waiting";
        }

        private const float UndoWindow = 12f;
        private float _undoUntil;

        private bool UndoArmed { get { return Store.CanUndo && Time.unscaledTime < _undoUntil; } }

        /// <summary>Deletes the highlighted card. Dropping a card on the Clear button does the same.</summary>
        public void RemoveFocused()
        {
            TaskItem t = Store.Focused;
            if (t != null) Store.Remove(t.id);
        }

        /// <summary>What the Clear button will do if pressed now.</summary>
        public string ClearLabel()
        {
            if (UndoArmed) return "Undo";
            if (Store.CountIn(TaskLane.Done) > 0) return "Clear done";
            return Store.Tasks.Count > 0 ? "Clear all" : "Clear";
        }

        /// <summary>
        /// One button for tidying up: clears Done, or the whole board when Done is already empty,
        /// and for a few seconds afterwards it is the Undo that puts everything back.
        /// </summary>
        public void ClearOrUndo()
        {
            if (UndoArmed)
            {
                _undoUntil = 0f;
                if (Store.Undo())
                {
                    _sfx.Undo();
                    if (_dock != null) _dock.ShowBanner("Restored", false);
                }
                return;
            }

            Store.ForgetUndo();
            int n = Store.CountIn(TaskLane.Done) > 0 ? Store.ClearDone() : Store.ClearAll();
            if (n == 0)
            {
                if (_dock != null) _dock.ShowBanner("Nothing to clear", false);
                return;
            }
            _sfx.Remove();
            _undoUntil = Time.unscaledTime + UndoWindow;
            if (_dock != null) _dock.ShowBanner("Cleared " + n + ". Pinch Undo to bring them back", false);
            CheckOnboarding();
        }

        private void OnRemoved(TaskItem item)
        {
            // A single card was deleted (dropped on Clear). Clearing Done comes through here too and sets its own banner.
            _undoUntil = Time.unscaledTime + UndoWindow;
            if (item.lane == TaskLane.Done) return;
            _sfx.Remove();
            if (_dock != null) _dock.ShowBanner("Removed: " + item.title + ". Pinch Undo to restore", false);
            CheckOnboarding();
        }

        // ---------- commands (also the hook for voice) ----------

        /// <summary>Re-places the dock if it is sitting at the fixed fallback because no surface was known yet.</summary>
        public void RecenterIfFallback()
        {
            if (_placer != null && _placer.LastMethod == "Fixed position") _placer.Recenter();
        }

        /// <summary>"Move dock here".</summary>
        public void Recenter()
        {
            _placer.Recenter();
        }

        /// <summary>
        /// Breaks a goal into up to three cards in Next. Wire a dictation transcript event here
        /// (UnityEvent&lt;string&gt;) to add voice; presets and typed text use the same path.
        /// </summary>
        public void SubmitGoal(string goal)
        {
            List<string> steps = GoalDecomposer.Split(goal);
            if (steps.Count == 0)
            {
                if (_dock != null) _dock.ShowBanner("Did not catch that. Pick a goal instead", false);
                return;
            }
            StartCoroutine(AddSteps(goal, steps));
        }

        /// <summary>"New task": type one task with the system keyboard (hands only, works offline).</summary>
        public void NewTask()
        {
            if (_dock == null || _text.Active) return;
            bool started = _text.Begin("Call the bank at 3pm",
                typed => _dock.ShowBanner("New task: " + typed + "_", false),
                AddTask,
                () => _dock.ShowBanner("Nothing added", false));
            if (started)
            {
                _dock.ShowBanner("Try \"Call the bank at 3pm\" or \"Report 1h 30m\"", false);
            }
            else
            {
                // No keyboard on this device: never a dead end.
                _dock.OpenPicker();
                _dock.ShowBanner("No keyboard here. Pick a routine instead", false);
            }
        }

        /// <summary>
        /// Adds what the user typed or dictated. "at 3pm" or "in 20 min" sets a due time,
        /// "for 25 min" sets the sprint length, and "A then B" or "A; B" becomes several cards.
        /// </summary>
        public void AddTask(string text)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return;
            if (text.Contains(" then ") || text.Contains(";"))
            {
                SubmitGoal(text);
                return;
            }

            QuickTask q = QuickAdd.Parse(text, Store.Clock());
            // Card titles stay short enough to read at a glance; the full text goes underneath.
            bool longTitle = q.title.Length > 40;
            string title = longTitle ? q.title.Substring(0, 39).TrimEnd() + "." : q.title;

            SetSpawnPoint(true);
            TaskItem item = Store.Add(title, longTitle ? q.title : "", q.minutes);
            SetSpawnPoint(false);
            if (q.hasDue) Store.SetDue(item.id, q.due);

            _sfx.Tick();
            if (_dock != null)
            {
                string note = q.hasDue ? ", due " + QuickAdd.FormatDue(q.due, Store.Clock()) : "";
                if (q.minutes > 0f) note += ", " + QuickAdd.FormatMinutes(q.minutes) + " sprint";
                _dock.ShowBanner("Added: " + title + note, false);
            }
            CheckOnboarding();
        }

        private void SetSpawnPoint(bool on)
        {
            if (_cards == null) return;
            if (on && _dock != null && _dock.RoutinesButton != null) _cards.SpawnPoint = _dock.RoutinesButton.transform.position;
            else _cards.SpawnPoint = null;
        }

        private IEnumerator AddSteps(string goal, List<string> steps)
        {
            TaskItem first = null;
            for (int i = 0; i < steps.Count; i++)
            {
                // A step can carry its own sprint length, as in "10m: Review notes".
                QuickTask q = QuickAdd.Parse(steps[i], Store.Clock());
                SetSpawnPoint(true);
                TaskItem item = Store.Add(q.title, goal, q.minutes);
                if (q.hasDue) Store.SetDue(item.id, q.due);
                if (first == null) first = item;
                _sfx.Tick();
                yield return new WaitForSeconds(0.15f);
            }
            SetSpawnPoint(false);

            // Nothing running: the first step starts straight away, so a routine is one tap from pick to work.
            bool started = first != null && Store.ActiveSprint() == null;
            if (started) Store.Move(first.id, TaskLane.Urgent);
            if (_dock != null)
                _dock.ShowBanner(started ? goal + ": started \"" + first.title + "\"" : "Added " + steps.Count + " steps to NEXT: " + goal, false);
            CheckOnboarding();
        }

        // ---------- reactions ----------

        private void OnAlert(string message, bool urgent)
        {
            if (_dock != null) _dock.ShowBanner(message, urgent);
            if (urgent) _sfx.AlertTone();
        }

        private void OnMoved(TaskItem item, TaskLane from)
        {
            if (item.lane == TaskLane.Done) _sfx.Chime();
            else if (item.lane == TaskLane.Urgent) _sfx.SprintStart();
            else _sfx.Tick();
            CheckOnboarding();
        }

        private void OnFocusChanged(TaskItem item)
        {
            // Quiet by design: focus follows hover, so a sound here would fire constantly.
        }

        private void CheckOnboarding()
        {
            if (Store.OnboardingDone || Store.OnboardingCardsOpen()) return;
            Store.MarkOnboardingDone();
            if (_dock != null) _dock.ShowBanner("You are set. Add what your day needs", false);
        }

        // ---------- pause when the app loses focus on device ----------

        private void OnApplicationFocus(bool focused)
        {
            if (Application.isEditor || Store == null) return;
            Store.SetSystemPaused(!focused);
        }

        // ---------- keyboard shortcuts for Editor testing (mirror the thumb gestures) ----------

        private void Update()
        {
            if (!Application.isEditor) return;
            if (_text != null && _text.Active) return;   // typing a task: letters are not shortcuts
            if (DeskInput.KeyDown(DeskKey.Left)) Store.MoveFocused(TaskLane.Urgent);
            if (DeskInput.KeyDown(DeskKey.Right)) Store.MoveFocused(TaskLane.Done);
            if (DeskInput.KeyDown(DeskKey.Up)) Store.CycleFocus(-1);
            if (DeskInput.KeyDown(DeskKey.Down)) Store.CycleFocus(1);
            if (DeskInput.KeyDown(DeskKey.Space)) Store.ToggleUserPause();
            if (DeskInput.KeyDown(DeskKey.B) && _dock != null) _dock.OpenPicker();
            if (DeskInput.KeyDown(DeskKey.R)) Recenter();
        }
    }
}
