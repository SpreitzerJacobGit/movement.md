using MovementMD.Core.Match;
using UnityEngine;
using UnityEngine.UIElements;

namespace MovementMD.Dev
{
    /// <summary>
    /// Dev driver for the match flow without a sim: award a point to a side, or reset the match.
    /// In real play, points arrive from sim scoring events instead.
    /// </summary>
    [AddComponentMenu("MovementMD/Dev/Match Scoring Tool")]
    public sealed class MatchScoringTool : MonoBehaviour, IDevTool
    {
        public string DisplayName => "Match Scoring (manual)";

        private void OnEnable() => DevToolRegistry.Register(this);
        private void OnDisable() => DevToolRegistry.Unregister(this);

        public VisualElement CreateControl()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row.Add(new Button(() => MatchController.Instance?.AwardPointToSide(0)) { text = "Point · Side A" });
            row.Add(new Button(() => MatchController.Instance?.AwardPointToSide(1)) { text = "Point · Side B" });
            row.Add(new Button(() => MatchController.Instance?.ResetMatch()) { text = "Reset match" });
            return row;
        }
    }
}
