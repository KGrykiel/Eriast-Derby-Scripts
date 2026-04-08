using UnityEngine;

namespace Assets.Scripts.UI
{
    internal static class UITextHelpers
    {
        internal static string GetHealthColor(float percent)
        {
            if (percent > 0.6f) return "#44FF44";
            if (percent > 0.3f) return "#FFFF44";
            return "#FF4444";
        }

        internal static string GenerateBar(float percent, int length, bool brackets = false)
        {
            int filled = Mathf.RoundToInt(percent * length);
            filled = Mathf.Clamp(filled, 0, length);

            string bar = brackets ? "[" : "";
            for (int i = 0; i < length; i++)
                bar += i < filled ? "#" : "-";
            if (brackets) bar += "]";

            return bar;
        }
    }
}
