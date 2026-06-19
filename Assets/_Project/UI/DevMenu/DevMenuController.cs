using MovementMD.Dev;
using MovementMD.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace MovementMD.UI.DevMenu
{
    /// <summary>
    /// F1-toggled developer overlay. Hosts every registered <see cref="IDevTool"/> and reports
    /// the live sim-context status. Lives in Boot so it is available in every mode.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DevMenuController : MonoBehaviour
    {
        private VisualElement _root;
        private VisualElement _toolContainer;
        private Label _simStatus;
        private bool _visible;

        private void Start()
        {
            var doc = GetComponent<UIDocument>();
            _root = doc.rootVisualElement;
            _root.Clear();
            Build();
            SetVisible(false);

            DevToolRegistry.Changed += RebuildTools;
            RebuildTools();
            UpdateSimStatus();
        }

        private void OnDestroy()
        {
            DevToolRegistry.Changed -= RebuildTools;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[Key.F1].wasPressedThisFrame)
                SetVisible(!_visible);
            UpdateSimStatus();
        }

        private void Build()
        {
            _root.style.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 0.93f);
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.width = 400;
            _root.style.minHeight = 200;
            _root.style.paddingTop = 8;
            _root.style.paddingBottom = 8;
            _root.style.paddingLeft = 8;
            _root.style.paddingRight = 8;

            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6 } };
            header.Add(UITheme.MakeLabel("Developer Menu", 20, UITheme.Accent));
            var close = UITheme.MakeButton("Close [F1]", "btn-close");
            close.style.width = 130;
            close.clicked += () => SetVisible(false);
            header.Add(close);
            _root.Add(header);

            _simStatus = UITheme.MakeLabel("sim: …", 12, UITheme.Muted);
            _simStatus.style.marginBottom = 6;
            _root.Add(_simStatus);

            var scroll = new ScrollView { style = { flexGrow = 1, maxHeight = 560 } };
            _toolContainer = new VisualElement { style = { flexDirection = FlexDirection.Column } };
            scroll.Add(_toolContainer);
            _root.Add(scroll);
        }

        private void RebuildTools()
        {
            if (_toolContainer == null) return;
            _toolContainer.Clear();
            var tools = DevToolRegistry.All;
            if (tools.Count == 0)
            {
                _toolContainer.Add(UITheme.MakeLabel("No dev tools registered.", 12, UITheme.Muted));
                return;
            }
            for (int i = 0; i < tools.Count; i++)
                _toolContainer.Add(MakeCard(tools[i]));
        }

        private static VisualElement MakeCard(IDevTool tool)
        {
            var card = new VisualElement
            {
                style =
                {
                    backgroundColor = UITheme.Panel,
                    marginBottom = 6,
                    paddingTop = 6, paddingBottom = 6, paddingLeft = 8, paddingRight = 8,
                    borderTopLeftRadius = 4, borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
                }
            };
            card.Add(UITheme.MakeLabel(tool.DisplayName, 14, UITheme.Accent));
            var body = tool.CreateControl();
            if (body != null) card.Add(body);
            return card;
        }

        private void UpdateSimStatus()
        {
            if (_simStatus == null) return;
            var ctx = SimHost.Current;
            string status = ctx.IsAvailable ? "sim: ONLINE" : "sim: not installed (NullSimContext)";
            if (_simStatus.text != status) _simStatus.text = status;
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            if (_root != null) _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
