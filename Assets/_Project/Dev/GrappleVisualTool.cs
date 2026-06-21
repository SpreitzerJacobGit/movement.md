using MovementMD.Presentation;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

namespace MovementMD.Dev
{
    /// <summary>
    /// Live-tweak the grapple rope's look: color, glow, and the width function along the rope.
    /// Edits the <see cref="GrappleVisualSettings"/> asset in memory; Save persists to disk
    /// (editor-only). "Show demo rig" spawns a mock grapple so the look is visible before the sim
    /// is wired. Presentation only — never touches sim state.
    /// </summary>
    [AddComponentMenu("MovementMD/Dev/Grapple Visual Tool")]
    public sealed class GrappleVisualTool : MonoBehaviour, IDevTool
    {
        [SerializeField] private GrappleVisualSettings settings;

        public string DisplayName => "Grapple Visual";

        private void OnEnable() => DevToolRegistry.Register(this);
        private void OnDisable() => DevToolRegistry.Unregister(this);

        public VisualElement CreateControl()
        {
            var root = new VisualElement { style = { flexDirection = FlexDirection.Column } };

            if (settings == null)
            {
                root.Add(new Label("No GrappleVisualSettings asset assigned.")
                {
                    style = { color = new Color(1f, 0.6f, 0.6f) }
                });
                return root;
            }

            root.Add(Header("Color & Glow"));
            root.Add(ColorRow("Core Color", settings.CoreColor, c => { settings.CoreColor = c; settings.NotifyChanged(); }));
            root.Add(ColorRow("Glow Color", settings.GlowColor, c => { settings.GlowColor = c; settings.NotifyChanged(); }));
            root.Add(Slider("Glow Intensity", settings.GlowIntensity, 0f, 1f, v => { settings.GlowIntensity = v; settings.NotifyChanged(); }));

            root.Add(Header("Width (function along rope)"));
            root.Add(Slider("Core Width", settings.CoreWidth, 0.01f, 0.5f, v => { settings.CoreWidth = v; settings.NotifyChanged(); }));
            root.Add(Slider("Halo Width", settings.HaloWidth, 0.05f, 1.5f, v => { settings.HaloWidth = v; settings.NotifyChanged(); }));

#if UNITY_EDITOR
            var curve = new CurveField("Width Curve") { value = settings.WidthCurve };
            curve.RegisterValueChangedCallback(e => { settings.WidthCurve = e.newValue; settings.NotifyChanged(); });
            root.Add(curve);
            root.Add(PresetRow(curve));
#else
            root.Add(new Label("Width curve editing is editor-only.")
            {
                style = { color = new Color(1f, 0.6f, 0.6f) }
            });
#endif

            root.Add(Header("Geometry"));
            root.Add(IntSlider("Corner Vertices", settings.CornerVertices, 0, 16, v => { settings.CornerVertices = v; settings.NotifyChanged(); }));
            root.Add(IntSlider("Cap Vertices", settings.CapVertices, 0, 16, v => { settings.CapVertices = v; settings.NotifyChanged(); }));

            var showBtn = new Button(ToggleDemo) { text = "Show demo rig" };
            showBtn.style.marginTop = 8;
            root.Add(showBtn);

#if UNITY_EDITOR
            var saveBtn = new Button(Save) { text = "Save to asset (editor)" };
            saveBtn.style.marginTop = 4;
            root.Add(saveBtn);
#endif
            return root;
        }

        // ---- demo rig ---------------------------------------------------------------------

        private void ToggleDemo()
        {
            var existing = FindAnyObjectByType<GrappleLineRenderer>();
            if (existing != null)
            {
                Destroy(existing.gameObject);
                return;
            }
            SpawnDemo();
        }

        private void SpawnDemo()
        {
            var rigGo = new GameObject("[GrappleVisualDemo]");
            var rig = rigGo.transform;

            var playerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerGo.name = "PlayerEnd";
            playerGo.transform.SetParent(rig, false);
            playerGo.transform.localScale = Vector3.one * 0.4f;
            playerGo.transform.position = new Vector3(0f, 1f, 0f);
            TintPrimitive(playerGo, new Color(0.3f, 0.8f, 1f));

            var anchorGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            anchorGo.name = "AnchorEnd";
            anchorGo.transform.SetParent(rig, false);
            anchorGo.transform.localScale = Vector3.one * 0.4f;
            anchorGo.transform.position = new Vector3(4f, 3f, 0f);
            TintPrimitive(anchorGo, new Color(1f, 0.7f, 0.3f));

            var demo = rigGo.AddComponent<GrappleVisualDemo>();
            demo.Configure(playerGo.transform, anchorGo.transform);

            var renderer = rigGo.AddComponent<GrappleLineRenderer>();
            renderer.SetSettings(settings);
            renderer.SetSource(demo);
        }

        private static void TintPrimitive(GameObject go, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            if (go.TryGetComponent<Renderer>(out var r)) r.sharedMaterial = mat;
        }

        // ---- save -------------------------------------------------------------------------

        private void Save()
        {
#if UNITY_EDITOR
            if (settings == null) return;
            UnityEditor.EditorUtility.SetDirty(settings);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("[Dev] GrappleVisualSettings saved to disk.", this);
#else
            Debug.LogWarning("[Dev] Save is editor-only.", this);
#endif
        }

        // ---- UI helpers -------------------------------------------------------------------

        private static VisualElement Header(string text) =>
            new Label(text) { style = { marginTop = 6, unityFontStyleAndWeight = FontStyle.Bold } };

        private static VisualElement ColorRow(string label, Color value, System.Action<Color> onSet)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var field = new ColorField(label) { value = value };
            field.RegisterValueChangedCallback(e => onSet(e.newValue));
            row.Add(field);
            return row;
        }

        private static VisualElement Slider(string label, float value, float min, float max, System.Action<float> onSet)
        {
            var slider = new Slider(min, max) { label = label, value = value, showInputField = true };
            slider.RegisterValueChangedCallback(e => onSet(e.newValue));
            return slider;
        }

        private static VisualElement IntSlider(string label, int value, int min, int max, System.Action<int> onSet)
        {
            var slider = new SliderInt(min, max) { label = label, value = value, showInputField = true };
            slider.RegisterValueChangedCallback(e => onSet(e.newValue));
            return slider;
        }

#if UNITY_EDITOR
        private VisualElement PresetRow(CurveField curve)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 2 } };
            row.Add(PresetButton("Taper→Anchor", AnimationCurve.Linear(0f, 1f, 1f, 0.2f), curve));
            row.Add(PresetButton("Constant", AnimationCurve.Linear(0f, 1f, 1f, 1f), curve));
            row.Add(PresetButton("Funnel", new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, 0.9f),
                new Keyframe(1f, 0.05f)), curve));
            return row;
        }

        private Button PresetButton(string label, AnimationCurve preset, CurveField curve)
        {
            var btn = new Button(() =>
            {
                settings.WidthCurve = preset;
                curve.value = preset;
                settings.NotifyChanged();
            }) { text = label };
            btn.style.marginRight = 4;
            return btn;
        }
#endif
    }
}
