using System;
using System.Collections.Generic;

namespace PulseDesk
{
    /// <summary>Offline, rule-based goal splitter. Always available; no network, no model.</summary>
    public static class GoalDecomposer
    {
        private static readonly string[] Separators =
            { " and then ", " then ", ", and ", " and ", ", ", "; " };

        public static List<string> Split(string goal, int max = 3)
        {
            var parts = new List<string>();
            if (string.IsNullOrWhiteSpace(goal)) return parts;

            goal = goal.Trim().TrimEnd('.', '!', '?');

            string[] preset;
            if (GoalPresets.TryGet(goal, out preset))
            {
                parts.AddRange(preset);
            }
            else
            {
                foreach (string piece in goal.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
                {
                    string s = piece.Trim();
                    if (s.Length > 2) parts.Add(char.ToUpper(s[0]) + s.Substring(1));
                }

                if (parts.Count < 2)
                {
                    parts.Clear();
                    parts.Add("Write down what done looks like for: " + goal);
                    parts.Add("Do the first five minutes of: " + goal);
                    parts.Add("Check it and close out: " + goal);
                }
            }

            if (parts.Count > max) parts.RemoveRange(max, parts.Count - max);
            return parts;
        }
    }

    /// <summary>
    /// Routines offered on the dock. Picking one adds its three steps to Next as cards;
    /// it does not do the work for you, so the names are nouns, not commands.
    /// </summary>
    public static class GoalPresets
    {
        public static readonly string[] Titles =
        {
            "Deep work 25m",
            "Rapid triage",
            "Plan my day",
            "Meeting prep"
        };

        // Every step carries its own sprint length ("10m: ..."), read by QuickAdd.
        private static readonly string[][] Steps =
        {
            new[] { "2m: Clear the desk, silence notifications", "20m: Heads down on the one deliverable", "3m: Save work and note the next step" },
            new[] { "2m: Reply to anything under two minutes", "5m: Turn action emails into cards", "2m: Archive what is resolved" },
            new[] { "5m: List everything on your mind", "3m: Pick the three that matter today", "2m: Put a time on each one" },
            new[] { "5m: Read the agenda and last notes", "5m: Write your three points", "2m: Check audio and camera" }
        };

        public static bool TryGet(string goal, out string[] steps)
        {
            for (int i = 0; i < Titles.Length; i++)
            {
                if (string.Equals(Titles[i], goal, StringComparison.OrdinalIgnoreCase))
                {
                    steps = Steps[i];
                    return true;
                }
            }
            steps = null;
            return false;
        }
    }
}
