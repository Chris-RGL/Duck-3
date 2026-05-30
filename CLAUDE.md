# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Duck-3 is a Unity 6 (6000.3.8f1) 3D action game using the Universal Render Pipeline (URP). The project is in early development — `Assets/Scripts/` is the place to add all game code.

## Unity Development

There is no CLI build command to run from this directory. All building, testing, and play-mode execution happens inside the Unity Editor. To open the project, launch Unity Hub and open this folder.

- **Active scene for physics/mechanics work**: `Assets/Scenes/Pendulum.unity` (current branch `Pendelum` targets this)
- **Default scene**: `Assets/Scenes/SampleScene.unity`

## Project Conventions

- **Game scripts** → `Assets/Scripts/`
- **ML-Agents trainer YAML configs** → `config/`
- Do not modify tutorial utilities in `Assets/TutorialInfo/Scripts/`
- Use URP-compatible shaders and `UniversalRenderPipeline` APIs — not the legacy `Standard` shader

## ML-Agents Training

Trainer configs live in `config/`. To start a training run:

```
mlagents-learn config/trainer_config.yaml --run-id=<run_name>
```

Then press **Play** in the Unity Editor. To resume a stopped run add `--resume`.

The active config is `config/trainer_config.yaml` targeting behavior name **`Pendulum`** (must match the *Behavior Name* field in `BehaviorParameters` on the Cart). Key settings: PPO, `normalize: true`, `max_steps: 2000000`, 32 parallel agents, `DecisionPeriod: 5`. Set **Max Step** in `BehaviorParameters` to `5000` to cap episode length.

The pendulum model has been successfully trained to balance. The current architecture does **adaptive gain scheduling** — the NN outputs different Kp/Ki/Kd values each step based on the current state (angle, velocity, etc.), rather than settling on a single fixed set. This is expected and intentional.

**Adaptive vs fixed gains trade-off:**
- **Adaptive (current)**: more capable when disturbances are present (random pushes, variable mass). The NN reactively adjusts gains based on state.
- **Fixed**: sufficient for a stable plant with no disturbances. Simpler and fully interpretable.

To extract fixed gains from the trained model: run it while balanced, log the Kp/Ki/Kd outputs over ~1000 steps, and average them. Those values can be hardcoded into `PendulumController` and the ML agent removed entirely.

To make the agent find fixed gains instead of doing gain scheduling: restructure `OnActionReceived` to apply gains only once per episode (in `OnEpisodeBegin`) rather than every decision step.

## Architecture

### Input System
All player inputs are defined in `Assets/InputSystem_Actions.inputactions` using Unity's New Input System. The configured **Player** action map includes:

| Action | Binding |
|--------|---------|
| Move | WASD / arrows / gamepad left stick |
| Look | Mouse delta / gamepad right stick |
| Attack | Left mouse / gamepad west |
| Jump | Space / gamepad south |
| Sprint | Left shift / gamepad left stick press |
| Crouch | C / gamepad east |
| Interact | E (hold) / gamepad north |
| Previous/Next | 1/2 keys (item/weapon cycling) |

Control schemes: Keyboard&Mouse, Gamepad, Touch, Joystick, XR.

### Rendering
Two URP renderer assets live in `Assets/Settings/`:
- `PC_Renderer` — high quality, used for desktop builds
- `Mobile_Renderer` — optimized for mobile

Avoid modifying the URP asset configs directly; use Volume Profiles for per-scene post-processing.

### Key Packages
- **ML-Agents 4.0.3** — AI/learning-driven behavior; trainer configs go in `config/`
- **Input System 1.18.0** — use `PlayerInput` component or `InputAction` callbacks, not legacy `Input.*`
- **Timeline 1.8.10** — for scripted sequences/cutscenes
- **AI Navigation (NavMesh)** — built-in, available for NPC pathfinding
- **Visual Scripting 1.9.9** — available but prefer C# scripts in `Assets/Scripts/`

### Pendulum System (`Assets/Scenes/Pendulum.unity`)

#### Required scene hierarchy
```
Pendulum          ← empty root (no Rigidbody)
├── Cart          ← kinematic Rigidbody sphere
└── Rod           ← dynamic Rigidbody; sibling of Cart, NOT a child of it
```
Rod must be a **sibling** of Cart (both children of the Pendulum root). Making Rod a child of Cart causes `Physics.SyncTransforms()` to override the rod's physics position during episode resets, breaking respawn. Rod and Cart are linked by a **HingeJoint** on the Rod with `connectedBody` set to the Cart's Rigidbody.

#### Script placement — all on Cart
| Component | Notes |
|-----------|-------|
| `PendulumController` | Runs the PID loop; requires a Rigidbody |
| `PendulumAgent` | ML-Agents agent; requires PendulumController + DecisionRequester |
| `BehaviorParameters` | Behavior Name = `Pendulum`, Vector Obs = 4, Continuous Actions = 3, Max Step = 5000 |
| `DecisionRequester` | Decision Period = 5 |

Both `PendulumController` and `PendulumAgent` expose a **Rod Rigidbody** slot in the Inspector — drag the Rod's Rigidbody into both.

#### PendulumController (`Assets/Scripts/PendulumController.cs`)
Runs a PID loop on rod angle (`GetRodAngleDegrees` normalises `eulerAngles.z` to `(-180, 180]`, 0 = upright) and integrates the clamped output into an internal `_cartVelocity`, then moves the cart via `MovePosition`. Key design points:

- **Derivative on measurement** — Kd is applied to `−rodAngularVel` directly, not the error difference, to avoid derivative kick.
- **Anti-windup** — integral is frozen when `|rodAngle| > integralFreezeThreshold` and hard-clamped to `±integralClamp`.
- **Acceleration cap** — `maxAcceleration` prevents uncapped step accumulation that causes spazzing.
- **Wall velocity zeroing** — at `±positionLimit` the internal velocity is zeroed in the wall direction (not just position-clamped).
- **Position correction** — `KpPosition`/`KdPosition` provide a soft restoring force back to origin so the cart doesn't drift.

#### PendulumAgent (`Assets/Scripts/PendulumAgent.cs`)
ML-Agents agent that tunes the PID gains at runtime. The agent's **3 continuous actions** remap tanh output `[-1, 1]` to `[0, kpMax/kiMax/kdMax]` each step. **4 observations**: normalised cart X position, cart velocity, rod angle, rod angular velocity. Episode terminates when `|rodAngle| >= 90°`. On episode begin both Cart and Rod are reset to world origin — Cart via `transform.position = Vector3.zero`, Rod via `rodRigidbody.position = _rodOffsetFromCart` (offset captured once in `Initialize()` from the scene's starting positions). `Heuristic()` maps all actions to 0 (mid-range gains) for play-mode testing without a trained model.

#### PushPendulum (`Assets/Scripts/PushPendulum.cs`)
Attach to the Rod. Fires a one-shot impulse along +X in `Start()` to kick the pendulum off-balance — useful for testing recovery. Controlled by the `pushStrength` field.
