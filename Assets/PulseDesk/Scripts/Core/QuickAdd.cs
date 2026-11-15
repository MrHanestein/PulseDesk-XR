using System;
using System.Text.RegularExpressions;

namespace PulseDesk
{
    public struct QuickTask
    {
        public string title;
        public float minutes;     // 0 = use the default sprint length
        public bool hasDue;
        public DateTime due;
    }

    /// <summary>
    /// Reads a typed or dictated line such as "Call the bank at 3pm", "Stretch in 20 min"
    /// or "Write report for 25 min" into a title, a due time and a sprint length.
    /// Rule-based and offline.
    /// </summary>
    public static class QuickAdd
    {
        private const RegexOptions Opt = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        private const string Unit = @"(minutes?|mins?|m|hours?|hrs?|h)";

        private static readonly Regex InRx = new Regex(@"\bin\s+(\d{1,3})\s*" + Unit + @"\b", Opt);
        private static readonly Regex PrefixRx = new Regex(@"^\s*(\d{1,3})\s*" + Unit + @"\s*:\s*", Opt);
        private static readonly Regex ForRx = new Regex(@"\bfor\s+(\d{1,3})\s*" + Unit + @"\b", Opt);
        // "at 3pm", "at 3:30 pm", "by 15:00". A bare "at 3" only counts at the end of the line.
        private static readonly Regex AtRx = new Regex(@"\b(?:at|by)\s+(\d{1,2})(?:[:.](\d{2}))?\s*(am|pm)?(?=\s|$|[,.!?])", Opt);

        public static QuickTask Parse(string text, DateTime now)
        {
            QuickTask q = new QuickTask();
            string rest = (text ?? "").Trim();

            // "10m: Review notes" or "for 25 min" sets the sprint length.
            Match m = PrefixRx.Match(rest);
            if (!m.Success) m = ForRx.Match(rest);
            if (m.Success)
            {
                q.minutes = Clamp(ToMinutes(m), 1f, 180f);
                rest = rest.Remove(m.Index, m.Length);
            }

            m = InRx.Match(rest);
            if (m.Success)
            {
                q.hasDue = true;
                q.due = now.AddMinutes(Clamp(ToMinutes(m), 1f, 24f * 60f));
                rest = rest.Remove(m.Index, m.Length);
            }
            else
            {
                m = AtRx.Match(rest);
                DateTime due;
                if (m.Success && TryClock(m, rest, now, out due))
                {
                    q.hasDue = true;
                    q.due = due;
                    rest = rest.Remove(m.Index, m.Length);
                }
            }

            rest = Regex.Replace(rest, @"\s{2,}", " ").Trim().Trim(',', '.', ';', '-', ' ');
            if (rest.Length == 0) rest = (text ?? "").Trim();
            if (rest.Length > 0) rest = char.ToUpper(rest[0]) + rest.Substring(1);
            q.title = rest;
            return q;
        }

        /// <summary>"15:00", or "tomorrow 09:30" when the day differs.</summary>
        public static string FormatDue(DateTime due, DateTime now)
        {
            string time = due.ToString("HH:mm");
            if (due.Date == now.Date) return time;
            if (due.Date == now.Date.AddDays(1)) return "tomorrow " + time;
            return due.ToString("ddd HH:mm");
        }

        private static float ToMinutes(Match m)
        {
            float n = int.Parse(m.Groups[1].Value);
            return m.Groups[2].Value.ToLowerInvariant().StartsWith("h") ? n * 60f : n;
        }

        private static bool TryClock(Match m, string whole, DateTime now, out DateTime due)
        {
            due = now;
            int hour = int.Parse(m.Groups[1].Value);
            bool hasMinutes = m.Groups[2].Success;
            int minute = hasMinutes ? int.Parse(m.Groups[2].Value) : 0;
            string ampm = m.Groups[3].Success ? m.Groups[3].Value.ToLowerInvariant() : "";

            // "Look at 3 options" is not a time: a bare number must end the line.
            bool atEnd = whole.Substring(m.Index + m.Length).Trim(' ', '.', ',', '!', '?').Length == 0;
            if (ampm.Length == 0 && !hasMinutes && !atEnd) return false;

            if (ampm == "pm" && hour < 12) hour += 12;
            else if (ampm == "am" && hour == 12) hour = 0;
            else if (ampm.Length == 0 && hour >= 1 && hour <= 7) hour += 12;   // "at 3" means the afternoon
            if (hour > 23 || minute > 59) return false;

            due = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
            if (due <= now) due = due.AddDays(1);
            return true;
        }

        private static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }
}
