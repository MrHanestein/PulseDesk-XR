using System;
using System.Globalization;
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
    /// Reads a typed or dictated line into a title, a due time and a sprint length.
    ///   "Call the bank at 3pm"        due 15:00
    ///   "Stretch in 20 min"           due twenty minutes from now
    ///   "Write report for 25 min"     25 minute sprint
    ///   "Gym 1h 30m"                  90 minute sprint (a length at the end needs no "for")
    ///   "10m: Review notes"           10 minute sprint
    /// Rule-based and offline.
    /// </summary>
    public static class QuickAdd
    {
        private const RegexOptions Opt = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        // A length of time: "45m", "45 min", "2 hours", "1.5h", "1h30m", "1 hr 30", "1 hour and 30 minutes".
        private const string Dur =
            @"(?:(?<h>\d{1,2}(?:\.\d)?)\s*(?:hours?|hrs?|h)(?![a-z])(?:\s*(?:and\s+)?(?<hm>\d{1,2})\s*(?:minutes?|mins?|m)?(?![a-z0-9]))?" +
            @"|(?<m>\d{1,3})\s*(?:minutes?|mins?|m)(?![a-z]))";

        private static readonly Regex PrefixRx = new Regex(@"^\s*" + Dur + @"\s*:\s*", Opt);
        private static readonly Regex InRx = new Regex(@"\bin\s+" + Dur, Opt);
        private static readonly Regex ForRx = new Regex(@"\bfor\s+" + Dur, Opt);
        private static readonly Regex TrailRx = new Regex(@"\s+" + Dur + @"\s*$", Opt);
        // "at 3pm", "at 3:30 pm", "by 15:00". A bare "at 3" only counts at the end of the line.
        private static readonly Regex AtRx = new Regex(@"\b(?:at|by)\s+(\d{1,2})(?:[:.](\d{2}))?\s*(am|pm)?(?=\s|$|[,.!?])", Opt);

        public const float MaxSprintMinutes = 240f;

        public static QuickTask Parse(string text, DateTime now)
        {
            QuickTask q = new QuickTask();
            string rest = (text ?? "").Trim();

            Match m = PrefixRx.Match(rest);
            if (m.Success)
            {
                q.minutes = Clamp(ToMinutes(m), 1f, MaxSprintMinutes);
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

            if (q.minutes <= 0f)
            {
                m = ForRx.Match(rest);
                if (!m.Success) m = TrailRx.Match(rest.TrimEnd(' ', '.', ',', '!', '?'));
                if (m.Success)
                {
                    q.minutes = Clamp(ToMinutes(m), 1f, MaxSprintMinutes);
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

        /// <summary>"45m", "1h", "1h 30m".</summary>
        public static string FormatMinutes(float minutes)
        {
            int total = (int)Math.Round(minutes);
            if (total < 60) return total + "m";
            return total % 60 == 0 ? (total / 60) + "h" : (total / 60) + "h " + (total % 60) + "m";
        }

        private static float ToMinutes(Match m)
        {
            if (m.Groups["h"].Success)
            {
                float hours = float.Parse(m.Groups["h"].Value, CultureInfo.InvariantCulture);
                float extra = m.Groups["hm"].Success ? int.Parse(m.Groups["hm"].Value) : 0f;
                return hours * 60f + extra;
            }
            return int.Parse(m.Groups["m"].Value);
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
