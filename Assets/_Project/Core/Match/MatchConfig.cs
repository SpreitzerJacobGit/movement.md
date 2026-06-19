namespace MovementMD.Core.Match
{
    /// <summary>
    /// Match scoring constants: best-of-3 games, first-to-11 per game, with one mid-game edit
    /// window the first time any side reaches the mid-game threshold.
    /// </summary>
    public static class MatchConfig
    {
        public const int PointsToWinGame = 11;
        public const int MidGameEditThreshold = 6;
        public const int GamesToWinMatch = 2; // best of 3
    }
}
