using System;
using MovementMD.Core.Macro;
using UnityEngine;

namespace MovementMD.Core.Match
{
    /// <summary>
    /// Match lifecycle orchestrator. Creates <see cref="MatchState"/> + <see cref="MacroState"/> on
    /// match-enter, drives phase transitions (incl. edit windows), and tears down on match-exit.
    /// Core-only — has no UI references; UI controllers subscribe to its events. Count-agnostic:
    /// always 2 sides for Singles/Doubles (solo sides vs team sides; the sim's N movers aggregate
    /// into them).
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class MatchController : MonoBehaviour
    {
        public static MatchController Instance { get; private set; }

        public MatchState State { get; private set; }
        public MacroState Macro { get; private set; }
        public bool IsRunning { get; private set; }

        public event Action MatchStarted;
        public event Action MatchEnded;
        public event Action<MatchPhase> PhaseChanged; // relayed from the active MatchState

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[MatchController] Duplicate — Boot must contain exactly one. Destroying duplicate.", this);
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (AppFlow.Instance == null) return;
            AppFlow.Instance.ModeChanged += OnModeChanged;
            OnModeChanged(AppFlow.Instance.CurrentMode);
        }

        private void OnDestroy()
        {
            if (AppFlow.Instance != null)
                AppFlow.Instance.ModeChanged -= OnModeChanged;
            if (Instance == this) Instance = null;
        }

        private void OnModeChanged(GameMode mode)
        {
            bool isMatch = mode == GameMode.Singles || mode == GameMode.Doubles;
            if (isMatch) StartMatch();
            else EndMatch();
        }

        private void StartMatch()
        {
            if (IsRunning) EndMatch();
            State = new MatchState();
            Macro = new MacroState();
            State.PhaseChanged += OnStatePhaseChanged;
            IsRunning = true;
            MatchStarted?.Invoke();
            State.StartMatch(numSides: 2);
        }

        private void EndMatch()
        {
            if (!IsRunning) return;
            IsRunning = false;
            MatchEnded?.Invoke();
            if (State != null)
                State.PhaseChanged -= OnStatePhaseChanged;
            Macro?.Clear();
            State = null;
            Macro = null;
        }

        private void OnStatePhaseChanged(MatchPhase phase) => PhaseChanged?.Invoke(phase);

        public void AwardPointToSide(int sideIndex) => State?.AwardPoint(sideIndex);

        public void EndEditWindow() => State?.ResumeFromEdit();

        public void ResetMatch()
        {
            if (IsRunning) StartMatch();
        }
    }
}
