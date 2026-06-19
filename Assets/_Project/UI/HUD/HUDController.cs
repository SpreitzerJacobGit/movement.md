using MovementMD.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MovementMD.UI.HUD
{
    /// <summary>
    /// Placeholder in-match HUD (uGUI Canvas). Shown whenever a mode is active (not Boot).
    /// Real readouts (crosshair, abstract spin meter, score) plug in when the sim exists.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] private Text modeLabel;

        private Canvas _canvas;

        private void Start()
        {
            _canvas = GetComponent<Canvas>();
            var flow = AppFlow.Instance;
            if (flow == null) return;
            flow.ModeChanged += OnModeChanged;
            OnModeChanged(flow.CurrentMode);
        }

        private void OnDestroy()
        {
            if (AppFlow.Instance != null)
                AppFlow.Instance.ModeChanged -= OnModeChanged;
        }

        private void OnModeChanged(GameMode mode)
        {
            bool show = mode != GameMode.Boot;
            _canvas.enabled = show;
            if (modeLabel != null)
            {
                var def = ModeConfig.Get(mode);
                modeLabel.text = $"{def.DisplayName}   ·   {def.PlayerCount}p   ·   ESC = menu";
            }
        }
    }
}
