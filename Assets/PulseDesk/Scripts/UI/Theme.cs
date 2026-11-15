using UnityEngine;

namespace PulseDesk
{
    /// <summary>Colours and labels. Each lane has a colour AND an icon AND a text label.</summary>
    public static class Theme
    {
        public static readonly Color Plate = Hex(0x17191C);
        public static readonly Color Panel = Hex(0x1F2226);
        public static readonly Color CardBody = Hex(0x2A2D32);
        public static readonly Color CardHover = Hex(0x3C4047);
        public static readonly Color Button = Hex(0x30343A);
        public static readonly Color ButtonHover = Hex(0x474C54);
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

        public static Color Hex(int rgb)
        {
            return new Color32((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), 255);
        }
    }
}
