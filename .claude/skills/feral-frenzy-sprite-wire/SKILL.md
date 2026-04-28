---
name: feral-frenzy-sprite-wire
description: "Use this skill whenever a new spritesheet or contract JSON has been dropped into the Feral Frenzy project and needs to be wired up. Triggers on phrases like 'wire up the sprite', 'new spritesheet arrived', 'I dropped in the art', 'hook up the enemy sprite', 'integrate the spritesheet', 'sprite contract is ready', 'connect the animation frames', or any mention of a PNG or contract JSON file being added to assets/sprites/. Also use when the developer asks to verify import settings on a spritesheet, generate keyframe reference documents for AnimationPlayer clips, or check that SpriteFramesBuilder is hooked up correctly. This skill handles the full integration pipeline: import verification, AssetKeys registration, manifest update, SpriteFramesBuilder hookup, and keyframe reference generation."
---

# Feral Frenzy — Sprite Wire Skill

Integrates a new spritesheet + contract JSON into the Feral Frenzy project.
Covers simple enemies (AnimatedSprite2D path) and characters (AnimationPlayer path).

## Before Starting

Read the project architecture documents if not already in context:
- `CLAUDE.md` — asset registry rules, three-layer separation, never-do list
- `docs/phase_briefs/02_5_phase_2_5.md` — Developer Plug-in Points section

## Step 1 — Identify What Arrived

Determine entity type and rendering path from the user's message or by inspecting the dropped files.

**Simple enemy — standard states** (AnimatedSprite2D + FFSimpleEnemyState, Path A):
- Has a _sprite_contract.json alongside the PNG
- Behavior node does NOT implement IAnimationSetup
- Uses all five states or any subset — subset needs no code change, just omit unused states from clips and rules

**Simple enemy — custom states** (AnimatedSprite2D + custom enum, Path C):
- Has a _sprite_contract.json alongside the PNG
- Behavior node implements IAnimationSetup — check the behavior .cs file
- Has its own FF-prefixed state enum in FeralFrenzy.Core/src/core/data/content/
- Contract animation names must match the clip names in the behavior's Configure() method

**Character** (AnimationPlayer path, Path B):
- Bear, Honey Badger, Croc, Hammerhead
- May have multiple sheets: body, arm, weapon
- No contract JSON needed for SpriteFramesBuilder — clips are hand-keyed
- Generates a keyframe reference document instead

**Boss** (AnimationPlayer path, same as character, Path B):
- PlaceholderBoss or real villain
- Treated identically to character path

If the entity type is unclear, check the behavior node .cs file for IAnimationSetup. If it implements that interface, use Path C. Otherwise use Path A.

---

## Path A — Simple Enemy (FFSimpleEnemyState)

Follow references/simple-enemy-path.md for the full step-by-step.

Quick summary:
1. Verify import settings
2. Derive asset key from entity name
3. Add key to AssetKeys.cs
4. Add entry to assets_manifest.json
5. Uncomment / add SpriteFramesBuilder lines in EnemyHost._Ready() or behavior node
6. Validate contract JSON — include only animations for states the enemy actually uses
7. Generate verification report

Subset note: An enemy that only needs Idle/Walk/Death simply omits Attack and Hit from its
clips dictionary and rule list. The driver's TryGetValue skips unmapped states silently;
HasAnimation() skips missing clips. No new enum, no new code.

---

## Path B — Character or Boss

Follow references/character-path.md for the full step-by-step.

Quick summary:
1. Verify import settings on all sheets
2. Add asset keys to AssetKeys.cs
3. Add entries to assets_manifest.json
4. Assign textures to Sprite2D nodes in the .tscn
5. Generate keyframe reference document (the Rect2 values to enter in AnimationPlayer)
6. Instructions for developer: how to use the keyframe doc in Godot editor

---

## Path C — Simple Enemy (Custom Animation States)

For enemies whose behavior node implements IAnimationSetup with a custom FF-prefixed enum.

1. Verify import settings — same as Path A.

2. Read the behavior node to find the custom state enum and its clip name mappings:
   grep -n "IAnimationSetup\|BuildAnimation\|AnimationNames\|WithClips" src/godot/enemies/behaviors/{BehaviorName}.cs

3. Validate the contract JSON — confirm every animation name in the contract matches a clip
   name mapped in the behavior node's Configure() call. Names absent from WithClips are
   silently skipped by the driver — not an error, but flag them in the report.

4. Derive asset key and register — same as Path A Steps 3-5.

5. Wire SpriteFramesBuilder in the behavior node's Configure() method, not EnemyHost._Ready().
   Look for the commented-out block there. The pattern:

     AnimatedSprite2D? sprite = host.GetNodeOrNull<AnimatedSprite2D>(NodePaths.AnimatedSprite);
     FFSpriteContract contract = SpriteFramesBuilder.LoadContract(
         "res://assets/sprites/enemies/{folder}/{entity}_sprite_contract.json");
     Texture2D? sheet = assetRegistry.Load<Texture2D>(AssetKeys.Spritesheet{PascalEntity});
     if (sprite is not null && sheet is not null)
         sprite.SpriteFrames = SpriteFramesBuilder.Build(sheet, contract);

6. Build — dotnet build, must be clean.

7. Generate verification report — same structure as Path A, with an added section:
   Custom enum:  FF{Name}State  ({N} states)
   Clip mapping: {state} -> {animation name}, ...
   Contract animations not in WithClips (skipped silently): {list or "none"}

---

## Asset Key Naming Convention

Derive from entity name — always lowercase with underscores:

| Entity                    | Key pattern                   | Example                        |
|---------------------------|-------------------------------|--------------------------------|
| Simple enemy spritesheet  | spritesheet_{entity_key}      | spritesheet_ground_patroller   |
| Character body sheet      | spritesheet_{char}_body       | spritesheet_bear_body          |
| Character arm sheet       | spritesheet_{char}_arm        | spritesheet_bear_arm           |
| Character weapon sheet    | spritesheet_{char}_weapon     | spritesheet_bear_weapon        |

Entity keys come from the contract JSON entityKey field or from FFCharacterDefinition.CharacterKey.

---

## Import Settings — Always Verify First

Every spritesheet must have these settings or sprites will be blurry. Check before anything else.

In Godot FileSystem panel:
1. Click the PNG file
2. Open Import tab
3. Confirm:
   - Filter: Nearest
   - Mipmaps: Off (Generate = false)
   - Compress Mode: Lossless
4. If any setting is wrong: fix it, click Reimport, confirm no blurring

This is a hard requirement. Do not proceed if import settings are wrong.

---

## Never Do

- Add GD.Load<T>() with a hardcoded path — always go through AssetRegistry
- Skip import settings verification — always check first
- Modify AnimationPlayer clips — that is the developer's job
- Create a new AssetKeys constant that duplicates an existing one — check first
- Write a contract JSON — the developer writes this alongside the art in Aseprite
- Create a custom animation enum for an enemy that only needs a subset of FFSimpleEnemyState — just omit the unused states from clips and rules; the driver skips them silently
- Wire SpriteFramesBuilder in EnemyHost._Ready() for a Path C enemy — it belongs in the behavior node's Configure() method
- Call host.BuildAnimation<T>() outside of IAnimationSetup.Configure() — only valid during _Ready()
