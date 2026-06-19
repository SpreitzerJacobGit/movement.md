using UnityEngine;

namespace MovementMD.Sim
{
    /// <summary>
    /// Default <see cref="ISimContext"/>. Surfaces a clear pending state rather than silent
    /// no-ops — fail fast, never limp forward with the illusion that the sim is running.
    /// </summary>
    public sealed class NullSimContext : ISimContext
    {
        public bool IsAvailable => false;

        public void SpawnDummy() => WarnPending(nameof(SpawnDummy));
        public void ClearDummies() => WarnPending(nameof(ClearDummies));
        public void StartInputRecording() => WarnPending(nameof(StartInputRecording));
        public void StopInputRecording() => WarnPending(nameof(StopInputRecording));
        public void PlayLastRecording() => WarnPending(nameof(PlayLastRecording));

        public DeterminismTestResult RunDeterminismTest()
            => new(false, "Quantum sim host not installed. Run the spike from spike/determinism until ISimContext is wired.");

        private static void WarnPending(string what)
            => Debug.LogWarning($"[Sim] {what} pending Quantum integration (ISimContext = NullSimContext).");
    }
}
