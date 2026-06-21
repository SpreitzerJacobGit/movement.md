namespace Quantum
{
    using UnityEngine;
    using Photon.Deterministic;

    // Explicit local-sim bootstrap for the First Test (Stage 2), used INSTEAD of QuantumRunnerLocalDebug
    // while we diagnose why the debug runner never advances the simulation.
    //
    // Every step logs through UnityEngine.Debug.Log — which goes straight to the Unity Console and is
    // NEVER filtered by Quantum's own log level (that filter, defaulting to Error in-editor, is why our
    // Quantum-side Log.Info/Debug were invisible). This tells us exactly how far game start gets.
    //
    // USAGE: add this component to a GameObject in the scene and DISABLE the QuantumRunnerLocalDebug
    // component on QuantumDebugRunner (so they don't both try to start a game). Then press Play.
    public class RopeSpikeBootstrap : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("[RopeSpike] Bootstrap.Start() reached");

            // §5: attach the mover input poller + debug view so Play gives WASD/mouse/Space input and a
            // visible floor/camera/mover-cube automatically (no editor setup).
            if (GetComponent<MoverInputPoller>() == null) gameObject.AddComponent<MoverInputPoller>();
            if (GetComponent<MoverDebugView>() == null) gameObject.AddComponent<MoverDebugView>();
            if (GetComponent<SandboxTuner>() == null) gameObject.AddComponent<SandboxTuner>();

            var mapdata = FindAnyObjectByType<QuantumMapData>();
            if (mapdata == null)
            {
                Debug.LogError("[RopeSpike] No QuantumMapData in scene — cannot start the simulation.");
                return;
            }
            Debug.Log($"[RopeSpike] MapData found, Map AssetRef = {mapdata.AssetRef}");

            var runtimeConfig = new RuntimeConfig();
            runtimeConfig.Map = mapdata.AssetRef;
            if (QuantumDefaultConfigs.TryGetGlobal(out var defaults))
            {
                runtimeConfig.SimulationConfig = defaults.SimulationConfig;
                Debug.Log("[RopeSpike] SimulationConfig assigned from global defaults");
            }
            else
            {
                Debug.LogWarning("[RopeSpike] QuantumDefaultConfigs.TryGetGlobal returned false");
            }

            var sessionConfig = QuantumDeterministicSessionConfigAsset.DefaultConfig;
            Debug.Log($"[RopeSpike] SessionConfig.UpdateFPS = {sessionConfig.UpdateFPS}");

            // Add a player the moment the game starts — a Local game won't advance frames otherwise.
            QuantumCallback.Subscribe(this, (CallbackGameStarted c) =>
            {
                Debug.Log("[RopeSpike] CallbackGameStarted -> AddPlayer(0)");
                c.Game.AddPlayer(0, new RuntimePlayer());
            });

            var args = new SessionRunner.Arguments
            {
                RunnerFactory  = QuantumRunnerUnityFactory.DefaultFactory,
                GameParameters = QuantumRunnerUnityFactory.CreateGameParameters,
                RuntimeConfig  = runtimeConfig,
                SessionConfig  = sessionConfig,
                GameMode       = DeterministicGameMode.Local,
                RunnerId       = "ROPESPIKE",
                PlayerCount    = 1,
                InitialTick    = 0,
            };

            Debug.Log("[RopeSpike] Calling QuantumRunner.StartGame()...");
            try
            {
                var runner = QuantumRunner.StartGame(args);
                Debug.Log($"[RopeSpike] StartGame returned (runner null? {runner == null})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RopeSpike] QuantumRunner.StartGame threw: {e}");
            }
        }

        // Unity-side per-second probe: reads the verified frame directly and counts Rope components.
        // Uses UnityEngine.Debug.Log so the result cannot be hidden by Quantum's log-level filter, and
        // bypasses the State Inspector entirely. 4 => systems ran and spawned; 0 => systems not running.
        float _probeTimer;
        void Update()
        {
            var f = QuantumRunner.Default?.Game?.Frames?.Verified;
            if (f == null) return;
            _probeTimer += Time.deltaTime;
            if (_probeTimer < 1f) return;
            _probeTimer = 0f;
            int ropes = f.ComponentCount<Rope>(false);
            int movers = f.ComponentCount<Mover>(false);
            string info = "";
            var it = f.Filter<Mover>();
            while (it.Next(out EntityRef e, out Mover mv))
            {
                var t = f.Get<Transform3D>(e);
                // prevJump/grappleHeld echo last tick's inputs; grounded/sink/pos show movement + render state.
                info = $" | pos={t.Position} grounded={mv.Grounded} sink={mv.Sink} prevJump={mv.PrevJump} grappleHeld={mv.GrappleHeld} vel={mv.Velocity}";
                break;
            }
            Debug.Log($"[RopeSpike] frame {f.Number}: ropes={ropes} movers={movers}{info}");
        }
    }
}
