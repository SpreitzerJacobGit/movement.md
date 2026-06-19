using MovementMD.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace MovementMD.Dev
{
    /// <summary>Spawn/clear dummy movers for feel-testing, via <see cref="ISimContext"/>.</summary>
    [AddComponentMenu("MovementMD/Dev/Spawn Dummy Tool")]
    public sealed class SpawnDummyTool : MonoBehaviour, IDevTool
    {
        public string DisplayName => "Spawn / Clear Dummies";

        private void OnEnable() => DevToolRegistry.Register(this);
        private void OnDisable() => DevToolRegistry.Unregister(this);

        public VisualElement CreateControl()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row.Add(new Button(() => SimHost.Current.SpawnDummy()) { text = "Spawn" });
            row.Add(new Button(() => SimHost.Current.ClearDummies()) { text = "Clear all" });
            return row;
        }
    }
}
