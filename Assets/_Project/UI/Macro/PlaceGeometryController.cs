using MovementMD.Core.Macro;
using MovementMD.Core.Match;
using MovementMD.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace MovementMD.UI.Macro
{
    /// <summary>
    /// Between-game / mid-game edit window (UI Toolkit overlay + free-3D mouse placement). Each
    /// side places one piece per window; pieces persist the whole match via <see cref="MacroState"/>.
    /// Free placement: a ghost follows the cursor on the ground plane (y=0); left-click drops.
    /// Remove mode: click a placed piece to delete it.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class PlaceGeometryController : MonoBehaviour
    {
        [SerializeField] private GeometryPalette palette;

        private VisualElement _root;
        private VisualElement _panel;
        private Label _title;
        private Label _sideInfo;
        private VisualElement _typeRow;

        private bool _active;
        private MacroState _macro;
        private int _selectedType;
        private int _activeSide;
        private bool _removeMode;
        private bool[] _placedThisWindow;
        private GameObject _ghost;

        private static readonly Plane GroundPlane = new(Vector3.up, 0f);

        private void Start()
        {
            var doc = GetComponent<UIDocument>();
            _root = doc.rootVisualElement;
            _root.Clear();
            Build();
            _root.style.display = DisplayStyle.None;

            var mc = MatchController.Instance;
            if (mc == null) return;
            mc.PhaseChanged += OnPhaseChanged;
            mc.MatchEnded += OnMatchEnded;
        }

        private void OnDestroy()
        {
            var mc = MatchController.Instance;
            if (mc != null)
            {
                mc.PhaseChanged -= OnPhaseChanged;
                mc.MatchEnded -= OnMatchEnded;
            }
            DestroyGhost();
            _active = false;
        }

        private void Build()
        {
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.alignItems = Align.FlexStart;
            _root.style.paddingTop = 8;
            _root.style.paddingLeft = 8;

            _panel = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = UITheme.Panel,
                    paddingTop = 6, paddingBottom = 6, paddingLeft = 8, paddingRight = 8,
                    borderTopLeftRadius = 6, borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6, borderBottomRightRadius = 6,
                    width = 240,
                }
            };
            _title = UITheme.MakeLabel("Place geometry", 15, UITheme.Accent);
            _panel.Add(_title);

            _sideInfo = UITheme.MakeLabel("", 12, UITheme.Muted);
            _sideInfo.style.marginTop = 2;
            _panel.Add(_sideInfo);

            _typeRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginTop = 4 } };
            _panel.Add(_typeRow);

            var modeRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
            var placeBtn = UITheme.MakeButton("Place"); placeBtn.style.flexGrow = 1;
            var removeBtn = UITheme.MakeButton("Remove"); removeBtn.style.flexGrow = 1;
            placeBtn.clicked += () => { _removeMode = false; Highlight(placeBtn, removeBtn); UpdateGhostColor(); };
            removeBtn.clicked += () => { _removeMode = true; Highlight(placeBtn, removeBtn); DestroyGhost(); };
            Highlight(placeBtn, removeBtn);
            modeRow.Add(placeBtn);
            modeRow.Add(removeBtn);
            _panel.Add(modeRow);

            var switchSide = UITheme.MakeButton("Switch side");
            switchSide.clicked += CycleSide;
            _panel.Add(switchSide);

            var done = UITheme.MakeButton("Done");
            done.clicked += () => MatchController.Instance?.EndEditWindow();
            _panel.Add(done);

            _panel.Add(UITheme.MakeLabel("Click ground to place · click piece to remove", 11, UITheme.Muted));

            _root.Add(_panel);
        }

        private static void Highlight(Button on, Button off)
        {
            on.style.borderBottomWidth = 3;
            off.style.borderBottomWidth = 0;
        }

        private void OnPhaseChanged(MatchPhase phase)
        {
            if (phase == MatchPhase.MidGameEdit || phase == MatchPhase.BetweenGameEdit)
            {
                var mc = MatchController.Instance;
                if (mc != null) Activate(mc.Macro, phase);
            }
            else Deactivate();
        }

        private void OnMatchEnded() => Deactivate();

        private void Activate(MacroState macro, MatchPhase phase)
        {
            _macro = macro;
            _active = true;
            _removeMode = false;
            _activeSide = 0;
            _selectedType = 0;
            var mc = MatchController.Instance;
            int sides = (mc != null && mc.State != null) ? mc.State.NumSides : 2;
            _placedThisWindow = new bool[sides];
            _title.text = phase == MatchPhase.MidGameEdit ? "Edit — mid-game (1 piece each)" : "Edit — between games (1 piece each)";
            BuildTypeButtons();
            UpdateSideInfo();
            EnsureGhost();
            _root.style.display = DisplayStyle.Flex;
        }

        private void Deactivate()
        {
            _active = false;
            _macro = null;
            DestroyGhost();
            _root.style.display = DisplayStyle.None;
        }

        private void BuildTypeButtons()
        {
            _typeRow.Clear();
            if (palette == null) return;
            for (int i = 0; i < palette.Types.Length; i++)
            {
                int captured = i;
                var b = new Button { text = palette.Types[i].Name };
                b.style.flexGrow = 1;
                b.style.height = 30;
                b.style.fontSize = 12;
                b.clicked += () => { _selectedType = captured; EnsureGhost(); };
                _typeRow.Add(b);
            }
        }

        private void CycleSide()
        {
            int n = _placedThisWindow != null ? _placedThisWindow.Length : 2;
            _activeSide = (_activeSide + 1) % n;
            UpdateSideInfo();
            UpdateGhostColor();
        }

        private void UpdateSideInfo()
        {
            if (_sideInfo == null || _placedThisWindow == null) return;
            string who = _activeSide == 0 ? "A" : _activeSide == 1 ? "B" : ("S" + _activeSide);
            bool placed = _placedThisWindow[_activeSide];
            _sideInfo.text = $"Placing for: Side {who}{(placed ? "  (already placed)" : "")}";
        }

        private void Update()
        {
            if (!_active || _macro == null) return;
            var cam = Camera.main;
            var mouse = Mouse.current;
            if (cam == null || mouse == null) return;

            Vector2 mousePos = mouse.position.ReadValue();
            if (IsOverPanel(mousePos)) return; // don't place/remove when clicking the edit UI

            Ray ray = cam.ScreenPointToRay(mousePos);

            if (_removeMode)
            {
                if (mouse.leftButton.wasPressedThisFrame && Physics.Raycast(ray, out var hit, 200f))
                {
                    var view = hit.collider.GetComponentInParent<PlacedPieceView>();
                    if (view != null) _macro.Remove(view.Id);
                }
                return;
            }

            // Place mode: ghost follows the ground plane; left-click drops one piece for this side.
            if (GroundPlane.Raycast(ray, out float enter))
            {
                Vector3 p = ray.GetPoint(enter);
                if (_ghost != null)
                {
                    var type = CurrentType();
                    _ghost.transform.position = new Vector3(p.x, type.Size.y * 0.5f, p.z);
                }
                if (mouse.leftButton.wasPressedThisFrame && _placedThisWindow != null && !_placedThisWindow[_activeSide])
                {
                    _macro.Add(_activeSide, _selectedType, p);
                    _placedThisWindow[_activeSide] = true;
                    UpdateSideInfo();
                }
            }
        }

        // UI Toolkit worldBound is top-left origin; Input System mouse is bottom-left origin → flip Y.
        private bool IsOverPanel(Vector2 screenMouse)
            => _panel.worldBound.Contains(new Vector2(screenMouse.x, Screen.height - screenMouse.y));

        private GeometryType CurrentType()
        {
            if (palette != null && (uint)_selectedType < (uint)palette.Types.Length)
                return palette.Types[_selectedType];
            return new GeometryType { Name = "Block", Shape = GeometryShape.Box, Size = new Vector3(2f, 2f, 2f), Tint = Color.gray };
        }

        private void EnsureGhost()
        {
            if (_removeMode) { DestroyGhost(); return; }
            DestroyGhost();
            var tint = GeometryBuilder.SideTint(_activeSide);
            tint.a = 0.5f;
            _ghost = GeometryBuilder.Create(CurrentType(), tint, createCollider: false);
        }

        private void UpdateGhostColor()
        {
            if (_ghost == null) return;
            var r = _ghost.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                var c = GeometryBuilder.SideTint(_activeSide);
                c.a = 0.5f;
                r.sharedMaterial.color = c;
            }
        }

        private void DestroyGhost()
        {
            if (_ghost != null) { Destroy(_ghost); _ghost = null; }
        }
    }
}
