using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace MovementMD.Dev
{
    /// <summary>
    /// Live-tweak the <see cref="Tunables"/> asset at runtime. Edits the in-memory copy; the
    /// Save button persists to disk (editor-only). Float proxies only — the sim consumes FP.
    /// </summary>
    [AddComponentMenu("MovementMD/Dev/Param Tweaker Tool")]
    public sealed class ParamTweakTool : MonoBehaviour, IDevTool
    {
        [SerializeField] private Tunables tunables;

        public string DisplayName => "Live Param Tweaker";

        private void OnEnable() => DevToolRegistry.Register(this);
        private void OnDisable() => DevToolRegistry.Unregister(this);

        public VisualElement CreateControl()
        {
            var root = new VisualElement { style = { flexDirection = FlexDirection.Column } };

            if (tunables == null)
            {
                root.Add(new Label("No Tunables asset assigned.") { style = { color = new Color(1f, 0.6f, 0.6f) } });
                return root;
            }

            var fields = typeof(Tunables).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var f in fields)
            {
                if (f.FieldType != typeof(float)) continue;
                var captured = f;
                var ff = new FloatField(f.Name) { label = f.Name, value = (float)f.GetValue(tunables) };
                ff.RegisterValueChangedCallback(e => captured.SetValue(tunables, e.newValue));
                root.Add(ff);
            }

            var save = new Button(Save) { text = "Save to asset (editor)" };
            save.style.marginTop = 6;
            root.Add(save);
            return root;
        }

        private void Save()
        {
#if UNITY_EDITOR
            if (tunables == null) return;
            UnityEditor.EditorUtility.SetDirty(tunables);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("[Dev] Tunables saved to disk.", this);
#else
            Debug.LogWarning("[Dev] Save is editor-only.", this);
#endif
        }
    }
}
