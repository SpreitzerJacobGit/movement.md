using MovementMD.Core;
using MovementMD.Core.Match;
using UnityEngine;
using UnityEngine.UIElements;

namespace MovementMD.UI.Match
{
    /// <summary>
    /// In-match scoreboard overlay (UI Toolkit). Two-side points + best-of-3 game pips + phase
    /// label + match-over (Rematch / To Menu). Count-agnostic — iterates the state's sides.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScoreboardController : MonoBehaviour
    {
        private VisualElement _root;
        private VisualElement _panel;
        private Label _topLabel;
        private Label _phaseLabel;
        private VisualElement _sidesRow;
        private VisualElement _matchOverBox;
        private Label _winnerLabel;

        private MatchState _state;
        private bool _bound;

        private void Start()
        {
            var doc = GetComponent<UIDocument>();
            _root = doc.rootVisualElement;
            _root.Clear();
            Build();
            _root.style.display = DisplayStyle.None;

            var mc = MatchController.Instance;
            if (mc == null) return;
            mc.MatchStarted += OnMatchStarted;
            mc.MatchEnded += OnMatchEnded;
            mc.PhaseChanged += OnPhaseChanged;
            if (mc.IsRunning) OnMatchStarted();
        }

        private void OnDestroy()
        {
            var mc = MatchController.Instance;
            if (mc != null)
            {
                mc.MatchStarted -= OnMatchStarted;
                mc.MatchEnded -= OnMatchEnded;
                mc.PhaseChanged -= OnPhaseChanged;
            }
            UnbindState();
        }

        private void Build()
        {
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.alignItems = Align.Center;
            _root.style.paddingTop = 8;

            _panel = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column, alignItems = Align.Center,
                    backgroundColor = UITheme.Panel,
                    paddingTop = 6, paddingBottom = 6, paddingLeft = 14, paddingRight = 14,
                    borderTopLeftRadius = 6, borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6, borderBottomRightRadius = 6,
                }
            };
            _topLabel = UITheme.MakeLabel("", 13, UITheme.Muted);
            _panel.Add(_topLabel);

            _sidesRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
            _panel.Add(_sidesRow);

            _phaseLabel = UITheme.MakeLabel("", 12, UITheme.Accent);
            _phaseLabel.style.marginTop = 4;
            _panel.Add(_phaseLabel);

            _matchOverBox = new VisualElement { style = { flexDirection = FlexDirection.Column, alignItems = Align.Center, marginTop = 8, display = DisplayStyle.None } };
            _winnerLabel = UITheme.MakeLabel("", 18, UITheme.Accent);
            _matchOverBox.Add(_winnerLabel);
            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 6 } };
            var rematch = UITheme.MakeButton("Rematch");
            rematch.clicked += () => MatchController.Instance?.ResetMatch();
            buttons.Add(rematch);
            var toMenu = UITheme.MakeButton("To Menu");
            toMenu.clicked += () => AppFlow.Instance?.RequestMode(GameMode.Boot);
            buttons.Add(toMenu);
            _matchOverBox.Add(buttons);
            _panel.Add(_matchOverBox);

            _root.Add(_panel);
        }

        private void OnMatchStarted()
        {
            var mc = MatchController.Instance;
            if (mc == null || mc.State == null) return;
            BindState(mc.State);
            _root.style.display = DisplayStyle.Flex;
            RebuildSides();
            Render();
        }

        private void OnMatchEnded()
        {
            UnbindState();
            _root.style.display = DisplayStyle.None;
        }

        private void OnPhaseChanged(MatchPhase phase) => Render();

        private void BindState(MatchState state)
        {
            UnbindState();
            _state = state;
            _state.Changed += OnStateChanged;
            _bound = true;
        }

        private void UnbindState()
        {
            if (_bound && _state != null)
                _state.Changed -= OnStateChanged;
            _bound = false;
            _state = null;
        }

        private void OnStateChanged(MatchState state) => Render();

        private void RebuildSides()
        {
            _sidesRow.Clear();
            if (_state == null) return;
            for (int i = 0; i < _state.NumSides; i++)
            {
                if (i > 0)
                {
                    var sep = new Label("vs") { style = { color = UITheme.Muted, alignSelf = Align.Center, marginLeft = 14, marginRight = 14 } };
                    _sidesRow.Add(sep);
                }
                _sidesRow.Add(MakeSideBlock(i));
            }
        }

        private static VisualElement MakeSideBlock(int sideIndex)
        {
            var col = new VisualElement { style = { flexDirection = FlexDirection.Column, alignItems = Align.Center } };
            var name = new Label(SideName(sideIndex)) { style = { color = SideColor(sideIndex), fontSize = 12 } };
            col.Add(name);
            var pts = new Label("0") { name = "pts-" + sideIndex, style = { fontSize = 34, color = UITheme.Text, unityFontStyleAndWeight = FontStyle.Bold } };
            col.Add(pts);
            var pips = new VisualElement { name = "pips-" + sideIndex, style = { flexDirection = FlexDirection.Row, marginTop = 2 } };
            col.Add(pips);
            return col;
        }

        private void Render()
        {
            if (_state == null) return;
            _topLabel.text = $"Game {_state.CurrentGame}  ·  First to {MatchConfig.PointsToWinGame}  ·  Best of {MatchConfig.GamesToWinMatch * 2 - 1}";
            _phaseLabel.text = PhaseText(_state.Phase);
            for (int i = 0; i < _state.NumSides; i++)
            {
                var pts = _panel.Q<Label>("pts-" + i);
                if (pts != null) pts.text = _state.SidePoints[i].ToString();
                var pips = _panel.Q<VisualElement>("pips-" + i);
                if (pips != null) RenderPips(pips, _state.SideGameWins[i]);
            }
            bool over = _state.Phase == MatchPhase.MatchOver;
            _matchOverBox.style.display = over ? DisplayStyle.Flex : DisplayStyle.None;
            if (over) _winnerLabel.text = $"{SideName(_state.WinnerSide)} wins the match";
        }

        private static void RenderPips(VisualElement container, int wins)
        {
            int needed = MatchConfig.GamesToWinMatch;
            for (int i = 0; i < needed; i++)
            {
                VisualElement pip = i < container.childCount ? container.ElementAt(i) : new VisualElement();
                if (i >= container.childCount)
                {
                    pip.style.width = 12;
                    pip.style.height = 12;
                    pip.style.marginLeft = 2;
                    pip.style.marginRight = 2;
                    pip.style.borderTopLeftRadius = 6;
                    pip.style.borderTopRightRadius = 6;
                    pip.style.borderBottomLeftRadius = 6;
                    pip.style.borderBottomRightRadius = 6;
                    container.Add(pip);
                }
                pip.style.backgroundColor = i < wins ? UITheme.Accent : new Color(0.25f, 0.26f, 0.30f);
            }
        }

        private static string SideName(int i) => i == 0 ? "Side A" : i == 1 ? "Side B" : ("Side " + (char)('A' + i));
        private static Color SideColor(int i) => i == 0 ? new Color(0.30f, 0.78f, 1f) : i == 1 ? new Color(1f, 0.62f, 0.30f) : Color.white;

        private static string PhaseText(MatchPhase p) => p switch
        {
            MatchPhase.MidGameEdit => "Edit window — place a piece (mid-game)",
            MatchPhase.BetweenGameEdit => "Edit window — place a piece (between games)",
            MatchPhase.MatchOver => "Match over",
            _ => "",
        };
    }
}
