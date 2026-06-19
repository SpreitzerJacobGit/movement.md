using System;
using System.Collections.Generic;

namespace MovementMD.Core
{
    public readonly struct ModeDefinition
    {
        public GameMode Mode { get; }
        public string DisplayName { get; }
        public string SceneName { get; }
        public int PlayerCount { get; }
        public bool IsMatch { get; }

        public ModeDefinition(GameMode mode, string displayName, string sceneName, int playerCount, bool isMatch)
        {
            Mode = mode;
            DisplayName = displayName;
            SceneName = sceneName;
            PlayerCount = playerCount;
            IsMatch = isMatch;
        }
    }

    /// <summary>
    /// Static mode table. Singles and Doubles intentionally share one <c>Match</c> scene — the
    /// sim is count-agnostic, so only <see cref="ModeDefinition.PlayerCount"/> differs.
    /// </summary>
    public static class ModeConfig
    {
        public const string MatchScene = "Match";
        public const string SandboxScene = "Sandbox";
        public const string TrainingScene = "Training";

        private static readonly Dictionary<GameMode, ModeDefinition> s_definitions = new()
        {
            [GameMode.Boot]     = new(GameMode.Boot,     "Main Menu",      null,         0, false),
            [GameMode.Singles]  = new(GameMode.Singles,  "Singles (1v1)",  MatchScene,   2, true),
            [GameMode.Doubles]  = new(GameMode.Doubles,  "Doubles (2v2)",  MatchScene,   4, true),
            [GameMode.Sandbox]  = new(GameMode.Sandbox,  "Sandbox",        SandboxScene, 1, false),
            [GameMode.Training] = new(GameMode.Training, "Training",       TrainingScene, 1, false),
        };

        public static ModeDefinition Get(GameMode mode)
        {
            if (!s_definitions.TryGetValue(mode, out var def))
                throw new ArgumentException($"Unknown {nameof(GameMode)}: {mode}.", nameof(mode));
            return def;
        }

        public static IEnumerable<ModeDefinition> All => s_definitions.Values;
    }
}
