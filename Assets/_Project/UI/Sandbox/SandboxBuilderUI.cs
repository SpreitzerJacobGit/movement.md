using System;
using System.Collections.Generic;
using MovementMD.Core.Macro;
using MovementMD.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MovementMD.UI.Sandbox
{
    /// <summary>
    /// Sandbox builder (Stage 2): F toggles a 4-view placement panel — Front(X·Y), Side(Z·Y), Top(X·Z)
    /// ortho views + a perspective preview. Left-click a face view moves a ghost (sets its 2 in-plane
    /// coords from that view's ortho camera); Place commits a piece; right-click a placed piece removes
    /// it. Pieces render with Unity colliders (grapple anchors on them; remove raycasts them).
    ///
    /// NEXT (L3): spawn Quantum colliders for placed pieces so the §5 movement can walk on them.
    /// </summary>
    public sealed class SandboxBuilderUI : MonoBehaviour
    {
        [SerializeField] private GeometryPalette palette;

        private const int ViewSize = 256;
        private const float CubeHalf = 15f;   // arena X/Z span ±15
        private const float CubeH   = 30f;    // arena height (Y 0..30)

        private Camera _topCam, _frontCam, _sideCam, _perspCam;
        private RenderTexture _topRT, _frontRT, _sideRT, _perspRT;
        private GameObject _canvasGo;
        private bool _open;

        private MacroState _macro;
        private readonly Dictionary<Guid, GameObject> _pieces = new();
        private Vector3 _cursor = new Vector3(0, CubeH * 0.5f, 0);
        private int _selectedType;
        private GameObject _ghost;

        private float _logTimer;

        private void Start()
        {
            try
            {
                _macro = new MacroState();
                _macro.PieceAdded += OnPieceAdded;
                _macro.PieceRemoved += OnPieceRemoved;

                CreateViews();
                BuildCanvas();
                RebuildGhost();
                SetOpen(false);
                Debug.Log("[SandboxBuilder] ready — press F to toggle; left-click views to aim, Place to drop, right-click a piece to remove.");
            }
            catch (Exception e)
            {
                Debug.LogError("[SandboxBuilder] Start threw: " + e);
            }
        }

        private void OnDestroy()
        {
            if (_macro != null) { _macro.PieceAdded -= OnPieceAdded; _macro.PieceRemoved -= OnPieceRemoved; }
            ReleaseRT(_topRT); ReleaseRT(_frontRT); ReleaseRT(_sideRT); ReleaseRT(_perspRT);
            if (_canvasGo != null) Destroy(_canvasGo);
            if (_ghost != null) Destroy(_ghost);
            foreach (var kv in _pieces) if (kv.Value != null) Destroy(kv.Value);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[Key.F].wasPressedThisFrame) SetOpen(!_open);
            if (_ghost != null) _ghost.transform.position = _cursor;

            _logTimer += Time.deltaTime;
            if (_logTimer > 2f)
            {
                _logTimer = 0f;
                var kb = Keyboard.current;
                Debug.Log($"[SandboxBuilder] heartbeat kb={(kb != null)} open={_open}");
            }
        }

        private void SetOpen(bool open)
        {
            _open = open;
            Debug.Log("[SandboxBuilder] " + (open ? "open" : "closed"));
            if (_canvasGo != null) _canvasGo.SetActive(open);
            if (_topCam != null)
            {
                _topCam.enabled = open; _frontCam.enabled = open; _sideCam.enabled = open; _perspCam.enabled = open;
            }
            if (_ghost != null) _ghost.SetActive(open);
        }

        // ---- pieces (MacroState mirror; Unity colliders so grapple-aim + remove-raycast hit them) ----
        private void OnPieceAdded(PlacedPiece piece)
        {
            if (_pieces.ContainsKey(piece.Id)) return;
            var type = ResolveType(piece.TypeId);
            var go = GeometryBuilder.Create(type, GeometryBuilder.SideTint(piece.SideIndex), createCollider: true);
            go.transform.position = piece.Position;
            var view = go.AddComponent<PlacedPieceView>();
            view.Id = piece.Id;
            _pieces[piece.Id] = go;
        }

        private void OnPieceRemoved(Guid id)
        {
            if (_pieces.TryGetValue(id, out var go)) { if (go != null) Destroy(go); _pieces.Remove(id); }
        }

        private GeometryType ResolveType(int typeId)
            => (palette != null && (uint)typeId < (uint)palette.Types.Length) ? palette.Types[typeId] : DefaultType();

        private static GeometryType DefaultType()
            => new() { Name = "Block", Shape = GeometryShape.Box, Size = Vector3.one * 2f, Tint = Color.gray };

        private void RebuildGhost()
        {
            if (_ghost != null) Destroy(_ghost);
            var tint = GeometryBuilder.SideTint(0); tint.a = 0.5f;
            _ghost = GeometryBuilder.Create(ResolveType(_selectedType), tint, createCollider: false);
            _ghost.transform.position = _cursor;
            _ghost.SetActive(_open);
        }

        private void SelectType(int i) { _selectedType = i; RebuildGhost(); }
        private void Place() => _macro.Add(0, _selectedType, _cursor);

        // ---- view click handling (called by BuilderViewInput) ----
        public void OnViewClick(ViewFace face, Camera cam, Vector2 uv, PointerEventData.InputButton button)
        {
            if (face == ViewFace.Preview) return;

            if (button == PointerEventData.InputButton.Right)
            {
                Ray ray = cam.ViewportPointToRay(uv);
                if (Physics.Raycast(ray, out var hit, 200f))
                {
                    var view = hit.collider.GetComponentInParent<PlacedPieceView>();
                    if (view != null) _macro.Remove(view.Id);
                }
                return;
            }

            // Left: set the 2 in-plane coords from the ortho camera's world point at the click UV.
            Vector3 wp = cam.ViewportToWorldPoint(new Vector3(uv.x, uv.y, cam.nearClipPlane));
            switch (face)
            {
                case ViewFace.Front: _cursor.x = wp.x; _cursor.y = wp.y; break; // X·Y
                case ViewFace.Side:  _cursor.z = wp.z; _cursor.y = wp.y; break; // Z·Y
                case ViewFace.Top:   _cursor.x = wp.x; _cursor.z = wp.z; break; // X·Z
            }
            _cursor.x = Mathf.Clamp(_cursor.x, -CubeHalf, CubeHalf);
            _cursor.y = Mathf.Clamp(_cursor.y, 0f, CubeH);
            _cursor.z = Mathf.Clamp(_cursor.z, -CubeHalf, CubeHalf);
        }

        // ---- cameras / RTs ----
        private void CreateViews()
        {
            _topRT   = MakeRT(); _frontRT = MakeRT(); _sideRT = MakeRT(); _perspRT = MakeRT();
            _topCam   = MakeCam("Top",     new Vector3(0,  60, 0),            new Vector3(0, 0, 0),            Vector3.forward, true, CubeHalf,      _topRT);
            _frontCam = MakeCam("Front",   new Vector3(0, CubeH * 0.5f, -60), new Vector3(0, CubeH * 0.5f, 0), Vector3.up,      true, CubeH * 0.5f, _frontRT);
            _sideCam  = MakeCam("Side",    new Vector3(-60, CubeH * 0.5f, 0), new Vector3(0, CubeH * 0.5f, 0), Vector3.up,      true, CubeH * 0.5f, _sideRT);
            _perspCam = MakeCam("Preview", new Vector3(26, 26, -26),          new Vector3(0, CubeH * 0.4f, 0),  Vector3.up,      false, 0f,           _perspRT);
        }
        private static RenderTexture MakeRT() { var rt = new RenderTexture(ViewSize, ViewSize, 24) { antiAliasing = 2 }; rt.Create(); return rt; }
        private static Camera MakeCam(string name, Vector3 pos, Vector3 lookAt, Vector3 up, bool ortho, float orthoSize, RenderTexture rt)
        {
            var go = new GameObject("[Builder] " + name);
            var cam = go.AddComponent<Camera>();
            cam.transform.position = pos; cam.transform.LookAt(lookAt, up);
            cam.orthographic = ortho; if (ortho) cam.orthographicSize = orthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.10f, 0.11f, 0.14f);
            cam.targetTexture = rt; cam.enabled = false;
            return cam;
        }
        private static void ReleaseRT(RenderTexture rt) { if (rt != null) { rt.Release(); Destroy(rt); } }

        // ---- canvas (views + type buttons + Place) ----
        private void BuildCanvas()
        {
            _canvasGo = new GameObject("[SandboxBuilder]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = _canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var bgGo = CreateUIGo<Image>("BG", _canvasGo.transform);
            bgGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            Stretch(bgGo.GetComponent<RectTransform>());

            var hdrGo = CreateUIGo<Text>("Header", _canvasGo.transform);
            StyleText(hdrGo.GetComponent<Text>(), "Place Geometry   (F to close · left-click views · right-click piece to remove)", 16, Color.white);
            var hrt = hdrGo.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1); hrt.pivot = new Vector2(0.5f, 1);
            hrt.anchoredPosition = new Vector2(0, -8); hrt.sizeDelta = new Vector2(0, 30);

            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(_canvasGo.transform, false);
            var grt = gridGo.GetComponent<RectTransform>();
            Stretch(grt); grt.offsetMin = new Vector2(20, 70); grt.offsetMax = new Vector2(-20, -50);
            AddView(gridGo.transform, "Top (X·Z)",   _topRT,   _topCam,   ViewFace.Top,   0, 1);
            AddView(gridGo.transform, "Front (X·Y)", _frontRT, _frontCam, ViewFace.Front, 1, 1);
            AddView(gridGo.transform, "Side (Z·Y)",  _sideRT,  _sideCam,  ViewFace.Side,  0, 0);
            AddView(gridGo.transform, "Preview",     _perspRT, _perspCam, ViewFace.Preview, 1, 0);

            BuildBottomBar();
        }

        private void BuildBottomBar()
        {
            var barGo = new GameObject("BottomBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            barGo.transform.SetParent(_canvasGo.transform, false);
            var brt = barGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0); brt.pivot = new Vector2(0.5f, 0);
            brt.anchoredPosition = new Vector2(0, 12); brt.sizeDelta = new Vector2(-40, 40);

            if (palette != null)
            {
                for (int i = 0; i < palette.Types.Length; i++)
                {
                    int captured = i;
                    var b = MakeButton(palette.Types[i].Name, barGo.transform);
                    b.onClick.AddListener(() => SelectType(captured));
                    if (i == 0) Highlight(b);
                }
            }
            var place = MakeButton("PLACE", barGo.transform);
            place.onClick.AddListener(Place);
            Highlight(place);
        }

        private void AddView(Transform parent, string label, RenderTexture rt, Camera cam, ViewFace face, int col, int row)
        {
            var go = new GameObject("View_" + label, typeof(RawImage));
            go.transform.SetParent(parent, false);
            var vrt = go.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(col * 0.5f, row * 0.5f);
            vrt.anchorMax = new Vector2(col * 0.5f + 0.5f, row * 0.5f + 0.5f);
            vrt.offsetMin = new Vector2(4, 22); vrt.offsetMax = new Vector2(-4, -4);
            var img = go.GetComponent<RawImage>();
            img.texture = rt;

            // click handler (left=aim, right=remove)
            var input = go.AddComponent<BuilderViewInput>();
            input.owner = this; input.face = face; input.viewCamera = cam; input.img = img;

            var lblGo = CreateUIGo<Text>("Label", go.transform);
            StyleText(lblGo.GetComponent<Text>(), label, 12, new Color(0.8f, 0.8f, 0.85f));
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(1, 1); lrt.pivot = new Vector2(0.5f, 1);
            lrt.anchoredPosition = new Vector2(0, -2); lrt.sizeDelta = new Vector2(0, 18);
        }

        // ---- uGUI helpers ----
        private static GameObject CreateUIGo<T>(string name, Transform parent) where T : Component
        { var go = new GameObject(name, typeof(T)); go.transform.SetParent(parent, false); return go; }
        private static void Stretch(RectTransform rt)
        { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        private static void StyleText(Text t, string text, int size, Color color)
        { t.text = text; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = size; t.color = color; t.alignment = TextAnchor.MiddleCenter; }
        private static Button MakeButton(string label, Transform parent)
        {
            var go = new GameObject("Btn_" + label, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.color = new Color(0.2f, 0.2f, 0.25f);
            var le = go.AddComponent<LayoutElement>(); le.preferredWidth = 90;
            var txtGo = CreateUIGo<Text>("Text", go.transform);
            var txt = txtGo.GetComponent<Text>(); StyleText(txt, label, 13, Color.white);
            var rt = txtGo.GetComponent<RectTransform>(); Stretch(rt);
            return go.GetComponent<Button>();
        }
        private static void Highlight(Button b)
        { var img = b.GetComponent<Image>(); if (img != null) img.color = new Color(0.35f, 0.6f, 0.9f); }
    }

    public enum ViewFace { Front, Side, Top, Preview }

    public sealed class BuilderViewInput : MonoBehaviour, IPointerClickHandler
    {
        public SandboxBuilderUI owner;
        public ViewFace face;
        public Camera viewCamera;
        public RawImage img;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (owner == null || img == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(img.rectTransform, eventData.position, eventData.pressEventCamera, out var local)) return;
            var r = img.rectTransform.rect;
            Vector2 uv = new((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
            owner.OnViewClick(face, viewCamera, uv, eventData.button);
        }
    }
}
