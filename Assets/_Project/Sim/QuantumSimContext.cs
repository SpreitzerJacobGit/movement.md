using UnityEngine;
using Photon.Deterministic;
using Quantum;
using MovementMD.Core;

namespace MovementMD.Sim
{
    /// <summary>
    /// UI &lt;-&gt; Sim bridge for the §5 Quantum sim. Install in a mode scene (e.g. Sandbox) that contains a
    /// <c>QuantumMapData</c>. On enable it starts a Local Quantum session (player count from
    /// <see cref="AppFlow"/>/<c>ModeConfig</c>), installs itself as <see cref="SimHost.Current"/> so the
    /// Developer Menu tools route to the real sim, and attaches the §5 input poller + debug view. On
    /// disable it shuts the sim down and restores <see cref="NullSimContext"/>.
    ///
    /// The tool members of <see cref="ISimContext"/> (SpawnDummy / RunDeterminismTest / input record) are
    /// stubbed for now — the sim runs and <see cref="IsAvailable"/> is live; the tools light up next.
    /// </summary>
    public sealed class QuantumSimContext : MonoBehaviour, ISimContext
    {
        public bool IsAvailable => QuantumRunner.Default != null && QuantumRunner.Default.Game != null;

        void OnEnable()
        {
            if (!Application.isPlaying) return;   // never run the sim while editing the scene (wizard safe)
            SimHost.Set(this);
            // Attach the §5 view/input unconditionally — even if StartSim aborts (e.g. missing
            // QuantumMapData), the room + cube still render instead of a silent grey screen.
            if (GetComponent<MoverInputPoller>() == null) gameObject.AddComponent<MoverInputPoller>();
            if (GetComponent<MoverDebugView>()  == null) gameObject.AddComponent<MoverDebugView>();
            StartSim();
        }

        void OnDisable()
        {
            if (!Application.isPlaying) return;
            StopSim();
            SimHost.Set(new NullSimContext());
        }

        void StartSim()
        {
            var mapdata = FindAnyObjectByType<QuantumMapData>();
            if (mapdata == null)
            {
                Debug.LogError("[QuantumSimContext] No QuantumMapData in this scene — add one (copy it from the RopeSpike scene) and assign a Map asset.", this);
                return;
            }

            int players = (AppFlow.Instance != null && AppFlow.Instance.CurrentPlayerCount > 0)
                          ? AppFlow.Instance.CurrentPlayerCount : 1;

            var runtimeConfig = new RuntimeConfig { Map = mapdata.AssetRef };
            if (QuantumDefaultConfigs.TryGetGlobal(out var defaults))
                runtimeConfig.SimulationConfig = defaults.SimulationConfig;

            QuantumCallback.Subscribe(this, (CallbackGameStarted c) =>
            {
                for (int p = 0; p < players; p++) c.Game.AddPlayer(p, new RuntimePlayer());
            });

            var args = new SessionRunner.Arguments
            {
                RunnerFactory  = QuantumRunnerUnityFactory.DefaultFactory,
                GameParameters = QuantumRunnerUnityFactory.CreateGameParameters,
                RuntimeConfig  = runtimeConfig,
                SessionConfig  = QuantumDeterministicSessionConfigAsset.DefaultConfig,
                GameMode       = DeterministicGameMode.Local,
                RunnerId       = "MOVEMENTMD",
                PlayerCount    = players,
                InitialTick    = 0,
            };
            Debug.Log($"[QuantumSimContext] starting Quantum sim ({players} player(s)).");
            QuantumRunner.StartGame(args);
        }

        void StopSim()
        {
            var runner = QuantumRunner.Default;
            if (runner != null) runner.Shutdown();   // CONFIRM: QuantumRunner.Shutdown()
        }

        // --- ISimContext tool surface (wired progressively; sim is already running via IsAvailable) ---
        public void SpawnDummy()          => Debug.Log("[QuantumSimContext] SpawnDummy: pending wiring (sim is running).");
        public void ClearDummies()        => Debug.Log("[QuantumSimContext] ClearDummies: pending wiring.");
        public DeterminismTestResult RunDeterminismTest()
            => new(false, "Pending: route the spike/determinism/FIRST_TEST.md harness through the sim.");
        public void StartInputRecording() => Debug.Log("[QuantumSimContext] StartInputRecording: pending wiring.");
        public void StopInputRecording()  => Debug.Log("[QuantumSimContext] StopInputRecording: pending wiring.");
        public void PlayLastRecording()   => Debug.Log("[QuantumSimContext] PlayLastRecording: pending wiring.");
    }
}
