using MovementMD.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace MovementMD.Dev
{
    /// <summary>Jump to any mode or reload the current one — basic dev navigation.</summary>
    [AddComponentMenu("MovementMD/Dev/Scene Loader Tool")]
    public sealed class SceneLoaderTool : MonoBehaviour, IDevTool
    {
        public string DisplayName => "Scene / Mode Loader";

        private void OnEnable() => DevToolRegistry.Register(this);
        private void OnDisable() => DevToolRegistry.Unregister(this);

        public VisualElement CreateControl()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var field = new EnumField(GameMode.Singles) { style = { flexGrow = 1 } };
            row.Add(field);

            var load = new Button(() => AppFlow.Instance.RequestMode((GameMode)field.value)) { text = "Load" };
            row.Add(load);

            var reload = new Button(() => AppFlow.Instance.ReloadCurrentMode()) { text = "Reload" };
            row.Add(reload);

            return row;
        }
    }
}
