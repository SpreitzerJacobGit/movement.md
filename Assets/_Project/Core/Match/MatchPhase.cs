namespace MovementMD.Core.Match
{
    public enum MatchPhase
    {
        None,
        InGame,          // actively playing a game (to 11)
        MidGameEdit,     // edit window: first side just reached the mid-game threshold
        BetweenGameEdit, // edit window after a game ended (someone reached 11)
        MatchOver,
    }
}
