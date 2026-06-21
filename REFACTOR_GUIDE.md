# Refactoring Guide: Code-First Architecture with Unity as Dumb Renderer

> **Goal:** Convert the existing Unity + Quantum project into a code-first architecture where the simulation layer is pure C#/.NET with comprehensive testing, and Unity serves only as a presentation/rendering layer.

> **Target Audience:** AI agent executing this refactoring
> **Estimated Effort:** 2-3 weeks
> **Risk Level:** Medium (preserves existing validated determinism, requires careful boundary management)

---

## Architecture Overview

### Before Refactoring
```
Unity Project (mixed concerns)
├── Quantum ECS (deterministic sim)
├── Unity Components (presentation + game logic)
├── MonoBehaviours (scene lifecycle, input handling)
└── Determinism tests (in Unity play mode)
```

### After Refactoring
```
/src
  /Simulation                    # PURE C#/.NET, NO Unity dependencies
    /Core                       # ECS, determinism, frame loop
    /Systems                    # All game systems (Movement, Grapple, Spin, Offense)
    /Networking                 # Rollback, input prediction, server authority
    /Math                       # Fixed-point utilities (Quantum FP wrappers)
    /Tests                      # HEADLESS tests, 10k runs in seconds
      /Determinism              # FIRST_TEST expansion
      /Systems                  # Unit tests per system
      /Integration              # Multi-system interaction tests
  /Presentation                  # Unity project, THIN renderer only
    /Scripts
      /SimBridge                # Reads sim state, writes to Unity transforms
      /Rendering                # Visuals only (meshes, materials, particles)
      /Input                    # Captures input, sends to sim
      /UI                       # HUD, menus (read-only sim state)
    /Scenes                     # Visual layout, NO game logic
    /Assets                     # Art, audio, prefabs (visuals only)
```

### Key Principle
**The Simulation/ directory is the source of truth. Unity/Presentation/ is a read-only view into it.**

---

## Phase 1: Establish the Pure C# Simulation Layer (Week 1)

### 1.1 Create the Simulation/ Project Structure

Create a new .NET project that will contain the pure simulation code:

```bash
# Create new directory structure
mkdir -p src/Simulation/{Core,Systems,Networking,Math,Tests/{Determinism,Systems,Integration}}
mkdir -p src/Presentation/{Scripts/{SimBridge,Rendering,Input,UI},Scenes,Assets}
```

Create `src/Simulation/Simulation.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>false</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <!-- Add references to Quantum DLLs once extracted -->
    <!-- <Reference Include="PhotonQuantum.dll" /> -->
  </ItemGroup>
</Project>
```

Create `src/Simulation/Tests/Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Simulation.csproj" />
  </ItemGroup>
</Project>
```

### 1.2 Extract and Port Core Simulation Components

#### Step 1: Port the Determinism Spike to Simulation/

Move and adapt the existing determinism spike:

```bash
# Copy the spike code
cp spike/determinism/*.cs src/Simulation/Tests/Determinism/
```

Adapt the spike to reference the new simulation structure (remove any Unity-specific dependencies):

**File: `src/Simulation/Tests/Determinism/RopeSolver.cs`**
- Remove any `UnityEngine` or `UnityEditor` references
- Ensure all math uses Quantum's `FP`/`FPVector3` types
- Keep the core algorithm identical (it already passed!)

#### Step 2: Create the Simulation Core Interface

**File: `src/Simulation/Core/ISimulation.cs`**

```csharp
namespace Simulation.Core;

/// <summary>
/// Pure simulation interface - no Unity dependencies.
/// All operations are deterministic and headless.
/// </summary>
public interface ISimulation
{
    /// <summary>
    /// Advance the simulation by one fixed timestep (128 Hz).
    /// </summary>
    void Tick();

    /// <summary>
    /// Get current simulation state for rendering (read-only).
    /// </summary>
    SimulationState GetState();

    /// <summary>
    /// Apply player inputs for the current tick.
    /// </summary>
    void ApplyInput(PlayerInputs inputs);

    /// <summary>
    /// Snapshot current state for rollback (must be bit-serializable).
    /// </summary>
    byte[] Snapshot();

    /// <summary>
    /// Restore from a snapshot for rollback re-simulation.
    /// </summary>
    void Restore(byte[] snapshot);
}

/// <summary>
/// Read-only simulation state for presentation layer.
/// Contains only what the renderer needs to know.
/// </summary>
public readonly struct SimulationState
{
    public readonly int Tick;
    public readonly MoverState[] Movers;
    public readonly RopeState[] Ropes;
    // Add other render-relevant state here
}
```

**File: `src/Simulation/Core/Simulation.cs`**

```csharp
using Quantum;
using Simulation.Systems;

namespace Simulation.Core;

/// <summary>
/// Pure deterministic simulation implementation.
/// Runs headless, no Unity dependencies.
/// </summary>
public sealed class Simulation : ISimulation
{
    private readonly Session _session; // Quantum session
    private int _tick;

    public Simulation(SessionConfig config)
    {
        _session = Session.Create(config);
        _tick = 0;
    }

    public void Tick()
    {
        _session.Update();
        _tick++;
    }

    public SimulationState GetState()
    {
        // Extract render-relevant state from Quantum frame
        var frame = _session.Frames.Prev;
        var movers = new MoverState[frame.PlayerCount];
        var ropes = new RopeState[frame.DynamicObjects.Count];

        // Populate from Quantum entities
        for (int i = 0; i < frame.PlayerCount; i++)
        {
            // Extract mover state from Quantum entity
            movers[i] = ExtractMoverState(frame, i);
        }

        return new SimulationState
        {
            Tick = _tick,
            Movers = movers,
            Ropes = ropes
        };
    }

    public void ApplyInput(PlayerInputs inputs)
    {
        // Send inputs to Quantum session
        // Quantum handles input buffering and prediction
    }

    public byte[] Snapshot()
    {
        // Quantum provides frame serialization
        return _session.Frames.Prev.Serialize();
    }

    public void Restore(byte[] snapshot)
    {
        // Quantum provides frame deserialization
        // Used for rollback re-simulation
    }

    private MoverState ExtractMoverState(Frame frame, int playerIndex)
    {
        // Extract position, rotation, velocity, etc. from Quantum entity
        // Return pure struct (no Quantum types leak out)
        return default;
    }
}
```

### 1.3 Port Game Systems to Pure C#

For each system in the existing Quantum codebase:

1. **Locate the existing Quantum system** (e.g., `RopeSolverSystem.cs` in Assets/QuantumUser/Simulation/)
2. **Copy to `src/Simulation/Systems/`**
3. **Remove Unity dependencies** (no `Debug.Log`, no `UnityEngine` references)
4. **Ensure all types are Quantum or pure C#** (no Unity types)

**Example System Structure:**

**File: `src/Simulation/Systems/RopeSolver.cs`**

```csharp
using Quantum;

namespace Simulation.Systems;

/// <summary>
/// Stiff coupled-spring rope solver with rope-rope collision.
/// Deterministic, fixed-point math only.
/// </summary>
public sealed class RopeSolver
{
    // Keep your existing validated algorithm here!
    // The core logic from your FIRST_TEST is already pure.

    public void Resolve(ref Frame frame)
    {
        // Your existing rope collision solver logic
        // Uses FP/FPVector3 for fixed-point determinism
    }
}
```

### 1.4 Create Comprehensive Test Coverage

**File: `src/Simulation/Tests/Determinism/DeterminismTests.cs`**

```csharp
using Xunit;
using Simulation.Core;

namespace Simulation.Tests.Determinism;

/// <summary>
/// Headless determinism tests - run in milliseconds, no Unity required.
/// Expand your existing FIRST_TEST pattern to cover all systems.
/// </summary>
public class DeterminismTests
{
    [Fact]
    public void RopeCollision_DeterministicAcrossRuns()
    {
        // Port your existing 10k-run test
        // Should run in < 1 second headless
    }

    [Fact]
    public void RopeCollision_DeterministicAcrossRollback()
    {
        // Port your rollback re-sim test
        // Should run in < 1 second headless
    }

    [Fact]
    public void MovementSystem_Deterministic()
    {
        // Test sprint/slide/jump/wall-jump/wall-run
    }

    [Fact]
    public void SpinSystem_Deterministic()
    {
        // Test angle integration, inverse-speed, order-independence
    }

    [Fact]
    public void FullSimulation_Deterministic()
    {
        // Run full sim for 1000 ticks, hash final state
        // Run 100 times, verify all identical
    }
}
```

**File: `src/Simulation/Tests/Systems/SystemTests.cs`**

```csharp
using Xunit;
using Simulation.Systems;

namespace Simulation.Tests.Systems;

/// <summary>
/// Unit tests for individual systems.
/// Test invariants and edge cases.
/// </summary>
public class SystemTests
{
    [Fact]
    public void SpinStacking_OrderIndependent()
    {
        // Verify A then B == B then A
    }

    [Fact]
    public void RopeCollision_IDSortedOrder()
    {
        // Verify collision pairs resolved by ID, not iteration order
    }

    [Fact]
    public void Movement_QuantizedInputs()
    {
        // Verify identical mouse motion produces identical spin
        // (test angle integration, not velocity)
    }
}
```

### 1.5 Verify Test Execution

```bash
cd src/Simulation/Tests
dotnet test

# Expected output: All tests pass in < 5 seconds
# Your existing FIRST_TEST should run 10x faster headless
```

---

## Phase 2: Create the Unity Presentation Bridge (Week 1-2)

### 2.1 Create the SimBridge Component

**File: `src/Presentation/Scripts/SimBridge/SimulationBridge.cs`**

```csharp
using UnityEngine;
using Simulation.Core;

namespace Presentation.SimBridge;

/// <summary>
/// Bridge between pure simulation and Unity renderer.
/// READS simulation state, WRITES to Unity transforms.
/// NEVER writes to simulation state.
/// </summary>
public sealed class SimulationBridge : MonoBehaviour
{
    private ISimulation _simulation;

    [Header("Render Mapping")]
    [SerializeField] private MoverRenderer[] _moverRenderers;
    [SerializeField] private RopeRenderer[] _ropeRenderers;

    private void Awake()
    {
        // Initialize pure simulation (no Unity dependencies)
        var config = new SessionConfig
        {
            UpdateFPS = 128, // From your validated tick rate
            // ... other config
        };

        _simulation = new Simulation(config);
    }

    private void Update()
    {
        // Tick the simulation at fixed rate
        // Unity's Update runs at variable frame rate, but simulation ticks at 128 Hz
        // Use a fixed timestep accumulator to maintain determinism
        TickSimulation();

        // Read simulation state and update Unity transforms
        RenderSimulationState();
    }

    private void TickSimulation()
    {
        // Fixed timestep accumulator pattern
        // Ensures simulation ticks deterministically regardless of Unity's frame rate
        // Implementation details below...
    }

    private void RenderSimulationState()
    {
        var state = _simulation.GetState();

        // Update mover visual transforms (READ-ONLY)
        for (int i = 0; i < state.Movers.Length; i++)
        {
            if (i < _moverRenderers.Length)
            {
                _moverRenderers[i].UpdateFromState(state.Movers[i]);
            }
        }

        // Update rope visual meshes (READ-ONLY)
        for (int i = 0; i < state.Ropes.Length; i++)
        {
            if (i < _ropeRenderers.Length)
            {
                _ropeRenderers[i].UpdateFromState(state.Ropes[i]);
            }
        }
    }

    // Public API for input system to send inputs to sim
    public void SendPlayerInputs(PlayerInputs inputs)
    {
        _simulation.ApplyInput(inputs);
    }
}
```

**File: `src/Presentation/Scripts/SimBridge/MoverRenderer.cs`**

```csharp
using UnityEngine;
using Simulation.Core;

namespace Presentation.SimBridge;

/// <summary>
/// Renders a mover's visual state.
/// READS from simulation state, WRITES to Unity transform.
/// </summary>
public sealed class MoverRenderer : MonoBehaviour
{
    [SerializeField] private Transform _transform;

    public void UpdateFromState(MoverState state)
    {
        // Map simulation state to Unity transform
        _transform.position = new Vector3(
            (float)state.Position.X,
            (float)state.Position.Y,
            (float)state.Position.Z
        );

        _transform.rotation = Quaternion.Euler(
            (float)state.Rotation.X,
            (float)state.Rotation.Y,
            (float)state.Rotation.Z
        );

        // Update other visual properties (meshes, materials, particles)
        // Based on simulation state (velocity, spin meter, etc.)
    }
}

/// <summary>
/// Pure C# state struct from simulation (no Unity types).
/// </summary>
public readonly struct MoverState
{
    public readonly FPVector3 Position;
    public readonly FPVector3 Rotation;
    public readonly FPVector3 Velocity;
    public readonly FPSpin Spin;
    // Add other render-relevant state
}

/// <summary>
/// Spin state for visual meter.
/// </summary>
public readonly struct FPSpin
{
    public readonly FP StoredAngle;
    public readonly FP DischargeRate;
    // Add other spin properties
}
```

### 2.2 Create the Input Capture Component

**File: `src/Presentation/Scripts/Input/InputCapture.cs`**

```csharp
using UnityEngine;
using Simulation.Core;

namespace Presentation.Input;

/// <summary>
/// Captures Unity input and forwards to simulation.
/// READS from Unity input, WRITES to simulation (only via ApplyInput).
/// </summary>
public sealed class InputCapture : MonoBehaviour
{
    [SerializeField] private SimulationBridge _simulationBridge;

    private void Update()
    {
        var inputs = CaptureInputs();
        _simulationBridge.SendPlayerInputs(inputs);
    }

    private PlayerInputs CaptureInputs()
    {
        return new PlayerInputs
        {
            Movement = new Vector2(
                Input.GetAxis("Horizontal"),
                Input.GetAxis("Vertical")
            ),
            Aim = new Vector2(
                Input.GetAxis("Mouse X"),
                Input.GetAxis("Mouse Y")
            ),
            Jump = Input.GetButton("Jump"),
            Slide = Input.GetButton("Slide"),
            Grapple = Input.GetButton("Grapple"),
            SpinRecord = Input.GetButton("SpinRecord"),
            SpinDischarge = Input.GetButton("SpinDischarge"),
            Fire = Input.GetButton("Fire"),
            // Add other inputs
        };
    }
}

/// <summary>
/// Pure C# input struct (no Unity types).
/// </summary>
public readonly struct PlayerInputs
{
    public readonly Vector2 Movement;
    public readonly Vector2 Aim;
    public readonly bool Jump;
    public readonly bool Slide;
    public readonly bool Grapple;
    public readonly bool SpinRecord;
    public readonly bool SpinDischarge;
    public readonly bool Fire;
    // Add other inputs
}
```

### 2.3 Create Perfect Information Visuals

**File: `src/Presentation/Scripts/Rendering/PerfectInfoRenderer.cs`**

```csharp
using UnityEngine;
using Simulation.Core;

namespace Presentation.Rendering;

/// <summary>
/// Renders perfect-information overlays.
/// READS from simulation state, WRITES to Unity visual effects.
/// </summary>
public sealed class PerfectInfoRenderer : MonoBehaviour
{
    [SerializeField] private SimulationBridge _simulationBridge;
    [SerializeField] private GameObject _positionLightPrefab;
    [SerializeField] private GameObject _lookConePrefab;
    [SerializeField] private GameObject _spinMeterPrefab;

    private GameObject[] _positionLights;
    private LookCone[] _lookCones;
    private SpinMeter[] _spinMeters;

    private void Update()
    {
        var state = _simulationBridge.GetState();

        // Update position lights (visible through geometry)
        for (int i = 0; i < state.Movers.Length; i++)
        {
            _positionLights[i].transform.position = ToVector3(state.Movers[i].Position);
        }

        // Update look cones
        for (int i = 0; i < state.Movers.Length; i++)
        {
            _lookCones[i].UpdateFromState(state.Movers[i]);
        }

        // Update spin meters
        for (int i = 0; i < state.Movers.Length; i++)
        {
            _spinMeters[i].UpdateFromState(state.Movers[i].Spin);
        }
    }

    private Vector3 ToVector3(FPVector3 fpVec)
    {
        return new Vector3(
            (float)fpVec.X,
            (float)fpVec.Y,
            (float)fpVec.Z
        );
    }
}
```

---

## Phase 3: Clean Up Existing Unity Code (Week 2)

### 3.1 Remove Game Logic from Unity Components

Audit existing Unity components and remove any game logic:

**Files to review and clean:**
- `Assets/_Project/Core/` - Remove sim logic, keep only mode state
- `Assets/_Project/UI/` - Ensure UI only reads state, never writes
- `Assets/QuantumUser/Simulation/` - Move systems to Simulation/, delete originals
- `Assets/QuantumUser/View/` - Keep only rendering logic

**Example cleanup:**

**Before:**
```csharp
// Unity component with mixed concerns
public class PlayerController : MonoBehaviour
{
    private void Update()
    {
        // BAD: Game logic in Unity component
        var inputs = GetInput();
        velocity += inputs * acceleration;
        transform.position += velocity * Time.deltaTime;

        // BAD: Direct visual update
        UpdateVisuals();
    }
}
```

**After:**
```csharp
// Unity component is purely visual
public class PlayerRenderer : MonoBehaviour
{
    private SimulationBridge _simBridge;

    private void Update()
    {
        // GOOD: Read state from simulation, update visuals only
        var state = _simBridge.GetState();
        transform.position = state.Position;
    }
}

// Game logic is in pure C# simulation
public class MovementSystem
{
    public void ApplyMovement(Mover mover, PlayerInputs inputs, FP deltaTime)
    {
        // Pure simulation logic, no Unity dependencies
        mover.Velocity += inputs.Movement * mover.Acceleration * deltaTime;
    }
}
```

### 3.2 Delete or Deactivate Determinism Tests in Unity

The Unity-based determinism harness is no longer needed:

```bash
# Move to archive (don't delete yet, for reference)
mkdir -p archive/old-unity-tests
mv Assets/QuantumUser/View/RopeDeterminismHarness.cs archive/old-unity-tests/
```

### 3.3 Update Unity Project References

Ensure the Unity project can reference the pure simulation code:

**Option 1: Compile Simulation/ as a DLL and reference in Unity**
```bash
cd src/Simulation
dotnet build -c Release
# Copy Simulation.dll to Unity project's Plugins/ or Assets/
```

**Option 2: Use Unity's Package Manager to reference the Simulation project**

Create `Packages/manifest.json` entry:
```json
{
  "dependencies": {
    "com.movement.simulation": "file:../../src/Simulation"
  }
}
```

---

## Phase 4: Validation and Testing (Week 2-3)

### 4.1 Run Pure Simulation Tests

```bash
cd src/Simulation/Tests
dotnet test --verbosity normal

# Expected: All tests pass in < 5 seconds
# Verify determinism tests still pass (10k runs, rollback, etc.)
```

### 4.2 Verify Unity Integration

1. **Open Unity project**
2. **Create a test scene** with `SimulationBridge` and minimal renderers
3. **Enter Play mode** and verify:
   - Simulation ticks at 128 Hz
   - Visuals update correctly
   - Input is captured and forwarded
   - Perfect information overlays render

### 4.3 Compare With Original

Run side-by-side comparison:

1. **Original Unity project** - Play a match
2. **Refactored project** - Play the same match
3. **Verify identical behavior** (inputs should produce same outputs)

### 4.4 Performance Profiling

Profile both versions:

**Pure simulation:**
```bash
cd src/Simulation/Tests
dotnet test --filter "FullSimulation_Deterministic" --logger "console;verbosity=detailed"
# Measure per-tick time
```

**Unity integration:**
- Use Unity Profiler to verify frame budget
- Confirm simulation cost is unchanged from FIRST_TEST results

### 4.5 Determinism Validation

Run expanded determinism tests:

**File: `src/Simulation/Tests/Determinism/FullGameDeterminism.cs`**

```csharp
using Xunit;
using Simulation.Core;

namespace Simulation.Tests.Determinism;

/// <summary>
/// Full-game determinism test - covers all systems interacting.
/// </summary>
public class FullGameDeterminism
{
    [Fact]
    public void FullMatch_Deterministic()
    {
        // Simulate a full match (multiple rounds, geometry edits, etc.)
        // Capture final state hash
        // Run 100 times, verify all identical
    }

    [Fact]
    public void FullMatch_WithRollback_Deterministic()
    {
        // Simulate match with forced rollbacks
        // Verify re-sim produces identical state
    }
}
```

---

## Phase 5: Documentation and Handoff (Week 3)

### 5.1 Update Architecture Documentation

Update `docs/ARCHITECTURE.md` with new structure:

```markdown
## Project status

- **Engine: Unity 6000.3.17f1 + Photon Quantum 3.0.11. VALIDATED by the First Test.**
- **Architecture: Code-first. Simulation/ contains pure C# simulation, Presentation/ contains Unity renderer.**
- **Testing: Comprehensive headless tests in Simulation/Tests/, run in milliseconds.**
- **Determinism: All simulation code is pure C#, no Unity dependencies, validated at 128 Hz.**
```

### 5.2 Create AI Agent Guidelines

Create `docs/AI_GUIDELINES.md`:

```markdown
# AI Agent Guidelines

## What to Edit

**DO EDIT:**
- `src/Simulation/` - All simulation logic, systems, networking
- `src/Simulation/Tests/` - Test coverage, new tests
- `src/Presentation/Scripts/Rendering/` - Visual effects, shaders, particles
- `src/Presentation/Scripts/UI/` - HUD, menus (read-only sim state)

**DO NOT EDIT:**
- `src/Presentation/Scripts/SimBridge/` - The boundary layer (unless architecture changes)
- Unity scene files (`.unity`) - These are binary and fragile
- Unity prefabs - Edit the component scripts, not the prefab files
- Any file that writes TO simulation state from Unity (violates the pattern)

## Code Patterns

**Simulation Code:**
- Pure C#, no `UnityEngine` or `UnityEditor` references
- Use Quantum types (`FP`, `FPVector3`, `Frame`) for determinism
- All math is fixed-point, no floating-point in simulation
- Systems read/write Frame data, never external state

**Presentation Code:**
- Reads from `SimulationState` structs (pure C#)
- Writes to Unity transforms, materials, particles
- Never modifies simulation state
- Input capture sends `PlayerInputs` structs to sim

**Testing:**
- All sim tests run headless (`dotnet test`)
- Test invariants, edge cases, and determinism
- Use `Assert.Equal` for bit-identical comparisons
- No Unity Play Mode tests for simulation logic

## Common Patterns

**Adding a new simulation system:**
1. Create `src/Simulation/Systems/NewSystem.cs`
2. Implement pure C# logic with Quantum types
3. Add unit tests in `src/Simulation/Tests/Systems/NewSystemTests.cs`
4. Register system in `Simulation.cs`
5. Add renderer in `src/Presentation/Scripts/Rendering/` (if visual)

**Fixing a bug:**
1. Write a failing test in `src/Simulation/Tests/`
2. Fix the bug in `src/Simulation/`
3. Verify test passes
4. Check for visual impact in Unity (if needed)

**Adding a new visual effect:**
1. Add state to `SimulationState` struct (if sim needs to track it)
2. Update renderer in `src/Presentation/Scripts/Rendering/`
3. Test in Unity Play mode
```

### 5.3 Update CLAUDE.md

Update `docs/CLAUDE.md` with new workflow:

```markdown
## Working conventions

- **Simulation/ is the source of truth.** All game logic lives here.
- **Presentation/ is read-only.** Unity renders what Simulation/ computes.
- **Test first.** Write tests in Simulation/Tests/ before touching Simulation/.
- **Run tests frequently.** `cd src/Simulation/Tests && dotnet test` takes < 5 seconds.
- **Never mix concerns.** Simulation code never references Unity; Presentation code never writes to sim.
```

---

## Validation Checklist

Before considering the refactoring complete:

- [ ] All existing determinism tests pass in `src/Simulation/Tests/`
- [ ] Tests run in < 5 seconds (vs minutes in Unity Play Mode)
- [ ] Unity project builds and runs without errors
- [ ] Simulation ticks at 128 Hz (verified in Profiler)
- [ ] Visuals update correctly from simulation state
- [ ] Input is captured and forwarded to simulation
- [ ] Perfect information overlays render correctly
- [ ] No Unity dependencies in `src/Simulation/`
- [ ] No game logic in Unity components (only rendering)
- [ ] Documentation updated (ARCHITECTURE.md, AI_GUIDELINES.md, CLAUDE.md)
- [ ] Side-by-side comparison with original shows identical behavior
- [ ] Performance profile shows simulation cost unchanged from FIRST_TEST

---

## Rollback Plan

If the refactoring introduces issues:

1. **Revert to Git commit** before refactoring started
2. **Archive the refactored code** to a branch (`refactor/code-first-architecture`)
3. **Diagnose the issue** using the pure simulation tests (fast feedback)
4. **Fix the issue** in the refactored branch
5. **Retry the migration** once fixed

The key advantage of this approach: **pure simulation tests give fast feedback without Unity overhead**, making debugging much easier.

---

## Estimated Timeline

| Phase | Duration | Key Deliverables |
|-------|----------|------------------|
| Phase 1: Pure Simulation Layer | 5 days | `src/Simulation/` project, all tests passing |
| Phase 2: Unity Presentation Bridge | 3-4 days | `SimBridge`, `InputCapture`, renderers working |
| Phase 3: Clean Up Unity Code | 2-3 days | Unity project stripped to renderer only |
| Phase 4: Validation & Testing | 3-4 days | All tests passing, side-by-side comparison |
| Phase 5: Documentation | 2 days | Updated docs, AI guidelines |
| **Total** | **15-18 days (3 weeks)** | Code-first architecture validated |

---

## Success Criteria

The refactoring is successful when:

1. **Tests run in seconds, not minutes** - `dotnet test` completes in < 5 seconds
2. **No Unity dependencies in Simulation/** - Pure C# only
3. **AI can work with Simulation/ independently** - No need to open Unity
4. **Determinism preserved** - All existing tests still pass
5. **Performance unchanged** - Simulation cost matches FIRST_TEST results
6. **Documentation clear** - Any AI agent can understand the architecture

---

## Next Steps After Refactoring

Once this refactoring is complete:

1. **AI agents can work on simulation logic independently** - No Unity required
2. **Test-driven development becomes practical** - Fast feedback loop
3. **Continuous integration is trivial** - `dotnet test` in CI pipeline
4. **Collaboration improves** - Simulation logic is just C# code, reviewable like any other code
5. **Future flexibility** - Can swap renderers (three.js, custom engine) without touching simulation

---

## Questions for the AI Agent Executing This

Before starting, the AI agent should verify:

1. **Does the existing Unity project build successfully?** (Start from known-good state)
2. **Are the existing determinism tests passing?** (FIRST_TEST validation)
3. **What is the current directory structure?** (Verify paths match this guide)
4. **Are there any custom Unity packages or dependencies?** (May affect extraction)
5. **What is the Git status?** (Ensure clean working state before major refactoring)

---

**End of Refactoring Guide**