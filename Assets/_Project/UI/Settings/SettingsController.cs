using UnityEngine;
using UnityEngine.UIElements;

namespace MovementMD.UI.Settings
{
    /// <summary>
    /// Settings overlay (UI Toolkit): Display (resolution, fullscreen, quality, vsync) and Audio
    /// (master/sfx/music). Persisted via PlayerPrefs. Master volume applies to AudioListener;
    /// SFX/Music are stored and need an AudioMixer to take effect (later).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SettingsController : MonoBehaviour
    {
        private const string k_Master = "vol_master";
        private const string k_Sfx = "vol_sfx";
        private const string k_Music = "vol_music";
        private const string k_Quality = "quality";
        private const string k_Resolution = "resolution";
        private const string k_Fullscreen = "fullscreen";
        private const string k_VSync = "vsync";

        private VisualElement _root;
        private System.Action _onClosed;

        private void Start()
        {
            var doc = GetComponent<UIDocument>();
            _root = doc.rootVisualElement;
            _root.Clear();
            Build();
            _root.style.display = DisplayStyle.None;
        }

        public void Show(System.Action onClosed = null)
        {
            _onClosed = onClosed;
            _root.style.display = DisplayStyle.Flex;
        }

        private void Close()
        {
            _root.style.display = DisplayStyle.None;
            _onClosed?.Invoke();
            _onClosed = null;
        }

        private void Build()
        {
            _root.style.backgroundColor = UITheme.Bg;
            _root.style.alignItems = Align.Center;
            _root.style.justifyContent = Justify.Center;
            _root.style.flexGrow = 1;

            var col = new VisualElement { style = { width = 440, flexDirection = FlexDirection.Column } };
            col.Add(UITheme.MakeLabel("Settings", 30, UITheme.Accent));
            col.Add(UITheme.Spacer(14));

            col.Add(UITheme.MakeLabel("Display", 15, UITheme.Muted));
            col.Add(ResolutionRow());
            col.Add(ToggleRow("Fullscreen", k_Fullscreen, true, v => Screen.fullScreen = v));
            col.Add(QualityRow());
            col.Add(ToggleRow("V-Sync", k_VSync, true, v => QualitySettings.vSyncCount = v ? 1 : 0));
            col.Add(UITheme.Spacer(10));

            col.Add(UITheme.MakeLabel("Audio", 15, UITheme.Muted));
            col.Add(VolumeRow("Master", k_Master, 1f, v => AudioListener.volume = v));
            col.Add(VolumeRow("SFX", k_Sfx, 1f, null));
            col.Add(VolumeRow("Music", k_Music, 1f, null));
            col.Add(UITheme.Spacer(16));

            var back = UITheme.MakeButton("Back");
            back.clicked += Close;
            col.Add(back);

            _root.Add(col);
            PlayerPrefs.Save();
        }

        private VisualElement ResolutionRow()
        {
            var row = Row();
            row.Add(FieldLabel("Resolution"));
            var dd = new DropdownField { style = { flexGrow = 1 } };
            var res = Screen.resolutions;
            int stored = PlayerPrefs.GetInt(k_Resolution, -1);
            int applyIdx = -1;
            for (int i = 0; i < res.Length; i++)
            {
                dd.choices.Add($"{res[i].width} x {res[i].height}");
                if (applyIdx < 0 && (stored == i || (stored < 0 && res[i].width == Screen.currentResolution.width && res[i].height == Screen.currentResolution.height)))
                    applyIdx = i;
            }
            if (applyIdx < 0 && res.Length > 0) applyIdx = res.Length - 1;
            dd.index = applyIdx >= 0 ? applyIdx : 0;
            dd.RegisterValueChangedCallback(_ => ApplyResolution(dd.index));
            ApplyResolution(dd.index);
            row.Add(dd);
            return row;
        }

        private void ApplyResolution(int idx)
        {
            var res = Screen.resolutions;
            if ((uint)idx >= (uint)res.Length) return;
            Screen.SetResolution(res[idx].width, res[idx].height, Screen.fullScreen);
            PlayerPrefs.SetInt(k_Resolution, idx);
        }

        private VisualElement QualityRow()
        {
            var row = Row();
            row.Add(FieldLabel("Quality"));
            var dd = new DropdownField { style = { flexGrow = 1 } };
            var names = QualitySettings.names;
            foreach (var n in names) dd.choices.Add(n);
            int cur = PlayerPrefs.GetInt(k_Quality, QualitySettings.GetQualityLevel());
            if (cur < 0 || cur >= names.Length) cur = Mathf.Max(0, names.Length - 1);
            dd.index = cur;
            dd.RegisterValueChangedCallback(_ =>
            {
                QualitySettings.SetQualityLevel(dd.index, applyExpensiveChanges: true);
                PlayerPrefs.SetInt(k_Quality, dd.index);
            });
            QualitySettings.SetQualityLevel(cur, applyExpensiveChanges: true);
            row.Add(dd);
            return row;
        }

        private VisualElement ToggleRow(string name, string key, bool def, System.Action<bool> applyEffect)
        {
            var row = Row();
            row.Add(FieldLabel(name));
            bool init = PlayerPrefs.GetInt(key, def ? 1 : 0) == 1;
            var t = new Toggle { value = init };
            t.RegisterValueChangedCallback(e => { PlayerPrefs.SetInt(key, e.newValue ? 1 : 0); applyEffect(e.newValue); });
            applyEffect(init);
            row.Add(t);
            return row;
        }

        private VisualElement VolumeRow(string name, string key, float def, System.Action<float> applyEffect)
        {
            var row = Row();
            row.Add(FieldLabel(name));
            float init = PlayerPrefs.GetFloat(key, def);
            var slider = new Slider(0f, 1f) { value = init, style = { flexGrow = 1 } };
            var readout = new Label(Pct(init)) { style = { width = 44, color = UITheme.Muted, fontSize = 12 } };
            slider.RegisterValueChangedCallback(e =>
            {
                PlayerPrefs.SetFloat(key, e.newValue);
                applyEffect?.Invoke(e.newValue);
                readout.text = Pct(e.newValue);
            });
            applyEffect?.Invoke(init);
            row.Add(slider);
            row.Add(readout);
            return row;
        }

        private static VisualElement Row() => new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 3, alignItems = Align.Center } };
        private static Label FieldLabel(string t) => new Label(t) { style = { width = 110, color = UITheme.Text, fontSize = 13 } };
        private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}
