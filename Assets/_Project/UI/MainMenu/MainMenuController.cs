using MovementMD.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace MovementMD.UI.MainMenu
{
    /// <summary>UI Toolkit main menu: mode select funnel. Visible only in Boot.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private VisualElement _root;

        private void Start()
        {
            Build();
            var flow = AppFlow.Instance;
            if (flow != null)
            {
                flow.ModeChanged += OnModeChanged;
                OnModeChanged(flow.CurrentMode);
            }
        }

        private void OnDestroy()
        {
            if (AppFlow.Instance != null)
                AppFlow.Instance.ModeChanged -= OnModeChanged;
        }

        private void Build()
        {
            var doc = GetComponent<UIDocument>();
            _root = doc.rootVisualElement;
            _root.Clear();
            _root.style.backgroundColor = UITheme.Bg;
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.alignItems = Align.Center;
            _root.style.justifyContent = Justify.Center;
            _root.style.flexGrow = 1;
            _root.style.paddingLeft = 24;
            _root.style.paddingRight = 24;

            var column = new VisualElement { style = { width = 380, flexDirection = FlexDirection.Column } };
            column.Add(UITheme.MakeLabel("movement.md", UITheme.TitleSize, UITheme.Accent));
            column.Add(UITheme.MakeLabel("deterministic movement shooter", UITheme.BodySize, UITheme.Muted));
            column.Add(UITheme.Spacer(28));

            column.Add(ModeButton(GameMode.Singles, "Singles   ·   1v1"));
            column.Add(ModeButton(GameMode.Doubles, "Doubles   ·   2v2"));
            column.Add(UITheme.Spacer(14));
            column.Add(ModeButton(GameMode.Sandbox, "Sandbox"));
            column.Add(ModeButton(GameMode.Training, "Training"));
            column.Add(UITheme.Spacer(28));
            column.Add(QuitButton());
            column.Add(UITheme.Spacer(36));
            column.Add(UITheme.MakeLabel("F1  Developer Menu      ESC  Back to menu", UITheme.BodySize - 2, UITheme.Muted));

            _root.Add(column);
        }

        private static Button ModeButton(GameMode mode, string label)
        {
            var b = UITheme.MakeButton(label, "btn-" + mode);
            b.clicked += () => AppFlow.Instance.RequestMode(mode);
            return b;
        }

        private static Button QuitButton()
        {
            var b = UITheme.MakeButton("Quit", "btn-quit");
            b.clicked += Quit;
            return b;
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnModeChanged(GameMode mode)
        {
            if (_root == null) return;
            _root.style.display = (mode == GameMode.Boot) ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
