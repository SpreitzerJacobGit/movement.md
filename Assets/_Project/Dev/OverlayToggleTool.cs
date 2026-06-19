using System;
using System.Text;
using MovementMD.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace MovementMD.Dev
{
    /// <summary>Independently toggle each perfect-information overlay (brief §1.3).</summary>
    [AddComponentMenu("MovementMD/Dev/Overlay Toggle Tool")]
    public sealed class OverlayToggleTool : MonoBehaviour, IDevTool
    {
        public string DisplayName => "Perfect-info Overlays";

        private void OnEnable() => DevToolRegistry.Register(this);
        private void OnDisable() => DevToolRegistry.Unregister(this);

        public VisualElement CreateControl()
        {
            var root = new VisualElement { style = { flexDirection = FlexDirection.Column } };
            foreach (OverlayKind kind in Enum.GetValues(typeof(OverlayKind)))
            {
                var captured = kind;
                var toggle = new Toggle(Nicify(kind.ToString())) { value = OverlaySettings.IsEnabled(kind) };
                toggle.RegisterValueChangedCallback(e => OverlaySettings.SetEnabled(captured, e.newValue));
                root.Add(toggle);
            }
            return root;
        }

        // Runtime-safe nicify (avoids UnityEditor.ObjectNames dependency in player builds).
        private static string Nicify(string pascal)
        {
            var sb = new StringBuilder(pascal.Length + 4);
            for (int i = 0; i < pascal.Length; i++)
            {
                char c = pascal[i];
                if (i > 0 && char.IsUpper(c)) sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
