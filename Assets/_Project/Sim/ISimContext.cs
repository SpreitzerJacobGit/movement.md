namespace MovementMD.Sim
{
    /// <summary>
    /// Boundary the dev tooling talks to. Keeps the dev menu / UI decoupled from the deterministic
    /// host (Photon Quantum), which is not installed yet. Wired through <see cref="SimHost.Current"/>.
    /// </summary>
    public interface ISimContext
    {
        bool IsAvailable { get; }

        void SpawnDummy();
        void ClearDummies();

        DeterminismTestResult RunDeterminismTest();

        void StartInputRecording();
        void StopInputRecording();
        void PlayLastRecording();
    }

    public readonly struct DeterminismTestResult
    {
        public bool Passed { get; }
        public string Summary { get; }

        public DeterminismTestResult(bool passed, string summary)
        {
            Passed = passed;
            Summary = summary ?? string.Empty;
        }
    }
}
