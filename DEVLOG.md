# Feral Frenzy — Dev Log

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
