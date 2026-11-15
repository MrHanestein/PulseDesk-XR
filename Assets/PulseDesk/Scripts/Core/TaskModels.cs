using System;
using System.Collections.Generic;

namespace PulseDesk
{
    public enum TaskLane { Next = 0, Urgent = 1, Done = 2 }

    [Serializable]
    public class TaskItem
    {
        public string id;
        public string title;
        public string microStep;
        public TaskLane lane = TaskLane.Next;
        public float durationSeconds = 300f;
        public float remainingSeconds = 300f;
        public bool alert50;
        public bool alert90;
        public bool alertEnd;
        public long dueTicks;          // 0 = no due time
        public bool dueSoonAlerted;
        public bool dueAlerted;
    }

    [Serializable]
    public class PulseDeskSave
    {
        public List<TaskItem> tasks = new List<TaskItem>();
        public int dayStreak;
        public int doneToday;
        public string lastDoneDate = "";
        public bool onboardingDone;
        public float defaultMinutes = 5f;
    }
}
