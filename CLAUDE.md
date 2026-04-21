# CLAUDE.md — Feral Frenzy
**Read this file completely before touching any code.**  
**Read docs/01_schema.md, docs/02_state_machine.md, and docs/00_implementation_plan.md before starting any task.**

---

## What This Project Is

Feral Frenzy is a chaotic co-op run-and-gun platformer. Four animal characters. Three chapters. Free always. Local co-op only. Built in Godot 4 C# (.NET 8). Solo developer.

The game bible lives at `docs/FERAL_FRENZY_BIBLE.md`. It is the design constitution. When any code decision feels ambiguous, the bible resolves it.

---

## Current Build Phase

**Check `docs/00_implementation_plan.md` for the current phase before starting any task.**

The phase determines what is in scope. Do not build Phase 3 systems during Phase 1. Do not anticipate Phase 4 requirements during Phase 2. The plan exists to prevent the infrastructure from eating the game.

If a task would require building something outside the current phase, say so explicitly before proceeding. Do not silently build ahead.

---

## Solution Structure

```
FeralFrenzy.sln
  FeralFrenzy/                  ← Godot project
    FeralFrenzy.csproj
    src/
      godot/                    ← everything that touches Godot APIs
        autoloads/              ← GameStateManager, AssetRegistry, InputManager
        characters/             ← CharacterBody2D controllers
        enemies/                ← enemy scene controllers  
        weapons/                ← weapon scene controllers
        ui/                     ← HUD, menus, overlays
        camera/                 ← shared-screen co-op camera
        importer/               ← JSON → TileMap/scene instantiation
    scenes/
      characters/               ← one .tscn per character
      enemies/                  ← one .tscn per enemy type
      weapons/                  ← one .tscn per weapon
      world/                    ← Level.tscn, chapter wrappers
      ui/
      genre_levels/             ← Gradius.tscn, Brawler.tscn (self-contained)
    assets/
      sprites/
      audio/
      fonts/
    data/
      characters/               ← CharacterDefinition .tres files
      weapons/                  ← WeaponDefinition .tres files
      chapters/                 ← chapter definition JSON
      enemies/                  ← enemy definition JSON
      assets_manifest.json      ← all asset keys → paths

  FeralFrenzy.Core/             ← plain .NET 8 class library, zero Godot dependencies
    FeralFrenzy.Core.csproj
    src/
      core/
        data/
          engine/               ← SegmentData, RunData, enums, ValidationResult
          content/              ← FFCharacterDefinition, FFWeaponDefinition, etc.
          migration/            ← schema version migration handlers
        generator/              ← ChapterGenerator, MacroValidator, RunSpine steps
        constants/              ← SolvabilityConstants, all named constants

  FeralFrenzy.Tests/            ← xUnit test project
    FeralFrenzy.Tests.csproj
    generator/                  ← generator and validator tests
    data/                       ← schema serialization tests
    fixtures/                   ← seed fixtures, known-good RunData JSON
```

---

## The Architectural Boundary — The Most Important Rule

**`FeralFrenzy.Core` has zero Godot API dependencies. Always. No exceptions.**

If a type in `src/core/` imports anything from `Godot`, `GodotSharp`, or `Godot.Collections`, it is an architectural violation. The core library must compile as a plain .NET class library with no Godot runtime present.

**The `FF` prefix rule:** All content-layer types are prefixed `FF` (e.g. `FFCharacterDefinition`, `FFWeaponDefinition`, `FFChapterDefinition`). Engine-layer types have no prefix (e.g. `SegmentData`, `RunData`, `ValidationResult`). If you see an `FF`-prefixed type in engine-layer code or in `FeralFrenzy.Core/src/core/data/engine/`, it is a boundary violation.

**The test for engine-layer code:** Would this code make sense in a game about space trading? If yes, it may belong in the engine layer. If it contains the word "dinosaur", "honey badger", "lava", "cretaceous", or anything Feral Frenzy-specific, it belongs in the content layer.

The importer (`src/godot/importer/`) is the only place the two layers touch. It translates content schema → engine schema. Nothing else crosses this boundary.

---

## Code Quality — Non-Negotiable from Day One

### .editorconfig

A `.editorconfig` lives at the solution root and is not optional. All formatting rules come from it. Do not manually format code — let the tooling enforce it.

Key rules already configured:
- Indent style: spaces, size 4
- `using` directives: outside namespace
- `var` usage: prefer explicit types for non-obvious cases
- Trailing whitespace: trimmed
- Final newline: required

### Static Analysis — Roslyn Analyzers

Both `FeralFrenzy.csproj` and `FeralFrenzy.Core.csproj` include these analyzer packages. Do not remove them.

```xml
<PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.*" />
<PackageReference Include="StyleCop.Analyzers" Version="1.2.*">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
<PackageReference Include="Roslynator.Analyzers" Version="4.*">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

A `stylecop.json` lives at the solution root. Configured for this project's conventions — do not override it inline.

### Warnings as Errors

```xml
<!-- In both .csproj files -->
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<Nullable>enable</Nullable>
<WarningLevel>9999</WarningLevel>
```

**Nullable is enabled.** Every nullable reference type violation is a compile error. There is no `!` suppression without a comment explaining why. `null` is never returned from a method that doesn't declare `T?` as its return type.

### No Suppression Without Justification

```csharp
// NEVER:
#pragma warning disable CS8600

// ALLOWED (with justification):
#pragma warning disable CS8600 // Godot's GetNode returns null for unconnected exports at edit time — validated in _Ready
#pragma warning restore CS8600
```

If you find yourself suppressing a warning, stop and fix the underlying issue instead. If it genuinely cannot be fixed (Godot interop edge case), add the comment.

### No Magic Strings

```csharp
// NEVER:
_sprite.Play("walk");
GetNode<Node>("AnimatedSprite2D");
Input.IsActionJustPressed("jump");

// ALWAYS:
_sprite.Play(AnimationNames.Walk);
GetNode<AnimatedSprite2D>(NodePaths.AnimatedSprite);
Input.IsActionJustPressed(InputActions.Jump);
```

All animation names live in `src/godot/constants/AnimationNames.cs`.
All node paths live in `src/godot/constants/NodePaths.cs`.
All input action names live in `src/godot/constants/InputActions.cs`.
All asset registry keys live in `src/godot/constants/AssetKeys.cs`.

These files are the authoritative source. When the game bible defines something by name, it goes here.

### No Hardcoded Asset Paths

```csharp
// NEVER:
GD.Load<Texture2D>("res://assets/sprites/characters/bear.png");

// ALWAYS:
_assetRegistry.Load<Texture2D>(AssetKeys.SpriteBear);
```

Every asset reference goes through `AssetRegistry`. Every key is defined in `AssetKeys.cs`. Every path is in `data/assets_manifest.json`. This is the entire custom skin system — do not undermine it.

### No Magic Numbers

```csharp
// NEVER:
if (difficultyBudget > 0.25f) ...
velocity.Y = -280f;

// ALWAYS:
if (difficultyBudget > SolvabilityConstants.MaxDifficultyRampPerSegment) ...
velocity.Y = _definition.JumpVelocity;
```

Numbers that come from the design live in `CharacterDefinition`, `WeaponDefinition`, or `SolvabilityConstants`. Numbers that are implementation details live as named `private const` values with a comment.

---

## Testing

### Structure

Tests live in `FeralFrenzy.Tests/`. The test project references `FeralFrenzy.Core` only — never `FeralFrenzy` (the Godot project). Godot systems are not unit tested; they are integration tested by running the game.

```
FeralFrenzy.Tests/
  generator/
    ChapterGeneratorTests.cs
    MacroValidatorTests.cs
    RunSpineTests.cs
  data/
    SegmentDataSerializationTests.cs
    RunDataRoundTripTests.cs
  fixtures/
    known_good_run_seed_12345.json    ← a validated RunData snapshot
    known_good_run_seed_99999.json
```

### What Gets Tested

**Generator output correctness:**
- Given a seed, the generator always produces the same output (determinism)
- No two segments in a run share a `UniqueMechanicTag`
- `DifficultyBudget` never jumps more than `MaxDifficultyRampPerSegment` between consecutive segments
- Exactly one segment per run has `DestructibleLevel.Heavy`
- The Heavy segment is always in `chapter_dead_station`
- 50% of seeds produce a second destructible segment (test over 1000 seeds)
- Every segment is passable by a sliding Croc (minimum gap width on critical path)
- The run always ends with a Boss segment

**Schema round-trips:**
- `SegmentData` serializes to JSON and deserializes back to an identical record
- `RunData` serializes to JSON and deserializes back to an identical record
- Known-good fixture files deserialize without errors across schema versions

**Validator rules:**
- Each rule in `MacroValidator` has at least one test for the passing case and one for the failing case
- `ValidationResult` errors include the segment ID and the violated rule name

### Test Naming Convention

```csharp
// Format: MethodName_Condition_ExpectedResult
[Fact]
public void Generate_WithSeed12345_ProducesDeterministicOutput() { }

[Fact]
public void Validate_WhenTwoSegmentsShareMechanicTag_ReturnsError() { }

[Fact]
public void Validate_WhenHeavyDestructibleInWrongChapter_ReturnsError() { }
```

### Running Tests

```bash
dotnet test FeralFrenzy.Tests/
```

Tests must pass before every commit. If a test is failing and you cannot fix it in the current task, do not commit — flag the failure explicitly.

### Snapshot / Fixture Tests

The `fixtures/` directory contains known-good `RunData` JSON files validated by hand. When the generator is modified, run the fixture tests. If a fixture fails after a valid change (e.g. schema migration), update the fixture with a comment explaining what changed and why.

---

## C# Conventions

### Records for Immutable Data

```csharp
// All data types that cross system boundaries are records.
public record SegmentData(string SegmentId, SegmentType Type, ...);

// Mutable game state is classes.
public partial class PlayerController : CharacterBody2D { }
```

### Partial Classes for All Godot Nodes

```csharp
// Always partial. Godot generates a partial counterpart for [Export] wiring.
public partial class PlayerController : CharacterBody2D { }
public partial class GameStateManager : Node { }

// Forgetting partial = cryptic compile error. Don't forget partial.
```

### Exports Over GetNode

```csharp
// PREFER: typed exports wired in the Godot editor
[Export] private AnimatedSprite2D _sprite;
[Export] private CollisionShape2D _collisionShape;

// AVOID: string-based node lookup (use only when export is not possible)
var sprite = GetNode<AnimatedSprite2D>(NodePaths.AnimatedSprite);
```

### Null Safety in _Ready

All `[Export]` fields must be validated in `_Ready`:

```csharp
public override void _Ready()
{
    if (_sprite is null)
        throw new InvalidOperationException($"{nameof(PlayerController)}: _sprite export is not assigned.");
    if (_collisionShape is null)
        throw new InvalidOperationException($"{nameof(PlayerController)}: _collisionShape export is not assigned.");
}
```

This surfaces wiring mistakes at startup, not during gameplay.

### Signal Connections

```csharp
// PREFER: typed signal connections
_sprite.AnimationFinished += OnAnimationFinished;

// NEVER: string-based Connect (legacy GDScript style)
_sprite.Connect("animation_finished", new Callable(this, nameof(OnAnimationFinished)));
```

### File Naming

| Type | Convention | Example |
|---|---|---|
| Godot node script | `PascalCase.cs` matching class name | `PlayerController.cs` |
| Core data type | `PascalCase.cs` matching class name | `SegmentData.cs` |
| Content type | `FFPascalCase.cs` | `FFCharacterDefinition.cs` |
| Godot scene | `PascalCase.tscn` matching root node | `Bear.tscn` |
| Content JSON | `snake_case.json` | `chapter_cretaceous.json` |
| Resource file | `PascalCase_descriptor.tres` | `Bear_character.tres` |
| Test file | `ClassNameTests.cs` | `MacroValidatorTests.cs` |

---

## Game-Specific Rules

### The Three-Layer Separation Rule — The Most Important Architectural Rule

The engine has zero knowledge of this game. Three layers exist and must stay separate:

```
┌─────────────────────────────────────┐
│         CONTENT LAYER               │
│  Feral Frenzy-specific, in JSON     │
│  Characters, enemies, weapons,      │
│  chapters, villains, tilesets       │
└──────────────┬──────────────────────┘
               │ feeds
┌──────────────▼──────────────────────┐
│         IMPORTER LAYER              │
│  The only place Godot touches       │
│  game content. Translates content   │
│  schema → engine schema.            │
└──────────────┬──────────────────────┘
               │ drives
┌──────────────▼──────────────────────┐
│         ENGINE LAYER                │
│  Game-agnostic, reusable.           │
│  Physics, camera, WFC, input,       │
│  signal bus, save system            │
└─────────────────────────────────────┘
```

**The test:** Would this code make sense in a game about space trading? If yes, engine layer. If it mentions dinosaurs, honey badgers, lava, or anything Feral Frenzy-specific, content layer. The importer is the only crossing point.

**In practice:**
- Bear, Croc, Hammerhead, Honey Badger are defined in content-layer JSON and `.tres` files — not in engine C# logic
- Chapter definitions are data-driven JSON — the engine does not know chapters exist
- Godot scenes are rendering containers only — game logic does not live in `.tscn` files
- The signal bus fires engine events (`entity_spawned`, `player_died`, `segment_loaded`) — Feral Frenzy listeners respond to those signals, the engine does not know who is listening
- The `FF` prefix on all content-layer C# types makes boundary violations immediately visible

Any line of engine code containing a Feral Frenzy-specific name is an architectural violation.

### The Resolution Rule — Locked, Do Not Revisit

**Base resolution: 320×180. This is final.**

Rationale: scales to 1080p at exactly 6×, 720p at exactly 4×, 4K at exactly 12×, Steam Deck (1280×800) at 4× letterboxed. All integer multiples — pixels stay perfectly square on all target displays. Alternatives (384×216, 426×240) break 720p integer scaling. The couch/large TV readability requirement is solved by art direction (high contrast silhouettes, saturated colors, identifiable shapes at thumbnail size) not by adding pixels.

In Godot project settings:
```
Display → Window → Viewport Width  = 320
Display → Window → Viewport Height = 180
Display → Window → Mode            = canvas_items
Display → Window → Aspect          = keep
Rendering → Textures → Default Texture Filter = Nearest
Rendering → 2D → Snap 2D Transforms to Pixel  = On
Rendering → 2D → Snap 2D Vertices to Pixel    = On
```

Do not change these settings. Do not add resolution options. Do not propose alternative base resolutions.

### The Solvability Rule — Enforced Always

Every level, segment, and generated layout must be passable by a sliding Croc. Croc is the solvability reference character: Huge size, slow speed, lower jump arc. If a layout is passable by a sliding Croc, it is passable by all characters.

This is `SolvabilityConstants.ReferenceCharacterKey = "char_croc"`. The generator enforces it. The validator checks it. When writing any geometry-producing code, ask: can a sliding Croc get through this?

### The Weapon Rule

Eight-directional aiming is non-negotiable for all ranged weapons. No exceptions. `FFWeaponDefinition.EightDirectional` is always `true`. If you are writing weapon code that does not support eight directions, stop and reconsider.

Damage is never a flat value. Damage = weapon base impact × `FFCharacterDefinition.WeaponDamageMultiplier`. Size is the affinity. There is no separate damage stat per character.

### The Generator Rule

The generator is deterministic. Given the same seed, it always produces the same `RunData`. No `Random.Shared`, no `DateTime.Now`, no GUID generation inside generator code. All randomness flows from a seeded `System.Random` instance passed into the generator as a parameter.

```csharp
// NEVER inside generator code:
var value = Random.Shared.Next();
var id = Guid.NewGuid().ToString();

// ALWAYS:
public RunData Generate(int seed, FFChapterDefinition[] chapters, int playerCount)
{
    var rng = new Random(seed);
    ...
}
```

### The State Machine Rule

State transitions go through `GameStateManager.TransitionTo()`. Always. No scene directly loads another scene. No node directly changes game state. The manager is the single source of truth.

If you are writing code that changes what the game is doing without calling `TransitionTo`, you are bypassing the state machine. Stop and route through the manager.

### The Asset Registry Rule

No `GD.Load<T>()` calls with literal path strings. Every asset goes through `AssetRegistry`. Every key is defined in `AssetKeys.cs`. If an asset key doesn't exist yet, add it to both `AssetKeys.cs` and `data/assets_manifest.json` as the first step of the task.

### The Scene Structure Rule — Established Phase 1, Never Changed

The main scene wraps the game world in a `SubViewport`. This is not optional and must never be flattened. The structure is:

```
Main (Node)
  └── SubViewport          ← game world renders here
      └── Level (Node2D)
          ├── TileMap
          └── Players / Enemies / Weapons
  └── CanvasLayer (UI)     ← HUD, menus — always above SubViewport, never inside it
```

This exists because screen-space shader effects (screen ripple on explosions, Bear's Roar shockwave, hit flash, lava glow, the silhouette level mechanic) require the game world to render into a `SubViewport` so a shader can be applied to the entire viewport texture. Shaders on the `SubViewport` do not affect the `CanvasLayer` UI — which is correct behaviour.

If you are writing scene structure code that puts gameplay nodes outside the `SubViewport`, or UI nodes inside it, stop and fix it.

Shader assets live in `assets/shaders/` and are referenced through `AssetRegistry` like all other assets.

### The Entity Density Rule — Performance Budget Established Phase 1

The game targets **20–30 visible entities simultaneously** at peak chaos. This is a hard design target from the bible, not a suggestion.

This means:
- Entity pooling/recycling is planned from Phase 1, not retrofitted later
- A density stress test (spawn 30 entities with basic AI, confirm frame budget holds) is a Phase 1 exit milestone
- The camera system must be validated at this density — 30 small sprites in motion must remain readable
- Minimum spec hardware to benchmark against: TBD, but should include a mid-range laptop GPU

Do not defer performance validation to Phase 4. If the architecture cannot hit 30 entities at 60fps by the end of Phase 1, it needs to be fixed before the generator exists.

### The Parallax Rule — Content-Layer Configured, Phase 1 Deliverable

Parallax backgrounds are required for all three chapters. They are not an afterthought.

- Parallax layer definitions belong in the **content-layer JSON** for each chapter — not hardcoded in scenes
- Chapter 1: clouds, distant mountains, pterodactyls on the horizon
- Chapter 2: viewport windows showing distant space
- Chapter 3: distant volcanic eruptions, falling ash

Parallax must work correctly inside the `SubViewport` wrapping — verify no shader or camera conflicts when implementing. The parallax system is a named engine-layer feature with content-layer configuration, established in Phase 1.

### The Camera Rule — Panoramic Scale, Validated Early

The visual target is cinematic and panoramic — Braveheart wide battlefield, multiple vertical layers of activity simultaneously, horizon suggesting more world beyond screen edges. The camera must be far enough back that the play area feels like a battlefield, not a corridor.

- Validate camera zoom level against couch readability in Phase 1 — a camera that sits too close defeats the panoramic philosophy
- Validate UI element sizes on a large display in Phase 1 or Phase 2 — elements sized for a monitor are unreadable at couch distance
- Vertical layer depth (enemies above, below, and level with the player simultaneously) is a generator and level design constraint, not just a visual aspiration
- The wide camera means segment dimensions in the generator must reflect a large play area

### The Asset Pipeline Rule — Established Workflow

The confirmed sprite/animation pipeline is:
- **Gemini** → sprite generation (validated against art style target — Croc confirmed)
- **Grok** → animation (walk loops confirmed working)

This pipeline is documented here so it does not need to be rediscovered. All generated sprites must pass the art style check before being imported: identifiable silhouette, saturated palette, readable at thumbnail size, matches the target between JP2 grimness and Joe & Mac looseness.

Sprites live in `assets/sprites/`, registered in `data/assets_manifest.json`, accessed through `AssetRegistry`.

---

## What Claude Code Should Never Do

- Add `using Godot;` to any file in `FeralFrenzy.Core/`
- Use `GD.Load<T>()` with a hardcoded path string
- Write a state transition without going through `GameStateManager`
- Suppress a warning without a justification comment
- Use `null!` or `default!` without a comment explaining the Godot interop reason
- Write a magic string for an animation name, node path, or input action
- Write a magic number without naming it
- Build Phase N+1 features during Phase N
- Generate a random value inside generator code without using the seeded `rng` parameter
- Create a `.tscn` file that contains game logic instead of rendering configuration
- Write enemy, weapon, or character names as string literals in engine-layer code
- Skip writing tests for any generator or validator component
- Put gameplay nodes outside the `SubViewport` in the main scene
- Put UI nodes inside the `SubViewport` — UI always lives on the `CanvasLayer` above it
- Flatten the `SubViewport` wrapping to "simplify" scene structure
- Change the base resolution from 320×180 — this is locked
- Propose alternative base resolutions
- Hardcode parallax layer definitions in scenes — parallax is content-layer JSON
- Write a Feral Frenzy-specific name (character, enemy, chapter) in engine-layer code
- Defer entity pooling to a later phase — it is a Phase 1 architecture decision

---

## How to Start a Task

Every Claude Code session starts with:

1. Read this file
2. Read `docs/00_implementation_plan.md` — confirm the current phase
3. Read the relevant architecture document for the system being built
4. Check `docs/FERAL_FRENZY_BIBLE.md` for any design rules that apply
5. State the task, confirm it is in-phase, then implement

End every session by:

1. Running `dotnet build "Feral Frenzy.sln"` — must be clean
2. Running `dotnet test FeralFrenzy.Tests/` — must pass
3. Running `dotnet format "Feral Frenzy.sln" --verify-no-changes` — must be clean
4. Updating `DEVLOG.md` with what was built, what decisions were made, and what is next

---

## DEVLOG.md

A `DEVLOG.md` lives at the project root. Every session appends an entry:

```markdown
## YYYY-MM-DD — [brief description]

**Phase:** 1  
**Built:** What was actually implemented  
**Decisions:** Any non-obvious choices made and why  
**Deferred:** Anything explicitly pushed to a later phase  
**Next:** The logical next task  
**Tests added:** Which test files were created or modified
```

This is the project memory. It is how future sessions pick up without relitigating decisions. It is also how you (the developer) track what Claude Code actually did vs. what you expected.

---

## Quick Reference

| Question | Answer |
|---|---|
| Where is the design constitution? | `docs/FERAL_FRENZY_BIBLE.md` |
| Where is the build order? | `docs/00_implementation_plan.md` |
| Where is the data schema? | `docs/01_schema.md` |
| Where is the state machine spec? | `docs/02_state_machine.md` |
| What is the base resolution? | 320×180 — locked, never change |
| What is the peak entity target? | 20–30 simultaneous visible entities |
| What character is the solvability reference? | Croc (`char_croc`) |
| How many directions do weapons aim? | 8. Always. |
| Where do asset paths live? | `data/assets_manifest.json` |
| Where do asset keys live? | `src/godot/constants/AssetKeys.cs` |
| Where do animation names live? | `src/godot/constants/AnimationNames.cs` |
| Where do input action names live? | `src/godot/constants/InputActions.cs` |
| What prefix on content-layer types? | `FF` |
| Can core library import Godot? | Never |
| How is randomness handled in generator? | Seeded `System.Random`, passed as parameter |
| How do state transitions happen? | `GameStateManager.TransitionTo()` only |
| What wraps the game world in the main scene? | `SubViewport` — never flatten this |
| Where does UI live? | `CanvasLayer` above the `SubViewport` — never inside it |
| Where do parallax definitions live? | Content-layer JSON per chapter — never hardcoded |
| What is the sprite generation pipeline? | Gemini (sprites) → Grok (animation) |
| What runs before every commit? | `dotnet build "Feral Frenzy.sln"` + `dotnet test FeralFrenzy.Tests/` + `dotnet format "Feral Frenzy.sln" --verify-no-changes` |
