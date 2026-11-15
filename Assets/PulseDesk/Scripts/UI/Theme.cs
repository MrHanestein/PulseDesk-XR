using UnityEngine;

namespace PulseDesk
{
    /// <summary>Colours and labels. Each lane has a colour AND an icon AND a text label.</summary>
    public static class Theme
    {
        // The plate and the back panels are smoked glass: the real desk shows through them.
        public static readonly Color Plate = Glass(0x0B0F16, 0.42f);
        public static readonly Color Panel = Glass(0x10151E, 0.62f);
        public static readonly Color CardBody = Hex(0x252B36);
        public static readonly Color CardHover = Hex(0x39424F);
        public static readonly Color Button = Hex(0x2E3644);
        public static readonly Color ButtonHover = Hex(0x47536A);
        public static readonly Color Rim = Hex(0x465168);
        public static readonly Color PipOff = Hex(0x2A3140);
        public static readonly Color TextMain = Hex(0xE8EAED);
        public static readonly Color TextDim = Hex(0x9AA0A6);
        public static readonly Color Urgent = Hex(0xF2383F);
        public static readonly Color Next = Hex(0xFCC934);
        public static readonly Color Done = Hex(0x81C995);

        public static Color Accent(TaskLane lane)
        {
            switch (lane)
            {
                case TaskLane.Urgent: return Urgent;
                case TaskLane.Done: return Done;
                default: return Next;
            }
        }

        public static string Label(TaskLane lane)
        {
            switch (lane)
            {
                case TaskLane.Urgent: return "URGENT";
                case TaskLane.Done: return "DONE";
                default: return "NEXT";
            }
        }

        public static Color Glass(int rgb, float alpha)
        {
            Color c = Hex(rgb);
            c.a = alpha;
            return c;
        }

        public static Color Hex(int rgb)
        {
            return new Color32((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), 255);
        }
    }
}
