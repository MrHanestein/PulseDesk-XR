using TMPro;
using UnityEngine;

namespace PulseDesk
{
    /// <summary>One lane on the dock. Cards rest at the slot and stack up and back from it.</summary>
    public class DockLane : MonoBehaviour
    {
        public const int MaxVisible = 4;
        public const float StackUp = 0.022f;
        public const float StackBack = 0.006f;

        public TaskLane lane;
        public Transform slot;
        public TextMeshPro badge;

        public Vector3 StackOffset(int indexFromFront)
        {
            return new Vector3(0f, indexFromFront * StackUp, indexFromFront * StackBack);
        }

        public void SetHiddenCount(int hidden)
        {
            if (badge != null) badge.text = hidden > 0 ? "+" + hidden : "";
        }
    }
}
