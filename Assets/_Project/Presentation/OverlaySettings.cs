using System;

namespace MovementMD.Presentation
{
    public enum OverlayKind
    {
        PositionLight,
        LookCone,
        SpinMeter,
        PlaybackTell,
    }

    /// <summary>
    /// Perfect-information overlay flags (brief §1.3). All ON by default — perfect info is a
    /// deliberate core tenet, never weakened. The presentation layer reads these read-only.
    /// </summary>
    public static class OverlaySettings
    {
        private static readonly int s_count = Enum.GetValues(typeof(OverlayKind)).Length;
        private static readonly bool[] s_enabled = new bool[s_count];

        public static event Action<OverlayKind, bool> Changed;

        static OverlaySettings()
        {
            for (int i = 0; i < s_count; i++) s_enabled[i] = true;
        }

        public static bool IsEnabled(OverlayKind kind) => s_enabled[(int)kind];

        public static void SetEnabled(OverlayKind kind, bool value)
        {
            int i = (int)kind;
            if (s_enabled[i] == value) return;
            s_enabled[i] = value;
            Changed?.Invoke(kind, value);
        }
    }
}
