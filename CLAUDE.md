# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 6 (`6000.4.0f1`) Pong game built on an event-driven ScriptableObject architecture. Based on Unity's Game Systems Cookbook (PaddleGameSO). Training lab for AI-assisted game development.

## Key Commands

- **Play the game:** Open `Assets/Scenes/Bootloader_Scene.unity` in Unity, press Play
- **Controls:** Player 1: W/S, Player 2: Up/Down arrows (2P mode), Restart: R
- **Single-player mode:** AI controls Player 2 by default. Disable via `AIPaddleSettings` asset (`Enabled = false`)
- **MCP connection:** Window > MCP for Unity (requires `pip install mcpforunityserver`). Uses `UnityMCP` server (not `mcpforunity`)
- **Tests:** Unity Test Framework (`com.unity.test-framework`), 36 EditMode tests in `Assets/Tests/Editor/`

## Architecture

### Event-Driven Pub/Sub System

All communication between systems uses **ScriptableObject event channels** — no direct component references. Event channels live in `Assets/Core/EventChannels/` with concrete types (Void, Bool, Float, Int, Vector2, Vector3, String, GameObject, PlayerID, PlayerScore, ScoreList).

Pattern: a MonoBehaviour subscribes to an event channel SO in `OnEnable`, unsubscribes in `OnDisable`, and raises events via `channel.RaiseEvent(payload)`.

### Game Flow

```
InputReaderSO (InputSystem adapter)
  → Paddle (AddForce via Rigidbody2D)
  → Ball (physics) → Bouncer (collision) → ScoreGoal (GoalHit event)
  → GameManager (PointScored) → ScoreManager → ScoreObjectiveSO
  → ObjectiveManager (AllObjectivesCompleted) → GameManager (GameEnded)
```

GameManager orchestrates state transitions by listening to and broadcasting events. It does not reference game objects directly.

### AI Paddle System

Single-player mode adds an AI opponent for Player 2. The AI is designed to be beatable — it reacts with delay and imperfect accuracy.

```
AIPaddleSettingsSO (config: reaction time, speed, accuracy, prediction)
  → GameSetup (if AI enabled, adds AIPaddle component to P2, disables human input)
    → AIPaddle (Update: tracks ball.y with delay/offset → Paddle.SetAIInput)
      → Paddle (FixedUpdate: applies force via Rigidbody2D with boundary clamping)
```

**Key files:**
- `Assets/PaddleBall/Scripts/AIPaddle.cs` — AI controller MonoBehaviour
- `Assets/PaddleBall/Scripts/ScriptableObjects/AIPaddleSettingsSO.cs` — Difficulty config SO
- `Assets/PaddleBall/Data/AIPaddleSettings.asset` — Default settings (assigned in all game scenes)

**AI behavior:** Reaction delay timer updates `m_TargetY` periodically (not every frame). Random accuracy offset is applied each update cycle. Optional ball velocity prediction. Dead zone prevents jitter. Paddle.SetAIInput feeds into existing CalculateMovement/AddForce pipeline.

**Toggling:** Set `Enabled = false` on the AIPaddleSettings asset, or clear the `AI Settings` field on GameSetup to revert to 2-player mode.

### Module Layout

- **`Assets/PaddleBall/`** — Game-specific code (Ball, Paddle, Bouncer, ScoreGoal, managers, UI, SO data assets)
- **`Assets/Core/`** — Reusable framework (EventChannels, UI system, Objectives, Audio, Utilities, SaveLoad, SceneManagement)

### Key ScriptableObjects

- **GameDataSO** — All gameplay parameters (paddle speed/drag/mass, ball speed/max/bounce, delay between points, player references, level layout)
- **LevelLayoutSO** — Positions/scales for ball, paddles, goals, walls. Supports JSON export/import
- **InputReaderSO** — Wraps Unity InputSystem, exposes `P1Moved`, `P2Moved`, `GameRestarted` as UnityActions
- **PlayerIDSO** — Marker SO used as player identity (no strings/ints)
- **ScoreObjectiveSO** — Win condition: checks if any player reaches target score
- **AIPaddleSettingsSO** — AI difficulty config (reaction time, tracking speed, accuracy offset, dead zone, prediction)

### UI System

Stack-based navigation in `Assets/Core/UI/`. `UIManager` manages `View` subclasses (GameScreen, ModalScreen, SplashScreen) via show/hide with breadcrumb history.

### Utilities

- **NullRefChecker** — Reflection-based validation of all `[SerializeField]` fields at runtime; skip with `[Optional]`
- **SaveManager** — Static JSON/XML file I/O to `Application.persistentDataPath`

## Code Style

- **Namespaces:** `GameSystemsCookbook` (core), `GameSystemsCookbook.Demos.PaddleBall` (game-specific)
- **Naming:** Pascal case for public, `m_` prefix for private fields, `s_` for static, `k_` for constants, camelCase for locals/params
- **ScriptableObjects:** class name ends with `SO` suffix
- **Formatting:** Allman braces, 80-120 char lines, no regions
- **Comments:** Use `[Tooltip]` on serialized fields instead of comments; `<summary>` XML tags on classes
- **Null checks:** Use `?.` for System.Object, explicit `!= null` for UnityEngine.Object

Full style reference: `Assets/Core/_StyleGuide/StyleExample.cs`

## Unity API Notes (Unity 6)

- **Rigidbody2D:** Use `linearVelocity` (not `velocity`) and `linearDamping` (not `drag`) — Unity 6 renamed these properties. Apply forces in `FixedUpdate` via `AddForce(vector, ForceMode2D.Impulse)`. Constant force produces acceleration, not constant speed — use max velocity checks to cap speed.
- **ScriptableObject pattern:** `[CreateAssetMenu]` attribute for editor creation. Use `[SerializeField] private` fields with public read-only properties. Subscribe in `OnEnable`, unsubscribe in `OnDisable`.
- **Input System:** `InputAction.performed += callback` to subscribe; `-=` to unsubscribe. Always enable/disable in `OnEnable`/`OnDisable`. `CallbackContext.ReadValue<T>()` extracts typed input values. `WasPerformedThisFrame()` checks action state.
- **Physics timing:** `Update` runs before `FixedUpdate` each frame. Set input in `Update`, apply forces in `FixedUpdate` for correct physics integration.

## Dependencies

Key packages: `com.unity.inputsystem`, `com.unity.render-pipelines.universal` (URP), `com.unity.ui` (UI Toolkit), `com.coplaydev.unity-mcp` (MCP integration), `com.unity.timeline`
