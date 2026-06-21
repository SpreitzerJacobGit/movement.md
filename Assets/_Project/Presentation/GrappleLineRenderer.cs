using UnityEngine;
using UnityEngine.Rendering;

namespace MovementMD.Presentation
{
    /// <summary>
    /// Renders a grapple rope as a glowing, width-tapered line connecting each physical point
    /// (brief §1.3 — grapples are visible physical ropes). Two stacked <see cref="LineRenderer"/>s
    /// fake glow without a post-processing dependency: a bright core over a wider additive halo.
    /// Width along the rope is driven by <see cref="GrappleVisualSettings.WidthCurve"/> — wider
    /// toward the player end by default. Presentation only: reads an <see cref="IGrapplePointSource"/>,
    /// never writes sim state.
    /// </summary>
    public sealed class GrappleLineRenderer : MonoBehaviour
    {
        [SerializeField] private GrappleVisualSettings settings;

        private LineRenderer _core;
        private LineRenderer _halo;
        private IGrapplePointSource _source;
        private bool _subbed;

        public void SetSource(IGrapplePointSource source) => _source = source;

        public void SetSettings(GrappleVisualSettings s)
        {
            if (_subbed && settings != null) settings.Changed -= Refresh;
            settings = s;
            _subbed = false;
            if (settings != null) { settings.Changed += Refresh; _subbed = true; }
            Refresh();
        }

        private void Awake()
        {
            BuildLine(ref _core, "Core", additive: false);
            BuildLine(ref _halo, "Halo", additive: true);
            if (_source == null)
            {
                _source = GetComponent<IGrapplePointSource>()
                          ?? GetComponentInParent<IGrapplePointSource>()
                          ?? GetComponentInChildren<IGrapplePointSource>();
            }
            if (settings != null) { settings.Changed += Refresh; _subbed = true; }
        }

        private void OnEnable() => Refresh();

        private void OnDisable()
        {
            if (_core != null) _core.enabled = false;
            if (_halo != null) _halo.enabled = false;
        }

        private void OnDestroy()
        {
            if (_subbed && settings != null) settings.Changed -= Refresh;
            if (_core != null) Destroy(_core.gameObject);
            if (_halo != null) Destroy(_halo.gameObject);
        }

        private void LateUpdate()
        {
            // Pull points every frame so moving sources (demo, sim) track live. Cheap for a short chain.
            if (_core != null) ApplyPoints();
        }

        private void BuildLine(ref LineRenderer line, string childName, bool additive)
        {
            var go = new GameObject("GrappleLine_" + childName);
            go.transform.SetParent(transform, false);
            line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.allowOcclusionWhenDynamic = false;
            line.textureMode = LineTextureMode.Stretch;
            line.sortingPriority = additive ? -1 : 0;
            line.sharedMaterial = MakeMaterial(additive);
        }

        private static Material MakeMaterial(bool additive)
        {
            // URP Unlit when available; fall back to legacy Unlit. Blend set via material properties.
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Unlit/Color")
                            ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.SetFloat("_Surface", 1f); // 1 = transparent on URP/Unlit
            mat.SetFloat("_Blend", additive ? 2f : 0f); // 2 = additive, 0 = alpha
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", additive ? (int)BlendMode.One : (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_Cull", (int)CullMode.Off);
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }

        private void ApplyPoints()
        {
            if (settings == null || _source == null || !_source.IsActive)
            {
                if (_core.enabled) _core.enabled = false;
                if (_halo.enabled) _halo.enabled = false;
                return;
            }

            var points = _source.GetPoints();
            if (points == null || points.Length < 2)
            {
                if (_core.enabled) _core.enabled = false;
                if (_halo.enabled) _halo.enabled = false;
                return;
            }

            _core.enabled = true;
            _halo.enabled = true;
            _core.positionCount = points.Length;
            _halo.positionCount = points.Length;
            _core.SetPositions(points);
            _halo.SetPositions(points);
        }

        private void Refresh()
        {
            if (_core == null || _halo == null || settings == null) return;

            _core.widthCurve = settings.WidthCurve;
            _core.widthMultiplier = settings.CoreWidth;
            _core.numCornerVertices = settings.CornerVertices;
            _core.numCapVertices = settings.CapVertices;
            _core.startColor = settings.CoreColor;
            _core.endColor = settings.CoreColor;

            float glow = Mathf.Clamp01(settings.GlowIntensity);
            _halo.widthCurve = settings.WidthCurve;
            _halo.widthMultiplier = settings.HaloWidth * (0.4f + glow); // glow expands the halo
            _halo.numCornerVertices = settings.CornerVertices;
            _halo.numCapVertices = settings.CapVertices;
            Color haloCol = settings.GlowColor;
            haloCol.a = Mathf.Clamp01(glow); // alpha modulates additive intensity / transparency
            _halo.startColor = haloCol;
            _halo.endColor = haloCol;
        }
    }
}
