namespace MovementMD.Core
{
    /// <summary>
    /// Every selectable top-level state the app can be in. The simulation is count-agnostic;
    /// <see cref="Singles"/> and <see cref="Doubles"/> run the same match code at different N.
    /// </summary>
    public enum GameMode
    {
        Boot,
        Singles,
        Doubles,
        Sandbox,
        Training,
    }
}
