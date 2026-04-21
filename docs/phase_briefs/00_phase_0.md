# Phase 0 Task Brief — Foundation
**For:** Claude Code  
**Status:** Ready to execute  
**Exit condition:** Godot project compiles clean, solution structure exists, Bear moves on screen.

---

## Before You Start

1. Read `CLAUDE.md` completely
2. Read `docs/00_implementation_plan.md` — confirm you are in Phase 0
3. Read `docs/01_schema.md` — you will be implementing the types defined there
4. Read `docs/02_state_machine.md` — you will be stubbing the autoloads defined there

Do not begin until you have read all four documents.

---

## Context

The Godot 4 project has already been created with the .NET template. The `.sln` and `.csproj` files exist. The project opens and compiles as a blank Godot project. Your job is to build the foundation everything else sits on — not to build gameplay, not to build the generator, not to build anything beyond what is explicitly listed here.

If something is not on the list, it does not get built in this task. Flag it as deferred in `DEVLOG.md`.

---

## Task 1 — Solution Structure

Restructure the solution to match the architecture:

```
FeralFrenzy.sln
  FeralFrenzy/                  ← existing Godot project
  FeralFrenzy.Core/             ← NEW: plain .NET 8 class library
  FeralFrenzy.Tests/            ← NEW: xUnit test project
```

### Steps

1. Create `FeralFrenzy.Core/` as a .NET 8 class library:
   ```bash
   dotnet new classlib -n FeralFrenzy.Core -f net8.0
   dotnet sln add FeralFrenzy.Core/FeralFrenzy.Core.csproj
   ```

2. Create `FeralFrenzy.Tests/` as an xUnit project:
   ```bash
   dotnet new xunit -n FeralFrenzy.Tests -f net8.0
   dotnet sln add FeralFrenzy.Tests/FeralFrenzy.Tests.csproj
   ```

3. Add project references:
   - `FeralFrenzy` references `FeralFrenzy.Core`
   - `FeralFrenzy.Tests` references `FeralFrenzy.Core` only — never `FeralFrenzy`

4. Add analyzer packages to both `FeralFrenzy.csproj` and `FeralFrenzy.Core.csproj`:
   ```xml
   <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.*" />
   <PackageReference Include="StyleCop.Analyzers" Version="1.2.*">
     <PrivateAssets>all</PrivateAssets>
   </PackageReference>
   <PackageReference Include="Roslynator.Analyzers" Version="4.*">
     <PrivateAssets>all</PrivateAssets>
   </PackageReference>
   ```

5. Add to both `FeralFrenzy.csproj` and `FeralFrenzy.Core.csproj`:
   ```xml
   <PropertyGroup>
     <Nullable>enable</Nullable>
     <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
     <WarningLevel>9999</WarningLevel>
     <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
   </PropertyGroup>
   ```

6. Create `.editorconfig` at the solution root:
   ```ini
   root = true

   [*.cs]
   indent_style = space
   indent_size = 4
   end_of_line = lf
   charset = utf-8
   trim_trailing_whitespace = true
   insert_final_newline = true

   # Using directives outside namespace
   dotnet_sort_system_directives_first = true
   csharp_using_directive_placement = outside_namespace

   # Prefer explicit types for non-obvious cases
   csharp_style_var_for_built_in_types = false:suggestion
   csharp_style_var_when_type_is_apparent = true:suggestion
   csharp_style_var_elsewhere = false:suggestion

   # Braces always
   csharp_prefer_braces = true:warning

   # Newline preferences
   csharp_new_line_before_open_brace = all
   csharp_new_line_before_else = true
   csharp_new_line_before_catch = true
   csharp_new_line_before_finally = true
   ```

7. Create `stylecop.json` at the solution root:
   ```json
   {
     "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",
     "settings": {
       "documentationRules": {
         "companyName": "Feral Frenzy",
         "xmlHeader": false,
         "documentInterfaces": false,
         "documentExposedElements": false,
         "documentInternalElements": false,
         "documentPrivateElements": false,
         "documentPrivateFields": false
       },
       "orderingRules": {
         "usingDirectivesPlacement": "outsideNamespace"
       }
     }
   }
   ```

8. Confirm `dotnet build` is clean across all three projects before proceeding.

---

## Task 2 — Folder Structure

Create the folder structure inside the Godot project. All folders are empty — add a `.gitkeep` file to each.

```
FeralFrenzy/
  src/
    godot/
      autoloads/
      characters/
      enemies/
      weapons/
      ui/
      camera/
      importer/
      constants/
  scenes/
    characters/
    enemies/
    weapons/
    world/
    ui/
    genre_levels/
  assets/
    sprites/
    audio/
    fonts/
  data/
    characters/
    weapons/
    chapters/
    enemies/
```

Inside `FeralFrenzy.Core/`:
```
src/
  core/
    data/
      engine/
      content/
      migration/
    generator/
    constants/
```

Inside `FeralFrenzy.Tests/`:
```
generator/
data/
fixtures/
```

---

## Task 3 — Core Data Types

Implement all types from `docs/01_schema.md` in `FeralFrenzy.Core/src/core/data/`.

### Engine types (`data/engine/`)

Implement exactly as specified in `01_schema.md`. No additions, no omissions:

- `SegmentType.cs` — enum
- `GeometryProfile.cs` — enum
- `DestructibleLevel.cs` — enum
- `HazardClass.cs` — enum
- `PlatformMotivation.cs` — enum
- `SightlineRating.cs` — enum
- `RewardNodeType.cs` — enum
- `GeometryTag.cs` — enum
- `RewardNode.cs` — record
- `SegmentData.cs` — record
- `RunData.cs` — record
- `ValidationError.cs` — record
- `ValidationResult.cs` — record

### Content types (`data/content/`)

- `FFCharacterSize.cs` — enum
- `FFWeaponTier.cs` — enum

Do NOT implement `FFCharacterDefinition` or `FFWeaponDefinition` as `Resource` subclasses yet — those require Godot and belong in `FeralFrenzy/src/godot/`. In this task, just define the enums they depend on.

### Constants (`constants/`)

Implement `SolvabilityConstants.cs` exactly as specified in `01_schema.md`.

### State types

Implement all payload types from `docs/02_state_machine.md`:

- `StatePayload.cs` — abstract record base class
- `CinematicPayload.cs`
- `SegmentPayload.cs`
- `BossFightPayload.cs`
- `ReviveWindowPayload.cs`
- `RunSummaryPayload.cs`
- `LevelEditorPayload.cs`
- `SpineStep.cs` — abstract record + all concrete step records (`PlaySegmentStep`, `PlayBossStep`, `PlayGenreLevelStep`, `PlayCinematicStep`, `EndRunStep`)

Also implement the `GameState` enum in `FeralFrenzy.Core` (it has no Godot dependencies and belongs in core):
- `GameState.cs` — enum, all values from `02_state_machine.md`

**Confirm:** `FeralFrenzy.Core` has zero `using Godot` statements. Run `grep -r "using Godot" FeralFrenzy.Core/` and confirm zero results.

---

## Task 4 — Serialization

Add JSON serialization support to `RunData` and `SegmentData` using `System.Text.Json`.

```csharp
// FeralFrenzy.Core.csproj — add:
// System.Text.Json is included in .NET 8, no extra package needed.
```

Requirements:
- `RunData` must serialize to and deserialize from JSON
- `SegmentData` must serialize to and deserialize from JSON
- All enum values serialize as strings (not integers) — use `JsonStringEnumConverter`
- Property names serialize as camelCase
- The JSON output must match the examples in `01_schema.md`

Write serialization tests in `FeralFrenzy.Tests/data/`:

```
RunDataSerializationTests.cs
  - Serialize_RunData_ProducesValidJson
  - Deserialize_ValidJson_ProducesRunData
  - RoundTrip_RunData_IsIdentical
  - Deserialize_ExampleFromSchema_Succeeds  ← uses the exact JSON from 01_schema.md
```

The test `Deserialize_ExampleFromSchema_Succeeds` must use the exact JSON example from `01_schema.md` as its input. This test existing means any schema change that breaks the documented example is caught immediately.

---

## Task 5 — Autoload Stubs

Create stub autoloads in `FeralFrenzy/src/godot/autoloads/`. These are stubs — enough to compile and be registered, not full implementations.

### `GameStateManager.cs`

```csharp
using Godot;
using FeralFrenzy.Core.Data.Engine;

namespace FeralFrenzy.Godot.Autoloads;

public partial class GameStateManager : Node
{
    public GameState Current { get; private set; } = GameState.Title;

    [Signal]
    public delegate void StateChangedEventHandler(GameState from, GameState to);

    public void TransitionTo(GameState next, StatePayload? payload = null)
    {
        // Phase 0 stub — no validation, no logic, just stores state
        // Full implementation: docs/02_state_machine.md
        var previous = Current;
        Current = next;
        EmitSignal(SignalName.StateChanged, (int)previous, (int)next);
    }
}
```

### `AssetRegistry.cs`

```csharp
using Godot;

namespace FeralFrenzy.Godot.Autoloads;

public partial class AssetRegistry : Node
{
    // Phase 0 stub — returns null for all keys
    // Full implementation: Phase 1, loads from data/assets_manifest.json
    public T? Load<T>(string key) where T : Resource
    {
        GD.PushWarning($"AssetRegistry: key '{key}' requested but registry not yet initialized.");
        return null;
    }

    public PackedScene? GetScene(string key) => Load<PackedScene>(key);
}
```

### `InputManager.cs`

```csharp
using Godot;

namespace FeralFrenzy.Godot.Autoloads;

public partial class InputManager : Node
{
    // Phase 0 stub — player 1 only, keyboard only
    // Full implementation: Phase 1, multi-device routing for 1–4 players

    public bool IsActionPressed(int playerIndex, string action)
    {
        if (playerIndex != 0) return false;
        return Input.IsActionPressed(action);
    }

    public bool IsActionJustPressed(int playerIndex, string action)
    {
        if (playerIndex != 0) return false;
        return Input.IsActionJustPressed(action);
    }

    public float GetAxis(int playerIndex, string negativeAction, string positiveAction)
    {
        if (playerIndex != 0) return 0f;
        return Input.GetAxis(negativeAction, positiveAction);
    }
}
```

Register all three autoloads in Godot's Project Settings → Autoloads:
- `GameStateManager` → `res://src/godot/autoloads/GameStateManager.cs`
- `AssetRegistry` → `res://src/godot/autoloads/AssetRegistry.cs`
- `InputManager` → `res://src/godot/autoloads/InputManager.cs`

---

## Task 6 — Constants Files

Create the constants files in `FeralFrenzy/src/godot/constants/`. These are mostly empty stubs in Phase 0 — they exist so nothing needs to be restructured later.

### `AnimationNames.cs`

```csharp
namespace FeralFrenzy.Godot.Constants;

public static class AnimationNames
{
    public const string Idle = "idle";
    public const string Walk = "walk";
    public const string WalkStart = "walk_start";
    public const string Jump = "jump";
    public const string Fall = "fall";
    public const string Slide = "slide";
    public const string Death = "death";
}
```

### `InputActions.cs`

```csharp
namespace FeralFrenzy.Godot.Constants;

public static class InputActions
{
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string Jump = "jump";
    public const string Slide = "slide";
    public const string PrimaryAttack = "primary_attack";
    public const string SecondaryAttack = "secondary_attack";
    public const string AimUp = "aim_up";
    public const string AimDown = "aim_down";
}
```

### `NodePaths.cs`

```csharp
namespace FeralFrenzy.Godot.Constants;

public static class NodePaths
{
    public const string AnimatedSprite = "AnimatedSprite2D";
    public const string CollisionShape = "CollisionShape2D";
}
```

### `AssetKeys.cs`

```csharp
namespace FeralFrenzy.Godot.Constants;

public static class AssetKeys
{
    // Characters
    public const string SpriteBear = "sprite_bear";
    public const string SpriteCroc = "sprite_croc";
    public const string SpriteHammerhead = "sprite_hammerhead";
    public const string SpriteHoneyBadger = "sprite_honeybadger";

    public const string SceneCharBear = "scene_char_bear";
    public const string SceneCharCroc = "scene_char_croc";
    public const string SceneCharHammerhead = "scene_char_hammerhead";
    public const string SceneCharHoneyBadger = "scene_char_honeybadger";
}
```

Create `data/assets_manifest.json` with stub entries:

```json
{
  "schemaVersion": "1.0",
  "assets": {
    "sprite_bear": "res://assets/sprites/characters/bear_placeholder.png",
    "sprite_croc": "res://assets/sprites/characters/croc_placeholder.png",
    "sprite_hammerhead": "res://assets/sprites/characters/hammerhead_placeholder.png",
    "sprite_honeybadger": "res://assets/sprites/characters/honeybadger_placeholder.png",
    "scene_char_bear": "res://scenes/characters/Bear.tscn",
    "scene_char_croc": "res://scenes/characters/Croc.tscn",
    "scene_char_hammerhead": "res://scenes/characters/Hammerhead.tscn",
    "scene_char_honeybadger": "res://scenes/characters/HoneyBadger.tscn"
  }
}
```

---

## Task 7 — Bear Character Definition Resource

Create `FFCharacterDefinition` as a `Resource` subclass in `FeralFrenzy/src/godot/` (not in Core — it has Godot dependencies):

```csharp
// FeralFrenzy/src/godot/characters/FFCharacterDefinition.cs
using Godot;
using FeralFrenzy.Core.Data.Content;

namespace FeralFrenzy.Godot.Characters;

[GlobalClass]
public partial class FFCharacterDefinition : Resource
{
    [Export] public string CharacterKey { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public FFCharacterSize Size { get; set; } = FFCharacterSize.Medium;
    [Export] public float MoveSpeed { get; set; } = 120f;
    [Export] public float JumpVelocity { get; set; } = -280f;
    [Export] public float JumpArcMultiplier { get; set; } = 1.0f;
    [Export] public bool AlwaysFitsGaps { get; set; } = false;
    [Export] public bool HasExtraHit { get; set; } = false;
    [Export] public float WeaponDamageMultiplier { get; set; } = 1.0f;
    [Export] public string SecondaryAbilityKey { get; set; } = string.Empty;
    [Export] public string SpriteFramesKey { get; set; } = string.Empty;
    [Export] public string PortraitKey { get; set; } = string.Empty;
}
```

Create `data/characters/Bear_character.tres` — a `FFCharacterDefinition` resource with Bear's values from `01_schema.md`:

```
CharacterKey = "char_bear"
DisplayName = "Bear"
Size = Large (2)
MoveSpeed = 90.0
JumpVelocity = -270.0
JumpArcMultiplier = 1.0
AlwaysFitsGaps = false
HasExtraHit = true
WeaponDamageMultiplier = 1.6
SecondaryAbilityKey = "ability_roar"
SpriteFramesKey = "sprite_bear"
PortraitKey = "portrait_bear"
```

---

## Task 8 — Bear Moves on Screen

Create the minimum scene tree to get Bear moving on a flat test level.

### `Bear.tscn` scene tree:
```
Bear (CharacterBody2D) ← PlayerController.cs attached
  ├── CollisionShape2D ← RectangleShape2D, sized for Bear (~14×20 tiles)
  └── AnimatedSprite2D ← no spritesheet yet, just the node
```

### `PlayerController.cs`:

```csharp
using Godot;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;

namespace FeralFrenzy.Godot.Characters;

public partial class PlayerController : CharacterBody2D
{
    [Export] public FFCharacterDefinition? Definition { get; set; }
    [Export] public int PlayerIndex { get; set; } = 0;

    private const float Gravity = 600f;
    private InputManager _input = null!;

    public override void _Ready()
    {
        if (Definition is null)
            throw new InvalidOperationException(
                $"{nameof(PlayerController)} on '{Name}': Definition export is not assigned.");

        _input = GetNode<InputManager>("/root/InputManager");
    }

    public override void _PhysicsProcess(double delta)
    {
        var velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;

        if (_input.IsActionJustPressed(PlayerIndex, InputActions.Jump) && IsOnFloor())
            velocity.Y = Definition.JumpVelocity;

        float dir = _input.GetAxis(PlayerIndex, InputActions.MoveLeft, InputActions.MoveRight);
        velocity.X = dir * Definition.MoveSpeed;

        Velocity = velocity;
        MoveAndSlide();
    }
}
```

### `TestLevel.tscn` scene tree:
```
TestLevel (Node2D)
  ├── StaticBody2D
  │   └── CollisionShape2D (WorldBoundaryShape2D — infinite floor)
  └── Bear (instance of Bear.tscn)
      └── [Definition export assigned to Bear_character.tres]
```

Set `TestLevel.tscn` as the main scene in Project Settings.

Add input actions to the Input Map:
- `move_left` → A key, Left Arrow
- `move_right` → D key, Right Arrow
- `jump` → Space, Up Arrow
- `slide` → Left Shift, Left Ctrl
- `primary_attack` → Z key, Gamepad A
- `secondary_attack` → X key, Gamepad B
- `aim_up` → W key, Up Arrow (held while firing)
- `aim_down` → S key, Down Arrow (held while firing)

---

## Task 9 — Smoke Tests

Write two smoke tests in `FeralFrenzy.Tests/` to confirm the foundation is solid.

### `SegmentDataSerializationTests.cs`

```csharp
// Test 1: the example from 01_schema.md round-trips correctly
// Test 2: all enum values serialize as strings not integers
// Test 3: camelCase property names in output JSON
```

### `SolvabilityConstantsTests.cs`

```csharp
// Test 1: ReferenceCharacterKey is "char_croc"
// Test 2: GuaranteedHeavyDestructibleChapter is "chapter_dead_station"
// Test 3: MaxDifficultyRampPerSegment is between 0.0 and 1.0
```

These are trivial tests. They exist to confirm the constants are set correctly and to establish the test runner as part of the workflow.

---

## Exit Checklist

Before marking Phase 0 complete, confirm every item:

- [ ] `dotnet build` clean across all three projects — zero warnings, zero errors
- [ ] `dotnet test` passes — all smoke tests green
- [ ] `dotnet format --verify-no-changes` clean — no formatting drift
- [ ] `grep -r "using Godot" FeralFrenzy.Core/` returns zero results
- [ ] Bear moves left and right with A/D keys
- [ ] Bear jumps with Space
- [ ] Bear lands on the infinite floor
- [ ] All three autoloads registered in Godot project settings
- [ ] `data/assets_manifest.json` exists
- [ ] `DEVLOG.md` updated with Phase 0 summary

---

## DEVLOG Entry Template for This Task

```markdown
## [DATE] — Phase 0: Foundation

**Phase:** 0  
**Built:**
- Solution restructured: FeralFrenzy, FeralFrenzy.Core, FeralFrenzy.Tests
- All engine and content enums and records from 01_schema.md
- All state payload types from 02_state_machine.md
- GameState enum
- AssetRegistry, GameStateManager, InputManager stubs
- AnimationNames, InputActions, NodePaths, AssetKeys constants
- FFCharacterDefinition resource class
- Bear_character.tres with correct values
- PlayerController.cs — Bear moves on screen
- TestLevel.tscn — Bear on infinite floor
- Smoke tests passing

**Decisions:**
- [any non-obvious choices]

**Deferred:**
- Animation (Phase 1)
- Full AssetRegistry implementation (Phase 1)
- Full GameStateManager transition validation (Phase 1)
- Multi-player input routing (Phase 1)
- All other characters (Phase 1)

**Next:** Phase 1 — begin with wall kick and slide movement, then add remaining three characters

**Tests added:** SegmentDataSerializationTests.cs, SolvabilityConstantsTests.cs
```

---

## What Is Explicitly Out of Scope for This Task

Do not build any of the following. If you find yourself starting to build them, stop and add them to the Deferred section of DEVLOG.md:

- Animation system or sprite art
- Any weapon code
- Any enemy code
- Any combat or damage system
- Full `GameStateManager` transition logic
- Full `AssetRegistry` manifest loading
- Multi-player input routing
- Any generator code
- Any importer code
- Level editor
- Any chapter content beyond the `TestLevel.tscn` flat floor
- Audio
- UI beyond what Godot shows by default
- Croc, Hammerhead, or Honey Badger character definitions or scenes
- `SubViewport` main scene wrapping — this is Phase 1 (noted in implementation plan)
