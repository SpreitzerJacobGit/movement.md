using System;

namespace MovementMD.Core.Match
{
    /// <summary>
    /// Match model. Count-agnostic: scores and game-wins are tracked per side (a list, default 2 —
    /// Singles = 2 solo sides, Doubles = 2 team sides). No player1/player2 anywhere. Drives the
    /// scoreboard via <see cref="Changed"/> and phase transitions via <see cref="PhaseChanged"/>.
    /// </summary>
    public sealed class MatchState
    {
        public int NumSides { get; private set; }
        public int[] SidePoints { get; private set; }   // current-game points per side
        public int[] SideGameWins { get; private set; } // games won per side
        public int CurrentGame { get; private set; }    // 1-based
        public MatchPhase Phase { get; private set; } = MatchPhase.None;
        public int WinnerSide { get; private set; } = -1;
        public bool MidGameEditUsedThisGame { get; private set; }

        public event Action<MatchState> Changed;
        public event Action<MatchPhase> PhaseChanged;

        public void StartMatch(int numSides)
        {
            if (numSides < 2)
                throw new ArgumentException("A match needs at least 2 sides.", nameof(numSides));
            NumSides = numSides;
            SidePoints = new int[numSides];
            SideGameWins = new int[numSides];
            CurrentGame = 1;
            WinnerSide = -1;
            BeginGame();
        }

        /// <summary>Award one point to <paramref name="sideIndex"/>. May trigger an edit window or game/match end.</summary>
        public void AwardPoint(int sideIndex)
        {
            if (Phase != MatchPhase.InGame) return;
            ValidateSide(sideIndex);
            SidePoints[sideIndex]++;

            if (!MidGameEditUsedThisGame && SidePoints[sideIndex] >= MatchConfig.MidGameEditThreshold)
            {
                MidGameEditUsedThisGame = true;
                SetPhase(MatchPhase.MidGameEdit);
                return;
            }

            if (SidePoints[sideIndex] >= MatchConfig.PointsToWinGame)
            {
                SideGameWins[sideIndex]++;
                if (SideGameWins[sideIndex] >= MatchConfig.GamesToWinMatch)
                {
                    WinnerSide = sideIndex;
                    SetPhase(MatchPhase.MatchOver);
                }
                else
                {
                    SetPhase(MatchPhase.BetweenGameEdit);
                }
                return;
            }

            Changed?.Invoke(this);
        }

        /// <summary>Called by the controller when an edit window closes, to resume or advance.</summary>
        public void ResumeFromEdit()
        {
            if (Phase == MatchPhase.MidGameEdit)
            {
                SetPhase(MatchPhase.InGame);
            }
            else if (Phase == MatchPhase.BetweenGameEdit)
            {
                CurrentGame++;
                BeginGame();
            }
        }

        private void BeginGame()
        {
            Array.Clear(SidePoints, 0, SidePoints.Length);
            MidGameEditUsedThisGame = false;
            SetPhase(MatchPhase.InGame);
        }

        private void SetPhase(MatchPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            PhaseChanged?.Invoke(phase);
            Changed?.Invoke(this);
        }

        private void ValidateSide(int sideIndex)
        {
            if ((uint)sideIndex >= (uint)NumSides)
                throw new ArgumentOutOfRangeException(nameof(sideIndex), $"side {sideIndex} out of range [0,{NumSides}).");
        }
    }
}
