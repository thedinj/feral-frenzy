# Feral Frenzy — Dev Log

## 2026-04-26 — Phase 2.5 complete: animation architecture + art integration scaffolding

**Phase:** 2.5
**Built:**
- **Core animation types** (`FeralFrenzy.Core/src/core/animation/`): `AnimationInput` record, `AnimationRule<T>` record, `AnimationRuleSet<T>` first-match evaluator, `AnimationStateMachine<T>` with one-shot blocking (`_finishedOneShots`) and `Reset()` for respawn. Zero Godot dependencies.
- **Content-layer animation enums** (`FeralFrenzy.Core/src/core/data/content/`): `FFPlayerAnimationState`, `FFSimpleEnemyState`, `FFBossAnimationState`, `FFMountedDinoAnimationState`. All FF-prefixed.
- **Sprite contract type** (`FFSpriteContract` / `FFSpriteAnimation` / `FFSpriteFrame`): JSON-deserializable records for the tile-indexed spritesheet contract format.
- **Godot base classes** (`src/godot/core/`): `IEntitySubsystem` interface, `GameEntity` (seals `_PhysicsProcess`, routes to `OnPhysicsProcess`), `GameArea`, `GameStatic`. All with `RegisterSubsystem` and `ConfigureAnimation<T>` entry points.
- **Animation driver** (`src/godot/animation/`): `AnimationDriverBase` abstract node, `AnimationDriver<T>` generic driver (subscribes `AnimationFinished`, guards `HasAnimation` before `Play()`), `AnimationBuilder<T>` fluent builder (`WithSprite`, `WithAnimationPlayer`, `WithRules`, `WithCustomEvaluator`, `WithOneShots`, `WithClips`, `WithInput`, `Build()`).
- **SpriteFramesBuilder**: builds `SpriteFrames` from `FFSpriteContract` using x/y tile indices → `Rect2`; `LoadContract()` reads adjacent JSON via `FileAccess`.
- **Migrated EnemyHost**: `CharacterBody2D` → `GameEntity`; renamed `_PhysicsProcess` → `OnPhysicsProcess(float)`; added `IsAttacking` property; added `ConfigureAnimation<FFSimpleEnemyState>()` block conditional on `AnimatedSprite2D` presence.
- **Migrated PlayerController**: `CharacterBody2D` → `GameEntity`; replaced `AnimatedSprite2D`/`UpdateAnimation()`/`TryPlayAnimation()` with `Sprite2D _bodySprite` and `ConfigureAnimation<FFPlayerAnimationState>()`; added `_facingLeft`, `_jumpExecutedThisFrame`, `_tookHitThisFrame` flags; `IsDead || IsDown` mapped to `IsDead` animation input so Death plays for both states; WalkStart → Walk transition via two-rule pattern; UpdateArmAim() for arm/weapon sprite rotation.
- **Updated character scenes** (`Bear.tscn`, `HoneyBadger.tscn`): removed `AnimatedSprite2D` + placeholder SpriteFrames; added `AnimationPlayer`, `BodySprite (Sprite2D)`, `ArmSprite (Sprite2D)`, `WeaponSprite (Sprite2D)`.
- **HitboxDebugController** (`src/godot/debug/`): cycles loaded character scenes with Tab, shows animation state and position in a debug label, enables collision hints.
- **HitboxDebugLevel.tscn** (`scenes/debug/`): floor `StaticBody2D` + `WorldBoundaryShape2D`, `DebugLabel`, `HitboxDebugController` root.
- **AnimationStateMachineTests** (19 tests): basic transitions, global transitions, one-shot blocking, rule-set integration, JustTransitioned flag, Reset.
- **AnimationRuleSetTests** (4 tests): first match wins, no match returns default, second rule match, current state passed to condition.
- **CLAUDE.md updated**: Animation Architecture Rule section added; Never Do entries for Play() in game logic, animation in OnPhysicsProcess, driver registration order, AnimationInput raw input fields, per-entity transition classes, `_PhysicsProcess` override in GameEntity subclasses. Quick Reference rows added.

**Decisions:**
- `EnemyHost.IsAttacking` is a plain `bool` property set by behavior nodes, read by the animation input closure. No virtual method needed — behaviors write it directly since EnemyHost is the only enemy base class.
- WalkStart → Walk transition uses two rules: `(c == Walk && isMoving) → Walk` and `(c == WalkStart && isMoving) → Walk`. The one-shot block on WalkStart ensures it plays fully before the second rule fires.
- `IsDead` animation input maps to `IsDead || IsDown` in PlayerController so the Death animation plays whether the player is dead or merely downed. Downed players run no physics anyway, so the distinction is visual only.
- `_tookHitThisFrame` is reset at the START of `OnPhysicsProcess`, not the end. Damage fires during collision callbacks (same physics frame), so the flag is set during frame N and read by the animation driver after `OnPhysicsProcess` returns.
- AnimationPlayer conditional: `ConfigureAnimation` is only called when `GetNodeOrNull<AnimationPlayer>()` is non-null. Prevents crashes on scenes not yet updated to the new structure. Scenes now have AnimationPlayer so this fires correctly.
- SA1623 (StyleCop bool property XML doc format) fired on `IsAttacking`. Fixed by converting the `<summary>` block to an inline comment — SA0001 is suppressed so XML docs are not required.
- Namespace collision between `FeralFrenzy.Godot` and `Godot.Collections` when using `GetNodesInGroup()` — resolved with `var` for the return type in EnemyHost.

**Deferred:**
- Hand-keying AnimationPlayer clips in Godot editor — developer does this when art arrives.
- SpriteFramesBuilder wiring in enemy controllers — developer drops spritesheet + contract, then Claude Code uncomments the initialization lines.
- Migrating `ProjectileController` and `SpinningBladeProjectile` from `Area2D` to `GameArea` — not in Phase 2.5 scope.
- Boss `FFBossAnimationState` + `WithCustomEvaluator` wiring — boss controller exists but animation is placeholder; plug-in points documented in Phase 2.5 brief.
- MountedDino `FFMountedDinoAnimationState` wiring — same deferral.

**Next:** Phase 3 — Chapter generator, WFC level assembly, full playthroughable loop with generated segments.

**Tests added:**
- `FeralFrenzy.Tests/animation/AnimationStateMachineTests.cs` (19 tests)
- `FeralFrenzy.Tests/animation/AnimationRuleSetTests.cs` (4 tests)

## 2026-04-25 — Phase 2 confirmed complete: input swap, gamepad feel, enemy contact damage, boss i-frames

**Phase:** 2 (final sign-off)
**Built:**
- **Controller/keyboard swap:** `GamepadPlayerIndex = 0`, `KeyboardPlayerIndex = 1`. `InputManager.ToDevice` helper skips the keyboard slot when mapping player index → joypad device. `LoadoutSelectController.DeviceToPlayerIndex` is the inverse. Gamepad P1 auto-joins; keyboard P2 is now optional — game starts as soon as gamepad P1 confirms, regardless of keyboard.
- **Right stick direction priority:** Fixed `HandleFiring` — left stick no longer overwrites `_aimDirection` when the right stick is already above threshold. Right stick always wins.
- **Precise right stick aiming:** Replaced 8-direction snap with raw stick vector. `WeaponController.FireRaw(Vector2, float)` bypasses `AimDirectionToVector`. `PlayerController._rawAimVector` (`Vector2?`) carries the precise direction; `null` means fall back to enum. Left stick X-button shots also use the raw path.
- **Horizontal snap zone:** `SnapToHorizontalIfClose` snaps to exact left/right when stick elevation is within ±15° of horizontal (`InputConstants.GamepadHorizontalSnapDeg`), preventing accidental floor shots. Applied to both right stick autofire and left stick X-button shots.
- **Dive enemy contact damage:** `DiveBehavior.DamageOverlappingPlayers` checks proximity (14px radius) during the Diving state and calls `player.TakeDamage(1)` with a 0.5s cooldown. Player i-frames provide the second protection layer.
- **Weapon feel tuning:** `DefaultBlaster_weapon.tres` `FireRate` 0.15 → 0.22s (~6.7 → ~4.5 shots/sec). `RapidFireMultiplier` 0.3 → 0.5 (rapid fire rate 0.045s → 0.11s; was ~22/sec, now ~9/sec).
- **Boss i-frames:** `FFEnemyDefinition.InvincibilitySeconds` field added (default 0 — no change to regular enemies). `EnemyHost` tracks `_invincibilityTimer`, guards `TakeDamage` entry. `Boss_enemy.tres` set to 0.4s — a full burst at base fire rate lands ~1.8 hits instead of all of them.

**Decisions:**
- `Vector2?` (nullable) used for raw aim rather than `Vector2.Zero` sentinel — zero is a valid direction after normalization; null unambiguously means "no input."
- Horizontal snap applied only to the horizontal axis (not all 8 directions) — the goal was floor-shot prevention, not restoring the old sector system.
- Boss `InvincibilitySeconds` kept separate from `HitStunSeconds` — hit stun locks AI, i-frames block damage; they serve different purposes and need independent tuning.
- Keyboard player made opt-in at the loadout screen rather than always-present — supports solo controller play without requiring a second human to join and immediately confirm.

**Deferred:** Nothing new.
**Next:** Phase 3 — real sprites, audio, chapter structure, generator integration.
**Tests added:** None — all changes are Godot-layer, integration-tested by running the game.

---

## 2026-04-25 — MountedDino/PteroBomber placement, Berserker HUD indicator

**Phase:** 2
**Built:**
- Removed stale null `[Export]` overrides from all enemy instances in `Level.tscn` (leftover from pre-component EnemyController era — Godot ignores unknown props but they were noise)
- MountedDino and PteroBomber were already present in Level.tscn from the previous session; confirmed placements are correct (MountedDino1 at 680,155 near boss trigger; PteroBomber1 at 400,40 aerial)
- `StatusEffectController.IsBerserkerActive()` — mirrors the `AreControlsReversed()` pattern
- `PlayerController.IsBerserkerActive` — public bool property delegating to StatusEffectController
- `BerserkerLabel` added to `HUD.tscn` — purple, top-center, hidden by default
- `HudController._Process` polls `LevelController.Instance?.GetPlayers()` each frame; shows label when any player has Berserker active
**Decisions:**
- Single shared label (not per-player) — sufficient for 1–2 player test; per-player HP bars with individual status icons are a later visual polish task
- Poll in `_Process` rather than subscribing to a StatusEffect event — simpler and consistent with how revive/kill labels already work; effect duration is short enough that frame polling is negligible
**Next:** Play-test all five enemy types and the Berserker effect in-game
**Tests added:** None (UI polling, no testable logic)

## 2026-04-25 — Enemy component architecture: EnemyHost + behavior/component nodes

**Phase:** 2 (post-completion cleanup)
**Built:**
- **`EnemyHost`** (`src/godot/enemies/EnemyHost.cs`): Replaces `EnemyController`. Minimal host — owns Definition, CurrentHp, IsDead, TakeDamage, FindNearestPlayer, signal emission. Zero virtual methods. Delegates physics, AI, gravity, damage interception, and death sequencing to child nodes.
- **Four behavior interfaces:** `ITickBehavior`, `IGravityBehavior`, `IDeathBehavior`, `IDamageBehavior` in `src/godot/enemies/behaviors/`. `EnemyHost._PhysicsProcess` is sealed and drives all four.
- **Components:** `HitStunComponent` (timer + active flag, no signals), `EnemyVisuals` (subscribes to `HpChanged`, drives hit flash). Both in `src/godot/enemies/components/`.
- **Gravity behaviors:** `DownwardGravity` (reads `LevelController.EffectiveGravity`), `NullGravity` (no-op for aerial enemies).
- **Death behaviors:** `DefaultDeath` (plays death animation + `AnimationFinished → QueueFree`, or fades out with tween), `BossDeath` (1s pause then `TransitionTo<VillainExitState>`).
- **AI behaviors:** `PatrolBehavior`, `FireAtPlayerBehavior` (child `Patrol` fallback), `DiveBehavior` (Patrol/Dive/Return state machine, exports replace hardcoded consts), `BombDropBehavior` (boundary/cooldown exports), `MountedDinoBehavior` (implements both `ITickBehavior` and `IDamageBehavior` — rider phase absorbs damage to its own HP pool; `host.CurrentHp` reassigned to dino HP on rider death).
- **Boss behaviors:** `PatternCycleBehavior` (wraps `PatternCycle<BossPattern>`, resolves `ChargeBehavior`/`BurstFireBehavior`/`SummonBehavior` children in `_Ready`), `ChargeBehavior` (completion callback), `BurstFireBehavior`, `SummonBehavior` (calls `host.RequestMinions` → signal up).
- **`PatternCycle<T>`** moved from `src/godot/enemies/` to `src/godot/enemies/behaviors/`, namespace updated.
- **LevelController updated:** `ActiveBoss` typed as `EnemyHost?`. `AddToEntities` subscribes to `ProjectileSpawnRequested` and `MinionSummonRequested` signals. New `OnEntityProjectileSpawnRequested(Vector2, float, float)` builds and spawns projectiles. New `OnMinionSummonRequested` uses `EntityPool.Get<EnemyHost>` and `AddToEntities`.
- **All callers updated:** `ProjectileController`, `SpinningBladeProjectile`, `HudController`, `DensityTestController` — all `EnemyController` type checks/casts replaced with `EnemyHost`.
- **All five enemy `.tscn` files rewritten** with new component hierarchy (HitStun/Gravity/Behavior/Death/Visuals child nodes).
- **Deleted:** `EnemyController.cs`, `GroundPatroller.cs`, `AerialDiver.cs`, `MountedDino.cs`, `PteroBomber.cs`, `PlaceholderBoss.cs`.
- **`NodePaths.cs`:** Added 5 EnemyHost slot constants (`EnemyBehavior`, `EnemyGravity`, `EnemyHitStun`, `EnemyDeath`, `EnemyVisuals`).

**Decisions:**
- `PlayerController` left unchanged. One variant for all characters (data-driven), behaviors tightly interdependent, `StatusEffectController` already handles extensibility.
- `EnemyVisuals` handles only hit flash (HpChanged signal). Death animation + QueueFree is entirely `DefaultDeath`'s responsibility — no dual subscription conflict.
- `MountedDinoBehavior` gets `HitStunComponent` reference via `GetNodeOrNull<HitStunComponent>("../HitStun")` for rider-phase stun activation, since it cannot call `host.TakeDamage` (that would recurse through the `IDamageBehavior` hook).
- `PatternCycleBehavior._isAttacking` prevents re-entry into charge while in progress; charge completes via `Action onComplete` callback in `ChargeBehavior.BeginCharge`.
- `BombDropBehavior` initializes its drop timer on first `Tick` rather than `_Ready` — components don't have reliable `_Ready` sequencing relative to host initialization.

**Deferred:** Nothing new.
**Next:** Assign `CretaceousLevel_config.tres` in Godot editor. Begin Phase 3 generator work.
**Tests added:** None — Godot-layer changes, integration-tested by running the game.

---

## 2026-04-25 — Architecture improvements: tuning to Resources, signal inversion, ReviveSystem, AssetRegistry hardening

**Phase:** 2 (post-completion cleanup)
**Built:**
- **`LevelConfig` Resource** (`src/godot/world/LevelConfig.cs`): New `[GlobalClass]` Godot Resource holding all level-specific tuning — `GravityScale`, revive window/hold/proximity durations, boss spawn position, 2-player extra spawn position, entity pool warm counts. Assigned via `[Export]` on `LevelController`. Defaults match the previous hardcoded values so no gameplay change.
- **Gravity override at level level:** `LevelController.EffectiveGravity` property returns `PhysicsConstants.Gravity * Config.GravityScale`. `PlayerController.ApplyGravity()` and `EnemyController._PhysicsProcess()` now read `LevelController.Instance?.EffectiveGravity ?? PhysicsConstants.Gravity`. `LevelController` also applies the scale to the physics world's `AreaParameter.Gravity` for Area2D/RigidBody2D nodes, restoring it on deactivation.
- **`ReviveSystem` node** (`src/godot/world/ReviveSystem.cs`): Extracted revive timer, proximity check, and all revive state out of `LevelController`. Owns `IsActive`, `SecondsRemaining`. Emits `ReviveCompleted(PlayerController)` and `WindowExpired` signals. `LevelController` subscribes to both and delegates. `HudController` reads `LevelController.IsReviveActive`/`ReviveSecondsRemaining` which delegate to `ReviveSystem`.
- **Signal inversion — "call down, signal up":** `PlayerController` now emits `[Signal] WentDown` instead of calling `LevelController.Instance?.HandlePlayerDown()`. `WeaponController` and `EnemyController` now emit `[Signal] ProjectileSpawned(Node2D)` instead of calling `LevelController.Instance?.AddToEntities()`. `LevelController` subscribes to all three at spawn time. `LevelController.HandlePlayerDown` is now private `OnPlayerWentDown`.
- **Tuning consts moved to Resources:** `PlayerController` consts `WallKickVelocityX`, `SlideSpeedMultiplier`, `SlideDuration`, `JumpBufferDuration` moved to `FFCharacterDefinition` exports. `WeaponController` consts `RapidFireMultiplier`, `RapidFireDuration` moved to `FFWeaponDefinition` exports. Pool warm counts moved to `LevelConfig`.
- **Definition backing fields:** `PlayerController`, `WeaponController`, and `EnemyController` each now assign a non-nullable `_definition` field in `_Ready()` using the `?? throw` pattern. All `Definition!.X` usages replaced with `_definition.X`.
- **AssetRegistry startup validation:** `ValidateScenes()` runs after manifest load and throws `InvalidOperationException` on any `scene_*` key that fails to load — fail-fast at boot rather than silent spawn failures at runtime.
**Decisions:**
- `LevelController.Instance` singleton kept for HudController read-only display access and `BossRoomTrigger.AddToEntities()`. The refactored pattern removes all *mutating* calls through the singleton; the remaining uses are read-only property reads and entity addition (not state mutation).
- `MaxJumps = 2` left as `PlayerController` constant per CLAUDE.md — applies equally to all characters by design.
- `PhysicsServer2D.AreaSetParam` for physics-world gravity is applied in addition to the `EffectiveGravity` property, so both manual-gravity characters and engine-gravity bodies (future additions) are both covered.
**Deferred:** Creating `data/levels/CretaceousLevel_config.tres` and assigning it in `Level.tscn` via the Godot editor — requires running Godot. The level functions correctly without it (all `Config?.X ?? default` null-coalescing preserves previous behavior).
**Next:** Create `CretaceousLevel_config.tres` in the Godot editor and assign to `Level.tscn`. Then continue with Phase 3 generator work.
**Tests added:** None — all changes are in Godot-layer code, which is integration-tested by running the game.

## 2026-04-23 — PlayerRoster refactor, composable state machine, title music

**Phase:** 2 (post-completion cleanup)
**Built:**
- **Composable state machine:** Replaced two ad-hoc timers (`_reviveTimer`, `_restartTimer`) in `GameStateManager` with a single `_autoTimer` driven by `IAutoTransition.GetDelay`/`SelectNext`. Each state with timer behavior implements `IAutoTransition` as a separate sealed class. `GameStateNode` base, `SimpleState` for no-behavior states, `ReviveWindowState`, `SegmentRestartState`, `LoadoutSelectState`, `SegmentState` as named classes.
- **GameStateContext:** Mutable bag passed to state nodes on entry/exit. Tracks `KillCount`, `DeathCount`, `RunTimeSeconds`, `StateBeforeRevive`, `WipedFromBossFight`, `ActiveSegmentPayload`. Player collections removed — those belong to `LevelController`.
- **PlayerRoster:** Plain C# class (no Godot deps) in `src/godot/world/`. Tracks total/down counts. `MarkDown()` returns `OneDown` or `AllDown`. `MarkRevived()`, `EliminateDownedPlayers()`, `Reset(n)`. Owned by `LevelController`.
- **LevelController revive timer:** `_reviveWindowTimer` (10s) starts on `ReviveWindow` entry, stops on `SegmentRestart` or successful revive. `OnReviveWindowTimerExpired` calls `_roster.EliminateDownedPlayers()`; routes to `SegmentRestart` only if `AliveCount == 0`, otherwise calls `_gameState.ExitReviveWindow()`.
- **PlayerController decoupled from GSM:** `GoDown()` now calls `LevelController.Instance?.HandlePlayerDown(this)` instead of `_gameState.NotifyPlayerDown(PlayerIndex)`. `_gameState` field and `GetNode<GameStateManager>` removed from `PlayerController`.
- **Title music:** `title.mp3` plays on title screen, loops at `loopPoint` stored in `assets_manifest.json` metadata. `AssetRegistry.GetLoopPoint(key)` reads from parsed manifest. `AudioStreamPlayer.Play(fromPosition)` used for loop-point restart without modifying the resource.
- **Debug print cleanup:** Removed all `[DBG ...]` prints from `PlaceholderBoss.cs` and `HudController.cs`.

**Decisions:**
- Revive timer stays in `LevelController`, not in `ReviveWindowState` via `IAutoTransition` — because the "should we restart or continue?" decision requires knowledge of living players, which belongs to `LevelController`/`PlayerRoster`, not the state machine context.
- `ExitReviveWindow()` on `GameStateManager` reads `_ctx.StateBeforeRevive` and routes back — LevelController doesn't need to know which state to return to, just that the revive window is over.
- `WipedFromBossFight` set in `SegmentRestartState.OnEnter` (when `from == BossFight`) so the state machine itself handles the boss-restart routing in `SelectNext` — LevelController reacts to the resulting `BossFight` state via `OnStateChanged`.

**Deferred:** Nothing new.

**Next:** Phase 3 — Real sprites, audio, chapter structure, generator integration.

**Tests added:** None — all 10 existing tests still pass.

---

## 2026-04-22 — Phase 2 Complete

**Phase:** 2
**Built:**
- **Task 1 — Enemy roster expansion:** MountedDino and PteroBomber scenes, .tres definitions, and controllers. MountedDino charges player (direction-aware), fires a burst when in range. PteroBomber flies patrol, drops a bomb projectile when above the player. LevelController spawns 1 extra GroundPatroller at (460, 155) when 2+ players active. EntityPool pre-warms 2 of each new enemy and 4 spinning blade projectiles.
- **Task 2 — Spinning Blade pickup:** SpinningBladeProjectile (Area2D, circle r=6, layer 8/mask 4) and SpinningBladePickup (.tscn + WeaponPickup.cs) placed in Level.tscn at (220, 120). SpinningBlade_weapon.tres: FireRate=0.6, BaseImpact=1.0, EightDirectional=true.
- **Task 3 — Power-up system:** FFPowerUpDefinition GlobalClass Resource (PowerUpKey, DisplayName, Type, EffectKey, SpriteKey). PowerUpEffects constants. Status effect system: abstract StatusEffect base with IDamageModifier/ISpeedModifier/IIncomingDamageModifier interfaces. RapidFireEffect, BerserkerEffect, HpRestoreEffect, ReverseControlsEffect, SlowEffect, MagnetEffect implementations. StatusEffectController node manages active effects per player, handles magnet radius pull toward pickups. PowerUp.cs rewired to dispatch via EffectKey switch. Three pickups placed in level: RapidFire (gold), ReverseControls (red), Berserker (purple).
- **Task 4 — 4-player input routing:** InputManager fully rewritten — MaxGamepadCount=3, per-device button state tracking, IsAnyButtonJustPressedOnDevice for join detection. LoadoutSelectController supports 4 players — P1 keyboard always joined, P2–P4 join by pressing any button on an unassigned gamepad; DPad navigates character select; X confirms. LoadoutSelect.tscn gains P3Label, P3Status, P4Label, P4Status nodes.
- **Task 5 — Placeholder boss fight:** BossRoomTrigger Area2D replaces ExitTrigger — on player entry spawns PlaceholderBoss from AssetRegistry, then transitions to BossIntro. BossIntroController: 2.5s title card then transitions to BossFight. PlaceholderBoss: three cycling attack patterns (Charge → Burst → Summon); dormant during BossIntro; 30 HP; emits HpChanged signal; on death transitions to VillainExit. VillainExitController: 2s hold then transitions to RunSummary. HudController connects to boss HpChanged signal and shows BossHpBar only during BossFight. GameStateManager updated: tracks _stateBeforeRevive so ReviveWindow correctly returns to BossFight (not always Segment); ReviveWindow → BossFight added to legal transitions; BossIntro/BossFight entry clears _downPlayers.
- **Task 6 — RunSummary polish:** "Enemies Defeated: X", "Times Wiped: X", "Time: MM:SS" display. Two-option menu with cursor: "Play Again" → LoadoutSelect, "Quit" → Title. DPad/arrow key navigation, Enter/Space/Z/X/A/Start to confirm.

**Decisions:**
- `_stateBeforeRevive` saved in `TransitionTo()` BEFORE `Current` is updated — saving it in HandleStateEntry fires after Current is already set to ReviveWindow, which would always return ReviveWindow itself.
- Boss remains dormant during BossIntro by checking `_gameState2.Current == GameState.BossFight` before every attack tick — boss is spawned before the title card but won't engage until state advances.
- HudController uses group query + type-check (`is PlaceholderBoss boss`) rather than a typed reference — keeps it decoupled from boss identity, only connects the typed HpChanged signal once per BossFight state entry.
- MagnetEffect radius/speed constants live in StatusEffectController (engine layer) rather than PowerUpEffects (content) — these are implementation details of the pull physics, not design content.
- SA1204 (static before non-static) enforced by StyleCop — reorganized RunSummaryController to put static helpers before instance methods.

**Deferred:**
- Real character art for Croc and Hammerhead (Phase 3)
- Audio/SFX pass (Phase 3)
- Boss identity and variant attacks (Phase 3)
- Character secondaries (Croc slide-hit, Honey Badger invincibility) (Phase 3)
- Bear/Honey Badger size differential visual verification (needs real sprites)

**Next:** Phase 3 — Real sprites, audio, chapter structure, generator integration.

**Tests added:** None new — all 10 existing tests still pass.

---

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
