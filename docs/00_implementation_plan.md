# Feral Frenzy — Implementation Plan
**Version:** 1.0  
**Status:** Authoritative  
**Depends on:** 01_schema.md, 02_state_machine.md  
**Referenced by:** 03_claude_md.md

---

## The Governing Principle

**Playable beats complete. Always.**

Every phase ends with something you can pick up a controller and feel. Infrastructure that doesn't produce a playable result within its phase is deferred. The generator does not exist until Phase 3. The WFC micro-generator does not exist until Phase 4. The level editor does not exist until Phase 5. None of this compromises the final architecture — it just means you build toward it from a working game, not toward a game from a working architecture.

The architecture documents define the destination. This plan defines the road.

---

## Phase 0 — Foundation (1–2 days)
**Exit condition: Godot project compiles, solution structure exists, one character moves on screen.**

This phase is pure setup. No gameplay. No generator. No JSON. Just the project skeleton that everything else builds on.

### What gets built
- Godot 4 project created, .NET solution configured
- Folder structure created per architecture (empty folders with .gitkeep)
- `src/core/` configured as a separate .NET class library with zero Godot dependencies
- `src/core/data/engine/` — all enums and records from 01_schema.md defined (no logic yet)
- `AssetRegistry` autoload — stub only, loads a hardcoded manifest, no custom asset support yet
- `GameStateManager` autoload — stub only, holds `Current` state, no transition validation yet
- `InputManager` autoload — stub only, maps keyboard to player 1
- One `CharacterDefinition` resource file for Bear (.tres)
- One `PlayerController.cs` attached to a `CharacterBody2D` — reads from Bear's definition
- Bear moves, jumps, and slides on a flat test level with placeholder geometry
- Project builds and runs without errors

### What does NOT get built in Phase 0
- No animation
- No combat
- No enemies
- No weapons
- No other characters
- No state machine transitions
- No JSON loading
- No generator

### Why this phase exists
It establishes that the solution structure works, the C# class library compiles independently, and the Godot/C# bridge is functioning. Every subsequent phase builds on a known-good foundation.

---

## Phase 1 — Vertical Slice (2–3 weeks)
**Exit condition: One handcrafted level is fun to play with two players. 30-entity density stress test passes at 60fps. This is the first moment the game feels like itself.**

This is the most important phase. Everything here is handcrafted and hardcoded where necessary — none of it uses the generator, and that is correct. The goal is to find the fun as fast as possible. A handcrafted level that feels great teaches you more about what the generator needs to produce than any amount of upfront generator design.

### What gets built

**All four characters playable:**
- All four `CharacterDefinition` .tres files
- `PlayerController.cs` handles all four — reads from whichever definition is assigned
- Character-specific stats: speed, jump arc, gap fitting (Honey Badger), extra hit (Bear)
- Placeholder sprites (colored rectangles are fine — silhouette shapes preferred)
- Basic animation states: idle, walk, jump, slide, fall

**Core movement — Mega Man X standard:**
- Run, double jump, wall kick, slide, eight-directional aiming
- All movement completable by sliding Croc (solvability reference enforced from day one)

**Local co-op input:**
- `InputManager` fully implemented — device routing for 1–4 players
- Player 1: keyboard or controller 0
- Players 2–4: controllers 1–3
- Characters selectable on a simple pre-game screen (no polish needed)

**One weapon — the default blaster:**
- Mega Buster style, rapid fire, eight-directional
- Damage scales with character size (WeaponDamageMultiplier applied)
- No weapon selection screen yet — everyone starts with the blaster

**One handcrafted level:**
- Built directly in Godot's TileMap editor — this is the one time we use it directly
- Chapter 1 feel: open, outdoor, rolling terrain, long sightlines
- Enough geometry to exercise wall kicks, double jumps, and slides
- 3–5 enemy types, hand-placed
- One power-up, hand-placed
- An exit trigger that ends the level

**Basic enemy behavior:**
- 2–3 enemy types with simple patrol/chase/shoot AI
- Enemy counts do NOT scale with player count yet — fixed counts
- Enemies die, drop no loot

**Death and revival:**
- Full death/revival system per 02_state_machine.md
- ReviveWindow, SegmentRestart, solo rules — all implemented
- This is non-negotiable for phase 1 because co-op feel depends on it

**Minimal state machine:**
- Title (static, just a "Press Start" screen)
- LoadoutSelect (character pick, no weapon pick yet)
- Segment (the handcrafted level)
- ReviveWindow
- SegmentRestart
- RunSummary (static — just shows "Run Complete", no stats yet)
- Transitions validated per legal transition table

**Camera:**
- Shared screen, all players visible at all times
- Basic follow — weighted average of all player positions
- No split, no edge cases yet

**Main scene structure — SubViewport established here:**
- Game world renders inside a `SubViewport` from Phase 1 onward
- UI lives on a `CanvasLayer` above the `SubViewport`
- This enables all screen-space shader effects (explosion ripple, Bear's Roar shockwave, hit flash, lava glow, silhouette mechanic) without restructuring later
- Cost in Phase 1: near zero. Cost if deferred to Phase 4: painful restructure.

**Entity pooling architecture:**
- Pool/recycle system established in Phase 1 — not retrofitted later
- Target: 20–30 visible entities simultaneously at 60fps
- Density stress test: spawn 30 entities with basic patrol AI, confirm frame budget holds
- This is a Phase 1 exit milestone — if it fails, the architecture is fixed before the generator exists

**Parallax system — Phase 1 deliverable:**
- Engine-layer parallax feature with content-layer configuration
- Parallax layer definitions live in chapter JSON, not hardcoded in scenes
- Must work correctly inside the `SubViewport` — verify no shader or camera conflicts
- Phase 1 implements Chapter 1 parallax only (clouds, distant mountains, pterodactyls)

**Camera validation:**
- Validate zoom level against panoramic intent — battlefield scale, not corridor scale
- Validate UI element readability at couch distance on a large display
- Vertical layer depth (enemies above, below, and at player level simultaneously) confirmed playable

### What does NOT get built in Phase 1
- No generator (levels are handcrafted)
- No JSON loading for level data (level lives in Godot scene)
- No weapon selection
- No second or third weapon
- No boss fight
- No cinematics
- No audio
- No chapter 2 or 3 content
- No genre levels (Gradius, Brawler)
- No meta progression
- No unlock system
- No asset registry custom skin support
- No attract mode
- No level editor

### Why this order
Movement and combat feel must be established before anything else. The generator cannot be designed correctly until you know what a good segment feels like to play. Phase 1 produces that knowledge. Every design decision made in Phase 2 onward is grounded in something you've actually played.

---

## Phase 2 — Combat Depth (2–3 weeks)
**Exit condition: The game has enough weapon variety and enemy variety to feel chaotic. Two strangers can pick it up and have a great time.**

Phase 1 proved the movement and co-op feel. Phase 2 proves the combat loop.

### What gets built

**Weapon system:**
- `WeaponDefinition` fully implemented
- Tier 1 (default blaster) — already exists
- Tier 2 (Spinning Blade) — Metal Blade style, eight-directional disc
- Weapon selection in LoadoutSelect screen
- Weapons carried through the full level

**3–5 enemy types per chapter 1 feel:**
- Mounted dinosaur (ranged attack from a moving platform)
- Ground trooper (rush attack)
- Aerial threat (dive bomb)
- Enemy difficulty weights implemented against DifficultyBudget
- Enemy counts scale with player count (this is the moment co-op gets noticeably easier)

**Character secondaries:**
- All four secondaries implemented: Roar, Jaw Slam, Reverse Fire, Pinball Jump
- Bear's Roar staggers all enemies on screen — the co-op force multiplier
- These are the moments the game becomes memorable

**Power-ups:**
- 4–6 power-up types, mix of positive and negative/double-edged
- Hand-placed in the existing level
- The hesitation mechanic: players pause before grabbing unknowns

**One handcrafted boss encounter:**
- Not Baroness Cretacia yet — a placeholder boss with 3 attack patterns
- Tests the boss fight state and transition back to run summary
- BossIntro state (brief, no cinematic yet — just a title card)

**Audio — minimal:**
- Weapon fire sounds
- Hit sounds
- Death sounds
- Background music loop for Chapter 1 feel
- No voice, no cinematics audio yet

**RunSummary — real stats:**
- Kill count, death count, time
- No unlock reveals yet

### What does NOT get built in Phase 2
- No generator
- No second handcrafted level
- No cinematics
- No Tier 3 weapons
- No meta progression/unlocks
- No chapter 2 or 3 content
- No genre levels

---

## Phase 3 — The Generator (3–4 weeks)
**Exit condition: The game generates a valid, playable Chapter 1 run from a seed. Every run is different. The handcrafted level is retired.**

This is when the architecture investments pay off. The schema from document 1 is already defined. The state machine from document 2 is already running. The generator slots into the existing infrastructure.

### What gets built

**The full generator pipeline:**
- `ChapterGenerator.cs` — produces a sequence of `SegmentData` from a seed and chapter config
- `MacroValidator.cs` — enforces all solvability and budget rules from 01_schema.md
- `GodotImporter.cs` — translates `SegmentData` → TileMap geometry
- `RunSpine.cs` — fully implemented per 02_state_machine.md
- `RunData` serialization — runs are reproducible from a seed

**Chapter 1 content JSON:**
- `chapter_cretaceous.json` — enemy pool, hazard pool, mechanic pool, tileset key
- All Chapter 1 enemy definitions
- All Chapter 1 mechanic tags

**WFC micro-generator — basic:**
- Fills geometry within the constraints the importer establishes
- Produces varied terrain within a segment's geometry profile
- Not sophisticated yet — correct and playable is the goal

**`AssetRegistry` fully implemented:**
- Loads from `assets_manifest.json`
- All Chapter 1 assets registered
- No custom skin support yet — that's Phase 5

**GameStateManager fully implemented:**
- Full legal transition table enforced
- All payloads implemented
- RunSpine integrated

**Enemy count scaling:**
- Already partially done in Phase 2
- Now fully data-driven from `FFEnemyDefinition.BaseCountPerPlayer`

### What does NOT get built in Phase 3
- No Chapter 2 or 3 content
- No genre levels
- No cinematics
- No boss cinematic (BossIntro is still a title card)
- No level editor
- No Tier 3 weapons
- No meta progression

---

## Phase 4 — Full Run (4–6 weeks)
**Exit condition: A complete 30-minute run is playable from title screen to run summary. All three chapters. Both genre levels. All three bosses (placeholder okay for boss fights). The Laugh heard three times.**

This is the longest phase and the one most likely to be broken into sub-phases as you go.

### What gets built

**Chapter 2 and 3:**
- Content JSON for both chapters
- Enemy pools, hazard pools, mechanic pools
- Tilesets
- Environmental hazards as first-class generator primitives (Ch3)
- Destructible geometry system — full implementation
- The guaranteed destructible level in Ch2
- The 50% surprise destructible rule

**All three villain boss fights:**
- Baroness Cretacia (Ch1)
- Professor Static (Ch2)
- Lord Inferno (Ch3)
- Each with 2–3 attack phases
- VillainExit sequences
- The Laugh beat — audio + shadow silhouette, never skippable

**Genre levels:**
- Gradius level (The Commute) — horizontal shooter, self-contained module
- Brawler level (The Layover) — top-down beat em up, self-contained module
- Full mode switch implemented per 02_state_machine.md

**Cinematics system — minimal:**
- Chapter intro title cards (not full animation — text and atmosphere)
- Boss intro sequences
- Villain exit sequences
- The Laugh moments
- Skippable flag respected

**The Recurring One:**
- Minor villain who appears in all three chapters
- Different outfit each time, same energy
- Simple encounter, always dispatched

**Meta progression — minimal:**
- Cosmetic unlocks
- Run achievements that gate unlocks
- Unlock reveal in RunSummary

**Tier 3 weapons — first batch:**
- 3–4 unlockable weapons
- Each produces a genuine "wait, THIS exists?" moment

**Audio — complete:**
- Full music for all three chapters
- Genre level music
- Boss fight music
- Villain exit music
- The Laugh audio

---

## Phase 5 — Polish and Shipping (ongoing)
**Exit condition: Steam page live. Public demo available.**

### What gets built
- Full pixel art for all characters, enemies, environments
- Level editor — standalone tool, same JSON as generator
- `AssetRegistry` custom skin support
- Player face photo upload system
- Custom weapon skins
- Attract mode (recorded playback)
- Full cinematic sequences (animated, not title cards)
- Steam Workshop infrastructure (deferred from launch if needed)
- Controller support verification across hardware
- TV/big screen compatibility
- Performance optimization
- Full playtesting and difficulty tuning

---

## The Non-Negotiables Across All Phases

These rules apply from Phase 0 onward and are never relaxed:

1. `src/core/` has zero Godot API dependencies at all times
2. No hardcoded asset paths — everything goes through AssetRegistry (even stubs)
3. All levels must be passable by a sliding Croc — enforced from the first handcrafted level
4. No content-layer types in engine-layer code — FF prefix violations are bugs
5. Every C# build must be clean before committing — no warning suppression
6. Base resolution is 320×180 — locked, never changed, never proposed for change
7. Engine layer contains zero Feral Frenzy-specific names — three-layer separation always

---

## The Milestones That Matter

| Milestone | Phase | What it proves |
|---|---|---|
| Bear moves on screen | 0 | Solution structure works |
| Two strangers have fun | 1 | Core loop is real |
| 30 entities at 60fps | 1 | Performance architecture is sound |
| Parallax live in Chapter 1 | 1 | Content-layer pipeline works end to end |
| Combat feels chaotic | 2 | Weapon and enemy systems work |
| Generator produces Chapter 1 | 3 | Architecture investment pays off |
| Full run is playable | 4 | The game exists |
| Steam page live | 5 | Ship it |
