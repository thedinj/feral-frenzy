# Feral Frenzy — Dev Log

## 2026-04-22 — Phase 1 Complete

**Phase:** 1
**Built:**
- Fixed missed jump inputs — discrete inputs (`Jump`, `Slide`, `PrimaryAttack`) moved from `_PhysicsProcess` polling to `_Input()` callback with a 120ms jump buffer; eliminates dropped inputs between physics ticks
- Refactored `PlayerController` input handling — `TickTimers()` merges slide and jump buffer timers; `_slideRequested`/`_fireRequested` flags consumed once per physics frame; `_Input()` sets them synchronously
- 8-way gamepad firing — right stick aim+autofire (hold to spray); X button fires toward left-stick direction if pushed, else fires in facing direction; R3 (right stick click) jumps
- `InputManager.IsActionJustPressedFromEvent(InputEvent, int, string)` — routes keyboard vs gamepad from the OS event, including R3→Jump mapping
- `InputManager.GetLeftStickVector` / `GetRightStickVector` — reads Joy axes with dead zone
- `VectorToAimDirection` static helper — atan2 + sector rounding to nearest of 8 × 45° directions
- Viewport resolution changed to 1280×720 native; SubViewport stays 320×180; `SubViewportContainer` with `texture_filter=1` keeps pixel art nearest-neighbor while UI renders sharp at native resolution
- `MainController` base class changed from `Node` to `Control` to match scene root
- Kill count and run time fixed — `[Export] Label?` fields in `HudController` and `RunSummaryController` were silently null; replaced with `GetNodeOrNull<Label>("NodeName")` in `_Ready()` and removed NodePath wiring from .tscn files; pattern now applied to all hand-written .tscn controllers
- Camera zoom tuned from 0.7 → 0.65 (re-calibrated after viewport resolution change); `_zoomLevel` is `[Export]` for live editor tuning
- Removed all debug `GD.Print` calls from `CoopCamera`; fixed StyleCop SA1214 (readonly field order)
- Players spawn with Default Blaster equipped — `LevelController.EquipDefaultWeapon` called after each player is added to the scene tree; `AssetKeys.WeaponDefDefaultBlaster` and `assets_manifest.json` entry added
- `GroundPatroller` and `AerialDiver` chase the nearest live player — enemies now move toward the player rather than always right; `GroundPatroller` uses `ChaseTarget` (track player X direction) when outside fire range, falls back to wall-bounce patrol with no target; `AerialDiver` steers patrol X toward nearest player each frame
- Density stress test passed — 30 entities (20 GroundPatrollers + 10 AerialDivers) holds 75fps (monitor-limited) on dev machine

**Decisions:**
- `GetNodeOrNull<T>("NodeName")` in `_Ready()` is the definitive pattern for all node references in hand-written .tscn files — `[Export]` node wiring is silently discarded by Godot when the property name has a leading underscore after C# reflection. Every controller now follows this pattern.
- Gamepad right stick autofire is intentional — holding the stick sprays continuously in that direction, which matches the run-and-gun feel. X button fires once per press (or continuously via `_fireRequested` flag) using left stick or facing direction.
- Spinning Blade deferred to Phase 2 — was initially in the Phase 1 scope but is Tier 2 content; the weapon system and `WeaponPickup` are implemented, the blade definition is Phase 2.
- Bear/Honey Badger size differential and `AlwaysFitsGaps` behaviour deferred to Phase 2 — placeholder art makes the size gap unverifiable; these checklist items depend on real sprites.

**Deferred:**
- Bear/Honey Badger size differential visual verification (Phase 2 — needs real sprites)
- Honey Badger `AlwaysFitsGaps` verification (Phase 2)
- Spinning Blade pickup and feel tuning (Phase 2)
- Remaining debug GD.Print calls in GameStateManager and LevelController (cleanup pass, Phase 2)

**Next:** Phase 2 — Croc and Hammerhead, character secondaries, enemy HP pool, audio, 4-player input routing, negative power-ups, Spinning Blade content

**Tests added:** None new — 10 existing tests still pass.

---

## 2026-04-21 — Phase 1: Runtime bug fixes — GetNode wiring + camera snap

**Phase:** 1
**Built:**
- Fixed `LevelController: _entities not assigned` crash — Godot 4 C# does not reliably wire node-type [Export] fields from hand-written .tscn files; replaced [Export] on `_entities`, `_playerSpawns`, `_camera` with `GetNode`/`GetNodeOrNull` in `_Ready()` using NodePaths constants
- Same fix applied to PlayerController (`_sprite`, `_collisionShape`, `_weaponMount`) and DensityTestController (`_spawnRoot`)
- Removed now-redundant NodePath wiring lines from Bear.tscn, HoneyBadger.tscn, Level.tscn, DensityTest.tscn
- Fixed camera starting at (0,0) — players at y=150 were outside the visible area with zoom=0.7; CoopCamera now sets `_snapOnNextFrame=true` on first player registration and snaps directly to player position on the next _Process frame instead of lerping from (0,0)

**Decisions:**
- Node-type exports replaced with GetNode/GetNodeOrNull across all controllers — Godot strips the leading underscore when registering C# property names in .tscn, causing the wiring to silently fail. GetNode in _Ready with NodePaths constants is the reliable pattern; CLAUDE.md explicitly allows this fallback
- `_spawnRoot` in DensityTestController stays nullable (`GetNodeOrNull`) — the fallback to `this` is intentional and correct if the Entities node is absent

**Deferred:** (unchanged from prior Phase 1 entry)

**Next:** Open Godot editor, run the game, play through title → loadout → level → exit trigger → run summary. Then run DensityTest.tscn and confirm 30-entity 60fps target.

**Tests added:** None — existing 10 tests still pass.

## 2026-04-21 — Phase 1: Vertical Slice — C# systems + scenes complete

**Phase:** 1
**Built:**
- Full GameStateManager with legal transition table (all 14 states), player tracking, death/revive timers, kill/death/time run stats
- Full InputManager: keyboard (player 0) + gamepad (player 1) with manual HashSet just-pressed tracking for gamepad
- EntityPool autoload — lazy pool with PreWarm/Get/Return; registered in project.godot
- RunSpine autoload — Phase 1 stub always returns RunSummary
- CoopCamera with panoramic zoom 0.7, multi-player lerp follow
- PlayerController full movement: gravity, wall kick, double jump (MaxJumps=2), slide (shrinks collision), eight-directional aiming, fire/revive
- EnemyController base class: gravity, TakeDamage, health, Die (notifies GameStateManager kill count)
- GroundPatroller: patrol + fire AI, finds nearest player via "players" group
- AerialDiver: patrol/dive/return state machine, flies (no gravity), three-state enum
- FFWeaponDefinition and FFEnemyDefinition GlobalClass Resource types
- WeaponController: cooldown, fire, rapid-fire power-up, eight-directional projectile spawn
- ProjectileController: travels, max distance QueueFree, damages both EnemyController and PlayerController on body-entered
- WeaponPickup and PowerUp Area2D pickups
- ExitTrigger → TransitionTo(RunSummary)
- LevelController: spawn/respawn players, revive proximity check (hold primary 2s), parallax builder, entity pool pre-warm; static Instance for weapon/enemy projectile spawning
- GodotParallaxBuilder: uses Godot 4.3+ Parallax2D (replaced deprecated ParallaxBackground/ParallaxLayer)
- FFParallaxLayerDefinition and FFChapterDefinition records in Core
- TitleController, LoadoutSelectController, HudController, RunSummaryController, MainController UI
- DensityTestController: spawns 30 enemies for 60fps stress test
- All scene files: Main.tscn (SubViewport + CanvasLayer structure), Level.tscn (handcrafted geometry + spawns), HoneyBadger.tscn, GroundPatroller.tscn, AerialDiver.tscn, Projectile.tscn, WeaponPickup.tscn, PowerUp.tscn, DensityTest.tscn, Title.tscn, LoadoutSelect.tscn, HUD.tscn, RunSummary.tscn
- Data files: HoneyBadger_character.tres, DefaultBlaster_weapon.tres, GroundPatroller_enemy.tres, AerialDiver_enemy.tres, chapter_cretaceous.json (with parallax layer config)
- assets_manifest.json updated with all Phase 1 keys (sprites → icon.svg placeholder, all scenes → correct paths)
- project.godot: main scene changed to scenes/ui/Main.tscn, EntityPool and RunSpine added as autoloads

**Decisions:**
- SubViewport wrapping established now (SubViewportContainer + SubViewport at 320x180) so future shader effects don't require scene restructure
- `new()` target-typed expressions replaced with explicit-type `new T()` throughout — StyleCop 1.1.x SA1000 does not recognize target-typed new
- `var` used for all `.GetChildren()`, `.GetNodesInGroup()`, `.AsGodotDictionary()` return types to avoid namespace collision between `FeralFrenzy.Godot.*` and `Godot.Collections.*`
- `protected float Health` and `protected bool IsDead` in EnemyController changed to `protected { get; private set; }` properties to satisfy SA1401 (fields must be private) and SA1306 (field names lowercase)
- Enemy projectiles use same ProjectileController as player projectiles; collision layers distinguish targets (players on layer 2, enemies on layer 3, projectiles on layer 4 masking both)
- Sprite assets use icon.svg as placeholder for all characters/parallax until Gemini sprites arrive
- RunSpine is a stub — full procedural generation is Phase 3
- LevelController.cs: `IsActionJustPressed` changed to `IsActionPressed` in CheckReviveProximity — hold-to-revive requires continuous press, not edge detection

**Deferred:**
- Actual sprite art and animation (Gemini/Grok pipeline — post Phase 1)
- Collision layer tuning for friendly fire prevention (no self-damage; Phase 2)
- Enemy spawner on LevelController (currently only pre-warms pool; spawning from enemies node not wired to level triggers — Phase 2)
- Death animation (EnemyController.Die uses QueueFree directly — Phase 2)
- Full 4-player input routing (Phase 2)
- Chapter intro cinematic (Phase 2)

**Next:** Open Godot editor, verify the scene runs, play through title → loadout → level → exit trigger → summary flow. Run density test scene at 60fps. If wiring issues surface from scene editor, fix exports in .tscn files.

**Tests added:** None new — existing 10 tests still pass. Godot systems are integration-tested by running the game.

## 2026-04-21 — Phase 0: Foundation

**Phase:** 0
**Built:**
- Solution restructured: FeralFrenzy (Godot), FeralFrenzy.Core (.NET 8 class library), FeralFrenzy.Tests (xUnit)
- All engine enums and records from docs/01_schema.md (SegmentData, RunData, ValidationError, ValidationResult, all enum types)
- All state payload types from docs/02_state_machine.md (StatePayload, CinematicPayload, SegmentPayload, BossFightPayload, ReviveWindowPayload, RunSummaryPayload, LevelEditorPayload)
- SpineStep abstract record + all concrete step types (PlaySegmentStep, PlayBossStep, PlayGenreLevelStep, PlayCinematicStep, EndRunStep)
- GameState enum in Core
- Content enums: FFCharacterSize, FFWeaponTier
- SolvabilityConstants
- AssetRegistry, GameStateManager, InputManager stub autoloads
- AnimationNames, InputActions, NodePaths, AssetKeys constants
- FFCharacterDefinition Resource subclass in Godot project
- Bear_character.tres with correct values from schema doc
- PlayerController.cs — Bear moves left/right and jumps
- Bear.tscn and TestLevel.tscn scenes
- data/assets_manifest.json stub
- Autoloads registered in project.godot; TestLevel.tscn set as main scene
- 10 smoke tests passing (RunData serialization, SegmentData serialization, SolvabilityConstants)

**Decisions:**
- StyleCop.Analyzers 1.2.x is beta-only on nuget.org; used 1.1.118 (latest stable) — same analyzer suite
- Suppressed SA1309 (underscore fields), SA0001 (XML analysis not required), SA1633 (file headers), SA1313 (record primary constructor params), SA1009 (record `) :` syntax), SA1101 (this prefix), SA1516 (blank lines between all members) — all are StyleCop 1.1.x limitations with modern C# record syntax or conflicts with Godot export property conventions
- GameStateManager signal uses `long` parameters instead of `GameState` enum for Godot Variant compatibility
- Added explicit `<Compile Remove>` items to Godot csproj — Godot.NET.Sdk globs `**/*.cs` from project root, picking up Core and Tests source files and their obj directories without this exclusion
- Added local nuget.config at solution root to add nuget.org source alongside the Godot-only GodotLocal feed
- Added `using System;` explicitly to PlayerController.cs — Godot.NET.Sdk does not enable standard .NET implicit usings
- Bear character values from schema doc: MoveSpeed=90, JumpVelocity=-270, WeaponDamageMultiplier=1.6, HasExtraHit=true

**Deferred:**
- Animation system and sprite art (Phase 1)
- Full AssetRegistry manifest loading (Phase 1)
- Full GameStateManager transition validation with legal transition table (Phase 1)
- Multi-player input routing for 1–4 players (Phase 1)
- All other characters: Croc, Hammerhead, Honey Badger (Phase 1)
- SubViewport main scene wrapping (Phase 1 — per implementation plan)
- Bear slide mechanics (Phase 1)
- Wall kick and double jump (Phase 1)

**Next:** Phase 1 — begin with complete movement system (wall kick, slide, double jump), then all four characters, then the handcrafted level

**Tests added:** FeralFrenzy.Tests/data/RunDataSerializationTests.cs (4 tests), FeralFrenzy.Tests/data/SegmentDataSerializationTests.cs (3 tests), FeralFrenzy.Tests/SolvabilityConstantsTests.cs (3 tests)
