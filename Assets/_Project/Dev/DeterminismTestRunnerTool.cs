using MovementMD.Sim;
using MovementMD.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace MovementMD.Dev
{
    /// <summary>
    /// One-click access to the First Test (run-to-run + rollback re-sim bit-diff). Goes through
    /// <see cref="ISimContext"/>; reports the stub status until Quantum is wired.
    /// </summary>
    [AddComponentMenu("MovementMD/Dev/Determinism Test Runner Tool")]
    public sealed class DeterminismTestRunnerTool : MonoBehaviour, IDevTool
    {
        public string DisplayName => "Determinism Test Runner";

        private Label _result;

        private void OnEnable() => DevToolRegistry.Register(this);
        private void OnDisable() => DevToolRegistry.Unregister(this);

        public VisualElement CreateControl()
        {
            var root = new VisualElement { style = { flexDirection = FlexDirection.Column } };
            var run = new Button(Run) { text = "Run First Test" };
            root.Add(run);
            _result = new Label("not run") { style = { color = UITheme.Muted, whiteSpace = WhiteSpace.Normal, marginTop = 4 } };
            root.Add(_result);
            return root;
        }

        private void Run()
        {
            var r = SimHost.Current.RunDeterminismTest();
            _result.text = (r.Passed ? "PASS — " : "FAIL — ") + r.Summary;
            _result.style.color = r.Passed ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
        }
    }
}
