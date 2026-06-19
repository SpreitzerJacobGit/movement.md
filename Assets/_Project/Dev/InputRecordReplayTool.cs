using MovementMD.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace MovementMD.Dev
{
    /// <summary>
    /// Capture and replay an input sequence via <see cref="ISimContext"/> — the core of the
    /// repro-bug workflow (a deterministic sim replays recorded inputs bit-identically).
    /// </summary>
    [AddComponentMenu("MovementMD/Dev/Input Record Replay Tool")]
    public sealed class InputRecordReplayTool : MonoBehaviour, IDevTool
    {
        public string DisplayName => "Input Record / Replay";

        private void OnEnable() => DevToolRegistry.Register(this);
        private void OnDisable() => DevToolRegistry.Unregister(this);

        public VisualElement CreateControl()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row.Add(new Button(() => SimHost.Current.StartInputRecording()) { text = "Record" });
            row.Add(new Button(() => SimHost.Current.StopInputRecording()) { text = "Stop" });
            row.Add(new Button(() => SimHost.Current.PlayLastRecording()) { text = "Play" });
            return row;
        }
    }
}
