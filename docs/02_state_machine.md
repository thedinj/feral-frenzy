# Feral Frenzy — Architecture Document 2: State Machine & Transitions
**Version:** 1.0  
**Status:** Authoritative  
**Depends on:** FERAL_FRENZY_BIBLE.md, 01_schema.md  
**Referenced by:** 03_claude_md.md, GameStateManager.cs, RunSpine.cs, all mode controllers

---

## Purpose

This document defines every game state, every legal transition between states, what data travels with each transition, and the responsibilities of the `GameStateManager` autoload. It is the skeleton of the entire game. Any system that changes game flow — the run spine, cinematics, genre levels, revive window, the editor — is described here first and implemented second.

---

## The Core Principle

State management in Feral Frenzy follows one rule: **exactly one state is active at a time, and every transition is explicit.**

There is no implicit state. There is no "we're kind of in gameplay but also in a cutscene." The `GameStateManager` is the single source of truth. If you need to know what the game is doing, you ask the manager. If you need to change what the game is doing, you go through the manager.

The run spine is not a separate system that competes with the state manager. It is a payload — a sequence of state transitions pre-computed from the generator output — that the manager executes one step at a time.

---

## States — Class-Based, Not an Enum

**There is no `GameState` enum.** Each state is its own sealed class that extends `GameStateNode`. `GameStateManager.Current` returns a `GameStateNode`. Check state with `is` patterns — never with equality comparisons against an enum value.

```csharp
// src/godot/autoloads/GameStateManager.cs — state base + transition pattern
public abstract class GameStateNode { }

// Implemented states (each in its own file under src/godot/autoloads/):
public sealed class TitleState        : GameStateNode { }
public sealed class LoadoutSelectState: GameStateNode { }
public sealed class SegmentState      : GameStateNode { }
public sealed class SegmentRestartState : GameStateNode, IAutoTransition { ... }
public sealed class BossFightState    : GameStateNode { }
public sealed class VillainExitState  : GameStateNode { }
public sealed class RunSummaryState   : GameStateNode { }
// Future: BossIntroState, CinematicState, GradiusLevelState, BrawlerLevelState, etc.

// Checking current state — always use 'is' patterns:
if (_gameState.Current is BossFightState) { ... }
if (_gameState.Current is SegmentState or BossFightState) { ... }
```

**States with timed auto-transitions implement `IAutoTransition`:**
```csharp
public interface IAutoTransition
{
    float GetDelay(StatePayload? payload);
    (Type Next, StatePayload? Payload)? SelectNext(GameStateContext ctx);
}

// Example — SegmentRestartState auto-transitions after 1.5s:
public sealed class SegmentRestartState : GameStateNode, IAutoTransition
{
    private const float RestartDelay = 1.5f;
    public float GetDelay(StatePayload? payload) => RestartDelay;
    public (Type Next, StatePayload? Payload)? SelectNext(GameStateContext ctx)
    {
        if (ctx.WipedFromBossFight) { ctx.WipedFromBossFight = false; return (typeof(BossFightState), null); }
        return (typeof(SegmentState), null);
    }
}
```

**Canonical state list** (design intent, some future states not yet implemented):

| State class | Purpose |
|---|---|
| `TitleState` | Main menu |
| `LoadoutSelectState` | Character + weapon selection, 1–4 players |
| `SegmentState` | Active gameplay — enemies, hazards, movement |
| `SegmentRestartState` | All players down — brief pause before retry |
| `BossFightState` | Chapter boss encounter |
| `VillainExitState` | Defeat cinematic + The Laugh beat |
| `RunSummaryState` | Stats, unlock reveals |
| `BossIntroState` *(future)* | Villain reveal moment before the fight |
| `CinematicState` *(future)* | Chapter intro, villain exit cinematic |
| `GradiusLevelState` *(future)* | Full mode switch — horizontal shooter |
| `BrawlerLevelState` *(future)* | Full mode switch — top-down beat em up |
| `AttractState` *(future)* | Auto-demo if title sits idle |

**Note on ReviveWindow:** Revive is handled by `ReviveSystem` (a child Node of `LevelController`) — it is not a separate `GameStateNode`. `LevelController` owns the revive timer, proximity check, and the decision to call `TransitionTo<SegmentRestartState>()` when all players are eliminated. This avoids putting game-world knowledge (player positions, roster) into the state machine context.

---

## The GameStateManager Autoload

```csharp
// src/godot/autoloads/GameStateManager.cs
// Registered as autoload "GameStateManager" in Godot project settings.
public partial class GameStateManager : Node
{
    // Current state. Read-only externally. Check with 'is' patterns, never enum equality.
    public GameStateNode Current { get; private set; }

    // Mutable context bag — routing flags and run-meta stats only. No game-world state here.
    private readonly GameStateContext _ctx = new GameStateContext();

    // Single auto-timer for all IAutoTransition states — no per-state Timer fields.
    private Timer _autoTimer = null!;

    // Plain C# event — NOT a Godot [Signal]. Passes GameStateNode instances directly.
    // Subscribers check 'to is BossFightState' etc. — no casting, no magic numbers.
    public event Action<GameStateNode, GameStateNode>? StateChanged;

    // The only way to change state.
    public void TransitionTo<T>(StatePayload? payload = null) where T : GameStateNode, new() { ... }

    // Dynamic overload for runtime type dispatch (RunSpine, etc.).
    public void TransitionTo(Type nextType, StatePayload? payload = null) { ... }
}
```

**Subscribing to StateChanged:**
```csharp
_gameState.StateChanged += OnStateChanged;
private void OnStateChanged(GameStateNode from, GameStateNode to)
{
    if (to is BossFightState) { ConnectBossHpBar(); }
    if (to is SegmentState or BossFightState) { Visible = true; }
}
```

### Transition Rules

`TransitionTo` validates every transition against the legal transition table before executing it. An illegal transition throws in debug builds and logs a warning in release builds.

**Implemented legal transitions** (current state of the game — expand as new states are added):

| From | To |
|---|---|
| `TitleState` | `LoadoutSelectState` |
| `LoadoutSelectState` | `TitleState`, `SegmentState` |
| `SegmentState` | `SegmentRestartState`, `BossFightState`, `RunSummaryState`, `SegmentState` |
| `SegmentRestartState` | `SegmentState`, `BossFightState` |
| `BossFightState` | `SegmentRestartState`, `VillainExitState` |
| `VillainExitState` | `RunSummaryState` |
| `RunSummaryState` | `TitleState`, `LoadoutSelectState` |

**Future transitions** (design intent, states not yet implemented):

| From | To |
|---|---|
| `LoadoutSelectState` | `CinematicState` (chapter 1 intro) |
| `SegmentState` | `BossIntroState`, `GradiusLevelState`, `BrawlerLevelState`, `CinematicState` |
| `BossFightState` | `ReviveWindow`* (handled by `ReviveSystem`/`LevelController`, not a state) |
| `VillainExitState` | `CinematicState`, `GradiusLevelState`, `BrawlerLevelState` |
| `RunSummaryState` | `CreditsState` |

---

## State Payloads

Every transition can carry a typed payload. The receiving state reads the payload to know what to do. Payloads are optional — `null` is valid for transitions that carry no data.

```csharp
// src/core/data/engine/StatePayload.cs
// Base class. All specific payloads inherit from this.
public abstract record StatePayload;

// Cinematic state needs to know which cinematic to play and where to return.
public record CinematicPayload(
    string CinematicKey,        // AssetRegistry key → cinematic scene/data
    GameState ReturnState,      // where to go when cinematic completes
    StatePayload ReturnPayload  // payload to carry into the return state
) : StatePayload;

// Segment state needs the segment data to instantiate.
public record SegmentPayload(
    SegmentData Segment,
    bool IsRestart              // true if arriving from SegmentRestart
) : StatePayload;

// BossFight state needs to know which boss.
public record BossFightPayload(
    string VillainKey,          // content key → FFVillainDefinition (deferred)
    string ChapterKey
) : StatePayload;

// ReviveWindow needs to know which player is down and the countdown.
public record ReviveWindowPayload(
    int DownPlayerIndex,
    float ReviveWindowSeconds   // configurable per chapter
) : StatePayload;

// RunSummary needs the completed run data for stats display.
public record RunSummaryPayload(
    RunData CompletedRun,
    bool RunCompleted,          // false if player quit mid-run
    List<string> UnlocksEarned  // content keys of cosmetics unlocked this run
) : StatePayload;

// LevelEditor carries optional pre-loaded segment data (for generator debugging).
public record LevelEditorPayload(
    SegmentData PreloadedSegment = null  // null = open blank editor
) : StatePayload;
```

---

## The Run Spine

The run spine is not a state — it is a sequencer that drives state transitions during a run. It lives as a child of `GameStateManager` and is created when a run begins, destroyed when it ends.

```csharp
// src/core/data/engine/SpineStep.cs
// A pre-computed instruction in the run sequence.
public abstract record SpineStep;

public record PlaySegmentStep(SegmentData Segment) : SpineStep;
public record PlayBossStep(string VillainKey, string ChapterKey) : SpineStep;
public record PlayGenreLevelStep(GameState GenreLevel) : SpineStep;  // Gradius or Brawler
public record PlayCinematicStep(string CinematicKey) : SpineStep;
public record EndRunStep : SpineStep;
```

```csharp
// src/godot/autoloads/RunSpine.cs
public partial class RunSpine : Node
{
    private Queue<SpineStep> _steps;
    private RunData _runData;

    // Called by GameStateManager when LoadoutSelect transitions to the run.
    public void Initialize(RunData runData)
    {
        _runData = runData;
        _steps = BuildStepQueue(runData);
    }

    // Called by GameStateManager after each state completes.
    // Returns the next transition to execute.
    public (GameState nextState, StatePayload payload) Advance()
    {
        if (_steps.Count == 0)
            return (GameState.RunSummary, BuildSummaryPayload());

        var step = _steps.Dequeue();
        return step switch
        {
            PlaySegmentStep s   => (GameState.Segment,     new SegmentPayload(s.Segment, false)),
            PlayBossStep b      => (GameState.BossIntro,   new BossFightPayload(b.VillainKey, b.ChapterKey)),
            PlayGenreLevelStep g => (g.GenreLevel,         null),
            PlayCinematicStep c => (GameState.Cinematic,   BuildCinematicPayload(c)),
            EndRunStep          => (GameState.RunSummary,  BuildSummaryPayload()),
            _ => throw new InvalidOperationException($"Unknown spine step: {step}")
        };
    }

    private Queue<SpineStep> BuildStepQueue(RunData runData)
    {
        // Translates the flat segment list from RunData into an ordered
        // sequence of SpineSteps, inserting cinematics, boss intros,
        // genre levels, and villain exits at the correct positions.
        // The run structure is fixed per the bible:
        //   Ch1 segments → Ch1 boss → Gradius → Ch2 segments → Ch2 boss → Brawler → Ch3 segments → Final boss
        ...
    }
}
```

### The Fixed Run Structure

The spine always produces steps in this order. This is not configurable — it is the bible's fixed macro structure.

```
Chapter 1 intro cinematic
  ├── Opening segment
  ├── Combat segments (2–4, generated)
  ├── Setpiece segment
  ├── Boss intro cinematic
  ├── Boss fight (Baroness Cretacia)
  └── Villain exit + The Laugh cinematic
Gradius level (The Commute)
Chapter 2 intro cinematic
  ├── Opening segment
  ├── Combat segments (2–4, generated) [guaranteed destructible level here]
  ├── Setpiece segment
  ├── Boss intro cinematic
  ├── Boss fight (Professor Static)
  └── Villain exit + The Laugh cinematic
Brawler level (The Layover)
Chapter 3 intro cinematic
  ├── Opening segment
  ├── Combat segments (2–4, generated)
  ├── Setpiece segment
  ├── Boss intro cinematic
  ├── Boss fight (Lord Inferno)
  └── Villain exit + The Laugh cinematic
Final boss [deferred]
Run summary
```

---

## State Responsibilities

Each state owns exactly what it needs to own — no more.

### Title
- Display main menu UI
- Start attract timer (fire `TransitionTo(Attract)` after `AttractIdleSeconds` with no input)
- Route to LoadoutSelect, LevelEditor, Credits, WorkshopBrowser on player action
- Reset any stale run state on entry

### Attract
- Play recorded or scripted gameplay demonstration
- Return to Title on any input
- Does not instantiate a real run — uses pre-recorded spine data

### LoadoutSelect
- Detect connected controllers (1–4 players)
- Character selection per player
- Weapon selection per player (Tier 1 always available, Tier 2 visible, Tier 3 if unlocked)
- On confirm: generate RunData from seed, initialize RunSpine, transition to Cinematic (chapter 1 intro)

### Cinematic
- Reentrant — can be called from multiple states
- Reads CinematicPayload to know which cinematic to play
- On complete: transitions to `ReturnState` with `ReturnPayload`
- On skip (any player input after skip window): same as complete
- Does not modify run state

### Segment
- Reads SegmentPayload — instantiates the segment via the importer
- Runs gameplay loop
- Monitors player death state:
  - One player down → TransitionTo(ReviveWindow)
  - All players down → TransitionTo(SegmentRestart)
- On segment complete: calls RunSpine.Advance() for next step
- On IsRestart: skip entry fanfare, drop players in immediately

### ReviveWindow
- Countdown timer (configurable per chapter, typically 8–12 seconds)
- Any living player near downed player revives them → TransitionTo(Segment)
- Timer expires → TransitionTo(SegmentRestart)
- Does not pause enemy AI or hazards — the chaos continues

### SegmentRestart
- Brief freeze frame + brief animation
- Returns to same segment: TransitionTo(Segment, new SegmentPayload(same segment, IsRestart: true))
- Does not re-roll the segment — same generated content, same seed

### BossIntro
- Plays villain reveal animation/cinematic
- No player control during intro
- On complete: TransitionTo(BossFight, same BossFightPayload)

### BossFight
- Boss-specific combat encounter
- Same death rules as Segment (ReviveWindow, SegmentRestart)
- On boss defeated: TransitionTo(VillainExit)

### VillainExit
- Plays villain defeat sequence
- Triggers The Laugh beat (audio + shadow silhouette, never skippable)
- On complete: RunSpine.Advance() → next step (genre level or next chapter or RunSummary)

### GradiusLevel / BrawlerLevel
- Full mode switch — load dedicated scene, swap input mapping, swap camera
- Self-contained — does not use the segment system or TileMap importer
- On complete: RunSpine.Advance() → chapter intro cinematic
- These scenes are modules, not generated content

### RunSummary
- Display run stats, time, kills, deaths
- Reveal any unlocks earned this run (cosmetics, weapon skins)
- Route to Title, LoadoutSelect (run again), or Credits

### LevelEditor
- Standalone tool — loads independently of any run state
- Reads/writes the same JSON schema as the generator
- Can preview any segment via the importer
- Can inject a segment into a test run for live playtesting
- Only exit: back to Title

---

## The Cinematic System in Detail

Cinematics are reentrant and caller-agnostic. The caller always specifies where to return. The cinematic system never decides what comes next.

```
Caller: TransitionTo(Cinematic, new CinematicPayload(
    CinematicKey: "cinematic_chapter1_intro",
    ReturnState: GameState.Segment,
    ReturnPayload: new SegmentPayload(firstSegment, false)
))

Cinematic plays.

On complete: TransitionTo(ReturnState, ReturnPayload)
→ TransitionTo(Segment, new SegmentPayload(firstSegment, false))
```

This means the same cinematic system handles chapter intros, boss intros, villain exits, and The Laugh without any special-casing. The Laugh is simply a cinematic with a very short runtime, a return to whatever comes next in the spine, and a rule in the content data that it is never skippable.

The "never skippable" rule is enforced in the cinematic payload:

```csharp
public record CinematicPayload(
    string CinematicKey,
    GameState ReturnState,
    StatePayload ReturnPayload,
    bool Skippable = true       // The Laugh sets this to false
) : StatePayload;
```

---

## Genre Level Mode Switch in Detail

Gradius and Brawler are full mode switches. "Full mode switch" means:

1. The main gameplay scene is unloaded
2. The genre level scene is loaded in its place
3. Input mappings are swapped (Gradius uses ship controls, Brawler uses top-down brawler controls)
4. Camera mode changes (Gradius: auto-scroll, Brawler: top-down follow)
5. The genre level runs entirely within its own scene — it does not use the TileMap importer, the WFC generator, or the segment system
6. On complete, the genre level scene is unloaded, the main scene is reloaded, and the spine advances

The genre level scenes are self-contained modules. They communicate with `GameStateManager` via one signal: `GenreLevelCompleted`. The manager handles the transition.

```csharp
// Inside GradiusLevel.tscn script:
private void OnLevelComplete()
{
    GetNode<GameStateManager>("/root/GameStateManager")
        .TransitionTo(GameState.Cinematic, new CinematicPayload(
            "cinematic_chapter2_intro",
            GameState.Segment,
            new SegmentPayload(spineNextSegment, false)
        ));
}
```

---

## The Attract Mode in Detail

Attract mode plays if the title screen sits idle for `AttractIdleSeconds` (default: 60). It is not a real run.

Two implementation options, in order of preference:

1. **Recorded spine playback** — record a real run's input stream alongside its `RunData`. Attract mode replays the input against the deterministic simulation. Perfectly faithful, requires one recorded session per game version.

2. **Scripted demo spine** — a hand-authored `RunData` JSON with a fixed seed, played with AI-driven or pre-scripted input. Easier to produce, slightly less faithful.

Either way, attract mode loads as `GameState.Attract`, uses a separate `RunSpine` instance with `IsAttractMode: true`, and any player input immediately fires `TransitionTo(Title)`. No run state is modified. No unlocks are awarded.

The attract implementation is deferred to post-prototype. The state and transition are defined now so nothing needs to be restructured later.

---

## Death and Revival Rules (Complete Specification)

The bible is explicit: death is not instant failure. This is the complete ruleset.

**One player down (1–3 players remaining alive):**
- `TransitionTo(ReviveWindow)`
- Downed player enters a downed animation, lies in place
- Living players can reach downed player and hold revive input
- Revive time: 2 seconds of held input (not configurable per character — applies equally to all)
- Enemies and hazards continue running — the game does not pause
- If revived: `GameStateManager.ExitReviveWindow()` — returns to the state that was active before the revive window (Segment or BossFight). Downed player respawns with 1 HP.
- If countdown expires with survivors: downed player is eliminated from the run; `ExitReviveWindow()` continues with remaining players
- If countdown expires with no survivors: `TransitionTo(SegmentRestart)`

**Implementation note:** Revive is handled entirely by `ReviveSystem` — a child `Node` of `LevelController` — not by a separate `GameStateNode`. `ReviveSystem` owns the countdown timer and proximity check. On expiry with no survivors it calls `_gameState.TransitionTo<SegmentRestartState>()`. On successful revive or expiry-with-survivors, `LevelController` calls `_roster.MarkRevived()` or `_roster.EliminateDownedPlayers()` and continues the level. There is no `ReviveWindowState`, no `ExitReviveWindow()` method, and no `StateBeforeRevive` in `GameStateContext`. This keeps game-world knowledge (who is alive, who is near whom) out of the state machine entirely.

**All players down simultaneously:**
- `TransitionTo(SegmentRestart)` directly — no ReviveWindow
- Brief freeze frame (0.5s)
- Brief "segment restart" animation (1.0s)
- `TransitionTo(Segment, new SegmentPayload(sameSegment, IsRestart: true))`
- Players respawn at segment entry point with full HP

**Solo play:**
- ReviveWindow does not exist — one player down = all players down
- Goes directly to SegmentRestart

**Edge case — player dies during ReviveWindow:**
- If last living player dies while another is in ReviveWindow: transition to SegmentRestart
- ReviveWindow resolves immediately, no additional countdown

---

## How State Transitions Are Triggered

Transitions are always called via `_gameState.TransitionTo<T>()`. The initiating node calls the manager directly — there is no string-based signal bus for state transitions.

**Implemented signal → transition wiring (current):**

```csharp
// PlayerController emits [Signal] WentDown — LevelController subscribes and decides:
player.WentDown += () => OnPlayerWentDown(player);

private void OnPlayerWentDown(PlayerController player)
{
    _gameState.NotifyPlayerDeath();
    PlayerRoster.DownResult result = _roster.MarkDown();
    if (result == PlayerRoster.DownResult.AllDown)
        _gameState.TransitionTo<SegmentRestartState>();
    else
        _reviveSystem.StartWindow(player, _players);  // ReviveSystem handles timer + proximity
}

// ReviveSystem (child of LevelController) emits C# events:
_reviveSystem.WindowExpired += OnReviveWindowExpired;
private void OnReviveWindowExpired()
{
    _roster.EliminateDownedPlayers();
    if (_roster.AliveCount == 0)
        _gameState.TransitionTo<SegmentRestartState>();
}

// ExitTrigger calls the manager directly:
_gameState.TransitionTo<RunSummaryState>();

// EnemyHost TriggerDeath calls NotifyEnemyKilled — boss death is handled by BossDeath behavior:
// BossDeath.Execute → GetTree().CreateTimer(1s) → gsm.TransitionTo<VillainExitState>()
```

**Design principle:** The engine fires C# events and Godot signals. Scene systems (`LevelController`, `ReviveSystem`, behavior nodes) subscribe to those events and decide when to call `TransitionTo`. The state machine does not know what caused the transition — only that it is legal.

---

## What Is Not in This Document

- Villain-specific boss fight state machines (deferred to villain design session)
- Setpiece event sequences within segments (deferred to setpiece design session)
- Workshop browser state detail (post-launch, deferred)
- Exact attract mode implementation (post-prototype, deferred)
- Final boss state machine (deferred per bible)

When those sessions happen, add the resulting states and transitions to this document. The state enum and legal transition table must be kept in sync with this document at all times.
