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

## The State Enum

```csharp
// src/godot/autoloads/GameState.cs
public enum GameState
{
    // ── Top-level modes ──────────────────────────────
    Title,              // main menu — new run, editor, credits, options
    Attract,            // plays if title is idle for AttractIdleSeconds
    LoadoutSelect,      // character + weapon selection, 1–4 players

    // ── Run spine states ─────────────────────────────
    Segment,            // active gameplay — enemies, hazards, movement
    BossIntro,          // villain reveal moment before the fight
    BossFight,          // chapter boss encounter
    VillainExit,        // defeat cinematic + The Laugh beat
    GradiusLevel,       // full mode switch — horizontal shooter
    BrawlerLevel,       // full mode switch — top-down beat em up

    // ── Nested / transient (within Segment or BossFight) ──
    ReviveWindow,       // one player down, teammates can revive
    SegmentRestart,     // all players died — brief pause before retry

    // ── Cinematic (reentrant) ────────────────────────
    Cinematic,          // chapter intro, boss intro lead-in, villain exit

    // ── Post-run ─────────────────────────────────────
    RunSummary,         // stats, unlock reveals, meta progression feedback

    // ── Utility / standalone ─────────────────────────
    LevelEditor,        // same JSON as generator, standalone tool
    Credits,            // accessible from title or post-run
    WorkshopBrowser     // post-launch, community levels (deferred)
}
```

---

## The GameStateManager Autoload

```csharp
// src/godot/autoloads/GameStateManager.cs
// Registered as autoload "GameStateManager" in Godot project settings.
public partial class GameStateManager : Node
{
    // Current state. Read-only externally.
    public GameState Current { get; private set; } = GameState.Title;

    // The state we will return to after a reentrant state (e.g. Cinematic) completes.
    private GameState _returnState;

    // The active run spine. Null when not in a run.
    private RunSpine _activeSpine;

    // Emitted after every transition. UI and systems listen to this.
    [Signal] public delegate void StateChangedEventHandler(GameState from, GameState to);

    // The only way to change state.
    public void TransitionTo(GameState next, StatePayload payload = null) { ... }

    // Convenience: is the game currently in any run spine state?
    public bool IsInRun => _activeSpine != null;
}
```

### Transition Rules

`TransitionTo` validates every transition against the legal transition table before executing it. An illegal transition throws a descriptive error in debug builds and logs a warning in release builds — it does not silently succeed.

```csharp
private static readonly Dictionary<GameState, HashSet<GameState>> LegalTransitions = new()
{
    [GameState.Title] = new() {
        GameState.Attract,
        GameState.LoadoutSelect,
        GameState.LevelEditor,
        GameState.Credits,
        GameState.WorkshopBrowser
    },
    [GameState.Attract] = new() {
        GameState.Title           // any input returns to Title
    },
    [GameState.LoadoutSelect] = new() {
        GameState.Title,          // back out
        GameState.Cinematic       // chapter 1 intro fires immediately
    },
    [GameState.Cinematic] = new() {
        GameState.Segment,        // chapter intro → first segment
        GameState.BossIntro,      // cinematic leading into boss
        GameState.BossFight,      // boss intro cinematic → fight
        GameState.VillainExit,    // defeat cinematic fires
        GameState.RunSummary,     // end-of-run cinematic → summary
        GameState.Title           // cinematic skipped or abandoned
    },
    [GameState.Segment] = new() {
        GameState.ReviveWindow,   // one player down
        GameState.SegmentRestart, // all players down
        GameState.BossIntro,      // spine advances to boss
        GameState.GradiusLevel,   // spine advances to genre level 1
        GameState.BrawlerLevel,   // spine advances to genre level 2
        GameState.Cinematic,      // setpiece triggers a cinematic beat
        GameState.Segment         // spine advances to next segment (self-transition)
    },
    [GameState.ReviveWindow] = new() {
        GameState.Segment,        // player revived — resume
        GameState.SegmentRestart  // timer expired, all players now down
    },
    [GameState.SegmentRestart] = new() {
        GameState.Segment         // restart animation complete — retry
    },
    [GameState.BossIntro] = new() {
        GameState.BossFight       // intro complete
    },
    [GameState.BossFight] = new() {
        GameState.ReviveWindow,   // one player down mid-fight
        GameState.SegmentRestart, // all players down mid-fight
        GameState.VillainExit     // boss defeated
    },
    [GameState.VillainExit] = new() {
        GameState.Cinematic,      // The Laugh beat fires
        GameState.GradiusLevel,   // Ch1 boss defeated → commute
        GameState.BrawlerLevel,   // Ch2 boss defeated → layover
        GameState.Segment,        // Ch3 boss defeated → final boss approach
        GameState.RunSummary      // final boss defeated → run ends
    },
    [GameState.GradiusLevel] = new() {
        GameState.Cinematic,      // genre level complete → chapter intro
        GameState.RunSummary      // player abandoned run
    },
    [GameState.BrawlerLevel] = new() {
        GameState.Cinematic,      // genre level complete → chapter intro
        GameState.RunSummary      // player abandoned run
    },
    [GameState.RunSummary] = new() {
        GameState.Title,          // return to menu
        GameState.LoadoutSelect,  // run again immediately
        GameState.Credits         // accessible from summary screen
    },
    [GameState.LevelEditor] = new() {
        GameState.Title           // only exit is back to title
    },
    [GameState.Credits] = new() {
        GameState.Title,
        GameState.RunSummary      // back to summary if arrived from there
    },
    [GameState.WorkshopBrowser] = new() {
        GameState.Title
    }
};
```

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
- If revived: `TransitionTo(Segment)` — downed player respawns with 1 HP
- If countdown expires: `TransitionTo(SegmentRestart)`

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

## What the Signal Bus Carries

The engine fires signals. Feral Frenzy listens to them. The `GameStateManager` is one of the listeners.

```csharp
// Engine signals that trigger state transitions (examples):
// "segment_exit_reached"     → Segment calls RunSpine.Advance()
// "player_died"              → Segment checks death state, may TransitionTo(ReviveWindow)
// "all_players_died"         → Segment or BossFight calls TransitionTo(SegmentRestart)
// "boss_defeated"            → BossFight calls TransitionTo(VillainExit)
// "genre_level_completed"    → GradiusLevel/BrawlerLevel calls TransitionTo(Cinematic)
// "cinematic_completed"      → Cinematic calls TransitionTo(ReturnState, ReturnPayload)
// "revive_timer_expired"     → ReviveWindow calls TransitionTo(SegmentRestart)
// "player_revived"           → ReviveWindow calls TransitionTo(Segment)
```

The engine fires these signals without knowing who is listening. The state-specific controllers register listeners on entry and deregister on exit. This is the Godot signal system used as the seam between engine and content layers.

---

## What Is Not in This Document

- Villain-specific boss fight state machines (deferred to villain design session)
- Setpiece event sequences within segments (deferred to setpiece design session)
- Workshop browser state detail (post-launch, deferred)
- Exact attract mode implementation (post-prototype, deferred)
- Final boss state machine (deferred per bible)

When those sessions happen, add the resulting states and transitions to this document. The state enum and legal transition table must be kept in sync with this document at all times.
