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

    /// <summary>Hand-written breakdowns for the goals offered on the dock.</summary>
    public static class GoalPresets
    {
        public static readonly string[] Titles =
        {
            "Plan my day",
            "Clear my inbox",
            "Prepare a presentation",
            "Tidy my workspace"
        };

        private static readonly string[][] Steps =
        {
            new[] { "List everything on your mind", "Pick the three that matter today", "Put the first one in Urgent" },
            new[] { "Archive what needs no reply", "Answer anything under two minutes", "Turn the rest into task cards" },
            new[] { "Write the one point to remember", "Outline three sections", "Draft the first slide" },
            new[] { "Clear everything off the desk", "Put back only what you use daily", "Bin or file the rest" }
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
