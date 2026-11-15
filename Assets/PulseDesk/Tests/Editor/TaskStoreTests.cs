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
    }
}
