using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace PulseDesk.Tests
{
    public class TaskStoreTests
    {
        private GameObject _go;
        private string _path;
        private DateTime _now;

        private TaskStore NewStore()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
            _go = new GameObject("store");
            TaskStore s = _go.AddComponent<TaskStore>();
            s.Clock = () => _now;
            s.Initialize(_path);
            return s;
        }

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Application.temporaryCachePath, "pulsedesk_test_" + Guid.NewGuid().ToString("N") + ".json");
            _now = new DateTime(2026, 11, 16, 10, 0, 0);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
            if (File.Exists(_path)) File.Delete(_path);
        }

        [Test]
        public void FirstRunSeedsThreeStarterCards()
        {
            TaskStore s = NewStore();
            Assert.AreEqual(3, s.Tasks.Count);
            Assert.IsNotNull(s.ActiveSprint());
            Assert.IsTrue(s.OnboardingCardsOpen());
        }

        [Test]
        public void OnlyOneUrgentAtATime()
        {
            TaskStore s = NewStore();
            TaskItem a = s.Add("A");
            TaskItem b = s.Add("B");
            s.Move(a.id, TaskLane.Urgent);
            s.Move(b.id, TaskLane.Urgent);
            Assert.AreEqual(1, s.CountIn(TaskLane.Urgent));
            Assert.AreEqual(TaskLane.Next, a.lane);
            Assert.AreEqual(b, s.ActiveSprint());
        }

        [Test]
        public void DoneIncrementsTodayAndStartsStreak()
        {
            TaskStore s = NewStore();
            TaskItem a = s.Add("A");
            s.Move(a.id, TaskLane.Done);
            Assert.AreEqual(1, s.DoneToday);
            Assert.AreEqual(1, s.DayStreak);
        }

        [Test]
        public void StreakContinuesNextDayAndResetsAfterAGap()
        {
            TaskStore s = NewStore();
            s.Move(s.Add("A").id, TaskLane.Done);

            _now = _now.AddDays(1);
            s = NewStore();
            Assert.AreEqual(0, s.DoneToday);
            Assert.AreEqual(0, s.CountIn(TaskLane.Done), "yesterday's Done pile is cleared");
            s.Move(s.Add("B").id, TaskLane.Done);
            Assert.AreEqual(2, s.DayStreak);

            _now = _now.AddDays(3);
            s = NewStore();
            Assert.AreEqual(0, s.DayStreak);
            s.Move(s.Add("C").id, TaskLane.Done);
            Assert.AreEqual(1, s.DayStreak);
        }

        [Test]
        public void FocusSkipsDoneCards()
        {
            TaskStore s = NewStore();
            for (int i = s.Tasks.Count - 1; i >= 0; i--) s.Move(s.Tasks[i].id, TaskLane.Done);
            TaskItem a = s.Add("A");
            TaskItem b = s.Add("B");
            s.SetFocus(a.id);
            s.CycleFocus(1);
            Assert.AreEqual(b, s.Focused);
            s.CycleFocus(1);
            Assert.AreEqual(a, s.Focused);
        }

        [Test]
        public void SaveAndLoadRoundTrip()
        {
            TaskStore s = NewStore();
            s.Add("Remember me");
            int count = s.Tasks.Count;
            s = NewStore();
            Assert.AreEqual(count, s.Tasks.Count);
            Assert.AreEqual("Remember me", s.Tasks[count - 1].title);
        }

        [Test]
        public void TimerAlertsAndPause()
        {
            TaskStore s = NewStore();
            TaskItem a = s.Add("A", "", 1f);
            s.Move(a.id, TaskLane.Urgent);
            int alerts = 0;
            s.Alert += (m, u) => alerts++;
            s.Tick(31f);
            Assert.AreEqual(1, alerts, "halfway alert");
            s.ToggleUserPause();
            float before = a.remainingSeconds;
            s.Tick(10f);
            Assert.AreEqual(before, a.remainingSeconds, "paused timer does not move");
            s.SetSystemPaused(true);
            s.SetSystemPaused(false);
            s.Tick(10f);
            Assert.AreEqual(before, a.remainingSeconds, "system resume must not undo a user pause");
        }

        [Test]
        public void SplitterHandlesPresetsListsAndSingleGoals()
        {
            Assert.AreEqual(3, GoalDecomposer.Split("Plan my day").Count);
            Assert.AreEqual(3, GoalDecomposer.Split("email Sam, book a room and print notes").Count);
            Assert.AreEqual(3, GoalDecomposer.Split("taxes").Count);
            Assert.AreEqual(0, GoalDecomposer.Split("  ").Count);
        }

        [Test]
        public void RemoveAndClearDone()
        {
            TaskStore s = NewStore();
            TaskItem a = s.Add("A");
            TaskItem b = s.Add("B");
            int before = s.Tasks.Count;
            s.SetFocus(a.id);
            Assert.IsTrue(s.Remove(a.id));
            Assert.AreEqual(before - 1, s.Tasks.Count);
            Assert.AreNotSame(a, s.Focused, "focus leaves a removed card");
            Assert.IsFalse(s.Remove("nope"));

            s.Move(b.id, TaskLane.Done);
            Assert.AreEqual(1, s.ClearDone());
            Assert.AreEqual(0, s.CountIn(TaskLane.Done));
            Assert.AreEqual(1, s.DoneToday, "clearing the pile keeps today's count");
        }

        [Test]
        public void SprintLengthCanBeChanged()
        {
            TaskStore s = NewStore();
            TaskItem sprint = s.ActiveSprint();
            s.Move(sprint.id, TaskLane.Done);

            // No sprint running: changes the default and every card that has not started.
            TaskItem a = s.Add("A");
            s.AdjustSprint(1);
            Assert.AreEqual(10f, s.DefaultMinutes);
            Assert.AreEqual(600f, a.durationSeconds);
            Assert.AreEqual(600f, s.Add("B").durationSeconds);

            // Sprint running: changes that sprint only.
            s.Move(a.id, TaskLane.Urgent);
            s.Tick(60f);
            s.AdjustSprint(1);
            Assert.AreEqual(900f, a.durationSeconds);
            Assert.AreEqual(840f, a.remainingSeconds, 0.01f);
            Assert.AreEqual(10f, s.DefaultMinutes);
            for (int i = 0; i < 10; i++) s.AdjustSprint(-1);
            Assert.AreEqual(60f, a.durationSeconds, "never below one minute");
            Assert.Greater(a.remainingSeconds, 0f);
        }

        [Test]
        public void DueTimesAlertOnceEach()
        {
            TaskStore s = NewStore();
            TaskItem a = s.Add("Call the bank");
            s.SetDue(a.id, _now.AddMinutes(20));
            int urgent = 0, nudges = 0;
            s.Alert += (m, u) => { if (u) urgent++; };
            s.Nudge += () => nudges++;

            s.CheckDue();
            Assert.AreEqual(0, nudges + urgent, "nothing yet");
            Assert.AreSame(a, s.NextDue());

            _now = _now.AddMinutes(16);
            s.CheckDue();
            s.CheckDue();
            Assert.AreEqual(1, nudges, "one heads-up five minutes before");

            _now = _now.AddMinutes(5);
            s.CheckDue();
            s.CheckDue();
            Assert.AreEqual(1, urgent, "one alert when due");
            Assert.IsTrue(s.IsOverdue(a));
            Assert.AreSame(a, s.Focused, "the due card becomes the highlighted one");

            s.Move(a.id, TaskLane.Done);
            Assert.IsNull(s.NextDue());
        }

        [Test]
        public void NextCardStartsByItselfAndDueCardsGoFirst()
        {
            TaskStore s = NewStore();
            TaskItem plain = s.Add("Plain");
            TaskItem timed = s.Add("Timed", "", 10f);
            s.SetDue(timed.id, _now.AddHours(2));

            s.CompleteSprint();
            Assert.IsNull(s.ActiveSprint());
            s.AutoFlowTick(1f);
            Assert.IsNull(s.ActiveSprint(), "a short pause before the next one starts");
            s.AutoFlowTick(2.5f);
            Assert.AreSame(timed, s.ActiveSprint(), "the card with a due time is first in line");
            Assert.AreEqual(600f, timed.remainingSeconds, "it runs for its own length");

            s.Tick(120f);
            Assert.AreEqual(2, s.FocusMinutesToday);
            s.AddSprintMinutes(5);
            Assert.AreEqual(780f, timed.remainingSeconds, 0.01f);
            Assert.IsNotNull(plain);
        }

        [Test]
        public void DueCardTakesOverOnlyWhenNothingIsRunning()
        {
            TaskStore s = NewStore();
            TaskItem a = s.Add("A");
            s.SetDue(a.id, _now.AddMinutes(1));
            _now = _now.AddMinutes(2);
            s.CheckDue();
            Assert.AreEqual(TaskLane.Next, a.lane, "a running sprint is not interrupted");

            TaskItem b = s.Add("B");
            s.SetDue(b.id, _now.AddMinutes(1));
            s.CompleteSprint();
            _now = _now.AddMinutes(2);
            s.CheckDue();
            Assert.AreSame(b, s.ActiveSprint(), "with nothing running the due card starts");
        }

        [Test]
        public void ClearAllAndUndo()
        {
            TaskStore s = NewStore();
            s.Add("A");
            int before = s.Tasks.Count;
            int resets = 0;
            s.BoardReset += () => resets++;

            Assert.AreEqual(before, s.ClearAll());
            Assert.AreEqual(0, s.Tasks.Count);
            Assert.IsTrue(s.CanUndo);
            Assert.IsTrue(s.Undo());
            Assert.AreEqual(before, s.Tasks.Count);
            Assert.IsNotNull(s.ActiveSprint(), "the running sprint comes back too");
            Assert.AreEqual(2, resets);
            Assert.IsFalse(s.Undo(), "one step of undo");

            TaskItem last = s.Tasks[s.Tasks.Count - 1];
            s.Remove(last.id);
            Assert.IsTrue(s.Undo());
            Assert.AreEqual(before, s.Tasks.Count);
        }

        [Test]
        public void RoutineStepsCarryTheirOwnLength()
        {
            System.Collections.Generic.List<string> steps = GoalDecomposer.Split("Deep work 25m");
            Assert.AreEqual(3, steps.Count);
            QuickTask q = QuickAdd.Parse(steps[1], _now);
            Assert.AreEqual(20f, q.minutes);
            Assert.AreEqual("Heads down on the one deliverable", q.title);
            Assert.IsFalse(q.hasDue);
        }

        [Test]
        public void QuickAddReadsTimesAndLengths()
        {
            DateTime now = new DateTime(2026, 11, 16, 10, 0, 0);

            QuickTask q = QuickAdd.Parse("call the bank at 3pm", now);
            Assert.AreEqual("Call the bank", q.title);
            Assert.IsTrue(q.hasDue);
            Assert.AreEqual(new DateTime(2026, 11, 16, 15, 0, 0), q.due);

            q = QuickAdd.Parse("Stand-up at 9:30", now);
            Assert.AreEqual(new DateTime(2026, 11, 17, 9, 30, 0), q.due, "a time already past means tomorrow");

            q = QuickAdd.Parse("stretch in 20 min", now);
            Assert.AreEqual("Stretch", q.title);
            Assert.AreEqual(now.AddMinutes(20), q.due);

            q = QuickAdd.Parse("Write report for 25 min", now);
            Assert.AreEqual("Write report", q.title);
            Assert.AreEqual(25f, q.minutes);
            Assert.IsFalse(q.hasDue);

            q = QuickAdd.Parse("Review slides for 1 hour by 14:00", now);
            Assert.AreEqual("Review slides", q.title);
            Assert.AreEqual(60f, q.minutes);
            Assert.AreEqual(new DateTime(2026, 11, 16, 14, 0, 0), q.due);

            q = QuickAdd.Parse("Look at 3 options", now);
            Assert.IsFalse(q.hasDue, "a bare number mid-sentence is not a time");
            Assert.AreEqual("Look at 3 options", q.title);

            q = QuickAdd.Parse("Lunch at 1", now);
            Assert.AreEqual(new DateTime(2026, 11, 16, 13, 0, 0), q.due, "a bare small hour means the afternoon");

            Assert.AreEqual("15:00", QuickAdd.FormatDue(new DateTime(2026, 11, 16, 15, 0, 0), now));
            Assert.AreEqual("tomorrow 09:30", QuickAdd.FormatDue(new DateTime(2026, 11, 17, 9, 30, 0), now));
        }
    }
}
