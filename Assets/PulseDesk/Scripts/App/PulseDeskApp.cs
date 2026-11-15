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
                _dock = DockView.Build(dockRoot, Store, Recenter, SubmitGoal, NewTask);
                _cards = gameObject.AddComponent<CardManager>();
                _cards.Init(Store, _dock);
                if (Store.OnboardingDone) _dock.ShowBanner("Welcome back", false);
                else _dock.ShowBanner("No controllers needed. Pinch a card to pick it up", false);
            }
            else
            {
                _dock.ShowBanner("Dock moved", false);
            }
        }

        // ---------- commands (also the hook for voice) ----------

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
            bool started = _text.Begin("Type a task",
                typed => _dock.ShowBanner("New task: " + typed + "_", false),
                AddTask,
                () => _dock.ShowBanner("Nothing added", false));
            if (started)
            {
                _dock.ShowBanner("Type a task, then confirm", false);
            }
            else
            {
                // No keyboard on this device: never a dead end.
                _dock.OpenPicker();
                _dock.ShowBanner("No keyboard here. Pick a goal instead", false);
            }
        }

        /// <summary>Adds what the user typed. "A then B" or "A; B" becomes several cards.</summary>
        public void AddTask(string text)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return;
            if (text.Contains(" then ") || text.Contains(";"))
            {
                SubmitGoal(text);
                return;
            }
            // Card titles stay short enough to read at a glance; the full text goes underneath.
            string title = text.Length > 40 ? text.Substring(0, 39).TrimEnd() + "." : text;
            if (_cards != null && _dock != null && _dock.BreakDownButton != null)
                _cards.SpawnPoint = _dock.BreakDownButton.transform.position;
            Store.Add(char.ToUpper(title[0]) + title.Substring(1), text.Length > 40 ? text : "");
            if (_cards != null) _cards.SpawnPoint = null;
            _sfx.Tick();
            if (_dock != null) _dock.ShowBanner("Added to NEXT: " + title, false);
            CheckOnboarding();
        }

        private IEnumerator AddSteps(string goal, List<string> steps)
        {
            if (_dock != null) _dock.ShowBanner("Breaking down: " + goal, false);
            for (int i = 0; i < steps.Count; i++)
            {
                if (_cards != null && _dock != null && _dock.BreakDownButton != null)
                    _cards.SpawnPoint = _dock.BreakDownButton.transform.position;
                Store.Add(steps[i], goal);
                _sfx.Tick();
                yield return new WaitForSeconds(0.15f);
            }
            if (_cards != null) _cards.SpawnPoint = null;
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
            if (_dock != null) _dock.ShowBanner("You are set. Break a goal down to fill your board", false);
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
