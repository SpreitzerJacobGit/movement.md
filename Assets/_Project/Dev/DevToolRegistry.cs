using System;
using System.Collections.Generic;

namespace MovementMD.Dev
{
    /// <summary>
    /// Process-wide registry of active <see cref="IDevTool"/> instances. Tools Register in
    /// OnEnable / Unregister in OnDisable; the dev menu rebuilds its panel on <see cref="Changed"/>.
    /// </summary>
    public static class DevToolRegistry
    {
        private static readonly List<IDevTool> s_tools = new();

        public static IReadOnlyList<IDevTool> All => s_tools;

        public static event Action Changed;

        public static void Register(IDevTool tool)
        {
            if (tool != null && !s_tools.Contains(tool))
            {
                s_tools.Add(tool);
                Changed?.Invoke();
            }
        }

        public static void Unregister(IDevTool tool)
        {
            if (s_tools.Remove(tool))
                Changed?.Invoke();
        }
    }
}
