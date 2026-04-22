# Feral Frenzy — Architecture Document 1: Data Schema
**Version:** 1.0  
**Status:** Authoritative  
**Depends on:** FERAL_FRENZY_BIBLE.md  
**Referenced by:** 02_state_machine.md, 03_claude_md.md, all generator and importer code

---

## Purpose

This document defines every data type in the system — the C# records, the enums, the JSON representations, and the boundary between the two JSON namespaces. It is the contract between the generator, the validator, the importer, and the level editor. All four systems speak this language. Nothing gets built until this is agreed.

---

## The Two JSON Namespaces

The bible establishes two distinct namespaces. This is not a style choice — it is an architectural boundary.

**Engine schema** — stable, versioned, game-agnostic. Describes segments, entities, budgets, and solvability constraints in terms the engine understands. The engine never sees a honey badger. Changes here are breaking changes requiring migration. Prefix: none (these are the base types).

**Content schema** — Feral Frenzy-specific, freely changeable. Describes chapters, characters, enemies, weapons, villains, and story beats. The importer translates content schema → engine schema. Nothing else crosses this boundary. Prefix: `FF` on all content-layer C# types to make violations immediately visible.

**The rule:** If a type name or field would make sense in a game about space trading, it belongs in the engine schema. If it contains the word "dinosaur," "honey badger," "lava," or anything Feral Frenzy-specific, it belongs in the content schema.

---

## Engine Schema

### Enums

```csharp
// src/core/data/engine/SegmentType.cs
public enum SegmentType
{
    Opening,
    Combat,
    Platforming,
    Breather,
    Setpiece,
    Boss
}

// src/core/data/engine/GeometryProfile.cs
public enum GeometryProfile
{
    Open,       // wide horizontal space, long sightlines (Ch1 personality)
    Corridor,   // tight interior, ceilings matter, vertical stacking (Ch2 personality)
    Hybrid      // semi-open, dramatic verticality (Ch3 personality)
}

// src/core/data/engine/DestructibleLevel.cs
public enum DestructibleLevel
{
    None,
    Partial,    // some destructible geometry near enemies
    Heavy       // majority destructible; exactly one segment per run is Heavy
}

// src/core/data/engine/HazardClass.cs
public enum HazardClass
{
    None,
    Environmental,  // flame bars, lava rises, dropping platforms
    Enemy,          // wave density is the primary threat
    Both            // Ch3 hybrid — hazard budget replaces some enemy budget
}

// src/core/data/engine/PlatformMotivation.cs
public enum PlatformMotivation
{
    Tactical,       // platforms as cover and positioning, not puzzle
    Traversal,      // platforms as the primary movement challenge
    Dramatic        // platforms that move, collapse, or change — setpiece feel
}

// src/core/data/engine/SightlineRating.cs
public enum SightlineRating
{
    Short,   // corridor, ambush-heavy, close quarters
    Medium,
    Long     // open terrain, ranged play rewarded
}

// src/core/data/engine/RewardNodeType.cs
public enum RewardNodeType
{
    Positive,       // majority of power-ups
    Negative,       // rare — players learn to hesitate
    DoubleEdged     // rare — contextually good or bad
}

// src/core/data/engine/GeometryTag.cs
public enum GeometryTag
{
    Indestructible, // critical path — never destroyed
    Destructible,   // can be destroyed by explosives
    ChainDestructible // barrel → wall → floor cascade
}
```

### Core Engine Records

```csharp
// src/core/data/engine/RewardNode.cs
public record RewardNode(
    RewardNodeType Type,
    string PowerUpKey      // key into content AssetRegistry, not a hardcoded name
);

// src/core/data/engine/SegmentData.cs
// This is the primary output of the generator and the primary input to the importer.
public record SegmentData(
    string SegmentId,           // unique per run, used for seed reproducibility
    string ChapterKey,          // content-layer key, e.g. "chapter_cretaceous"
    SegmentType Type,
    GeometryProfile Geometry,
    bool CeilingPresent,        // changes combat feel entirely — not just cosmetic
    DestructibleLevel Destructible,
    float DifficultyBudget,     // 0.0–1.0, scripted not dynamic
    HazardClass HazardClass,
    PlatformMotivation PlatformMotivation,
    SightlineRating Sightline,
    List<RewardNode> RewardNodes,
    List<string> EnemyRoster,   // keys into content enemy pool for this chapter
    string UniqueMechanicTag,   // enforced unique across all segments in a run
    int PlayerCountAtGeneration // enemy counts baked at gen time, not dynamic
);

// src/core/data/engine/RunData.cs
// The full serialized output of a run. Reproducible from seed.
public record RunData(
    string RunId,
    int Seed,
    string SchemaVersion,       // bump on breaking engine schema changes
    List<SegmentData> Segments, // ordered spine — segments play in this order
    bool HasSurpriseDestructible // the 50% extra destructible rule
);

// src/core/data/engine/ValidationError.cs
public record ValidationError(
    string SegmentId,
    string Rule,
    string Message
);

// src/core/data/engine/ValidationResult.cs
public record ValidationResult(
    bool IsValid,
    List<ValidationError> Errors
);
```

### JSON Representation (Engine Schema)

```json
{
  "runId": "run_20240815_001",
  "seed": 48291,
  "schemaVersion": "1.0",
  "hasSurpriseDestructible": true,
  "segments": [
    {
      "segmentId": "seg_001",
      "chapterKey": "chapter_cretaceous",
      "type": "Opening",
      "geometry": "Open",
      "ceilingPresent": false,
      "destructible": "None",
      "difficultyBudget": 0.15,
      "hazardClass": "Enemy",
      "platformMotivation": "Tactical",
      "sightline": "Long",
      "rewardNodes": [
        { "type": "Positive", "powerUpKey": "powerup_rapid_fire" }
      ],
      "enemyRoster": ["enemy_raptor_rider", "enemy_ptero_bomber"],
      "uniqueMechanicTag": "mechanic_dino_riding",
      "playerCountAtGeneration": 2
    }
  ]
}
```

---

## Content Schema (Feral Frenzy Layer)

All types prefixed `FF`. All files live under `data/` in the project root. The importer reads these at runtime. The generator reads chapter and enemy definitions to build segment sequences.

### Character Definitions

```csharp
// src/core/data/content/FFCharacterSize.cs
public enum FFCharacterSize
{
    Tiny,   // Honey Badger — always fits tight gaps
    Medium, // Hammerhead
    Large,  // Bear
    Huge    // Croc — solvability reference character
}

// src/core/data/content/FFCharacterDefinition.cs
// Serialized as .tres Resource in Godot for Inspector editing.
// Also exportable to JSON for tooling.
[GlobalClass]
public partial class FFCharacterDefinition : Resource
{
    [Export] public string CharacterKey { get; set; }   // "char_bear", "char_croc" etc.
    [Export] public string DisplayName { get; set; }
    [Export] public FFCharacterSize Size { get; set; }
    [Export] public float MoveSpeed { get; set; }
    [Export] public float JumpVelocity { get; set; }
    [Export] public float JumpArcMultiplier { get; set; }  // 1.0 = standard, 0.7 = Croc lower, 1.3 = HB further
    [Export] public bool AlwaysFitsGaps { get; set; }      // true for Honey Badger only
    [Export] public bool HasExtraHit { get; set; }         // true for Bear only
    [Export] public float WeaponDamageMultiplier { get; set; } // size IS the affinity
    [Export] public string SecondaryAbilityKey { get; set; }   // "ability_roar", "ability_jaw_slam" etc.
    [Export] public string SpriteFramesKey { get; set; }   // AssetRegistry key, not a path
    [Export] public string PortraitKey { get; set; }       // AssetRegistry key
}
```

JSON representation (for tooling and reference):
```json
{
  "characterKey": "char_croc",
  "displayName": "Croc",
  "size": "Huge",
  "moveSpeed": 85.0,
  "jumpVelocity": -260.0,
  "jumpArcMultiplier": 0.72,
  "alwaysFitsGaps": false,
  "hasExtraHit": false,
  "weaponDamageMultiplier": 1.4,
  "secondaryAbilityKey": "ability_jaw_slam",
  "spriteFramesKey": "sprite_croc",
  "portraitKey": "portrait_croc"
}
```

### Weapon Definitions

```csharp
// src/core/data/content/FFWeaponTier.cs
public enum FFWeaponTier
{
    Default,    // Tier 1 — available from start, Mega Buster style
    Discoverable, // Tier 2 — visible in weapon select screen, Spinning Blade
    Unlockable  // Tier 3 — earned through run achievements
}

// src/core/data/content/FFWeaponDefinition.cs
[GlobalClass]
public partial class FFWeaponDefinition : Resource
{
    [Export] public string WeaponKey { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public FFWeaponTier Tier { get; set; }
    [Export] public bool IsChargeable { get; set; }     // charging is weapon feature, not character
    [Export] public bool IsExplosive { get; set; }      // determines destructible level liability
    [Export] public bool EightDirectional { get; set; } // always true — non-negotiable per bible
    [Export] public string ProjectileKey { get; set; }  // AssetRegistry key
    [Export] public string SpriteKey { get; set; }      // AssetRegistry key
    [Export] public string SoundKey { get; set; }       // AssetRegistry key
    // Note: no base damage stat — damage = weapon impact × character WeaponDamageMultiplier
}
```

### Enemy Definitions

```csharp
// src/core/data/content/FFEnemyDefinition.cs
// Enemy behavior is defined in code; this record describes identity and budget cost.
[GlobalClass]
public partial class FFEnemyDefinition : Resource
{
    [Export] public string EnemyKey { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public string ChapterKey { get; set; }         // which chapter pool this belongs to
    [Export] public float DifficultyWeight { get; set; }    // cost against segment DifficultyBudget
    [Export] public bool IsEliteVariant { get; set; }       // Ch3 remixed enemies
    [Export] public string SceneKey { get; set; }           // AssetRegistry key → PackedScene
    [Export] public int BaseCountPerPlayer { get; set; }    // scales with player count at runtime
}
```

### Chapter Definitions

```csharp
// src/core/data/content/FFChapterDefinition.cs
// Read by the ChapterGenerator to parameterize segment production.
public record FFChapterDefinition(
    string ChapterKey,
    string DisplayName,
    GeometryProfile PreferredGeometry,
    SightlineRating DefaultSightline,
    bool CeilingsPreferred,
    float BaseHazardBudgetRatio,    // 0.0 = all enemies, 1.0 = all hazards
    List<string> EnemyPool,         // enemy keys available in this chapter
    List<string> HazardPool,        // hazard keys available in this chapter
    List<string> MechanicPool,      // unique mechanic tags available in this chapter
    string VillainKey,
    string TilesetKey               // AssetRegistry key
);
```

JSON representation:
```json
{
  "chapterKey": "chapter_cretaceous",
  "displayName": "The Cretaceous",
  "preferredGeometry": "Open",
  "defaultSightline": "Long",
  "ceilingsPreferred": false,
  "baseHazardBudgetRatio": 0.1,
  "enemyPool": [
    "enemy_raptor_rider",
    "enemy_ptero_bomber",
    "enemy_ground_trooper",
    "enemy_triceratops_charge"
  ],
  "hazardPool": [
    "hazard_dino_stampede"
  ],
  "mechanicPool": [
    "mechanic_dino_riding",
    "mechanic_dino_back_platform",
    "mechanic_aerial_assault",
    "mechanic_stampede_chase"
  ],
  "villainKey": "villain_baroness_cretacia",
  "tilesetKey": "tileset_cretaceous"
}
```

### Power-up Definitions

```csharp
// src/core/data/content/FFPowerUpDefinition.cs
public record FFPowerUpDefinition(
    string PowerUpKey,
    string DisplayName,
    RewardNodeType Type,        // engine enum — power-ups bridge the schemas here
    bool AffectsDestructibleBalance, // if true, validator flags in destructible segments
    string EffectKey,           // key to behavior class
    string SpriteKey,           // AssetRegistry key
    string SoundKey             // AssetRegistry key
);
```

---

## The AssetRegistry Contract

No hardcoded asset paths anywhere in the codebase. Every sprite, scene, sound, and tileset is accessed through a string key resolved at runtime by `AssetRegistry`.

The manifest file lives at `data/assets_manifest.json`:

```json
{
  "schemaVersion": "1.0",
  "assets": {
    "sprite_bear": "res://assets/sprites/characters/bear.png",
    "sprite_croc": "res://assets/sprites/characters/croc.png",
    "sprite_hammerhead": "res://assets/sprites/characters/hammerhead.png",
    "sprite_honeybadger": "res://assets/sprites/characters/honeybadger.png",
    "tileset_cretaceous": "res://assets/tilesets/cretaceous.tres",
    "tileset_dead_station": "res://assets/tilesets/dead_station.tres",
    "tileset_infernal_keep": "res://assets/tilesets/infernal_keep.tres",
    "scene_char_bear": "res://scenes/characters/Bear.tscn",
    "scene_char_croc": "res://scenes/characters/Croc.tscn"
  }
}
```

Custom skins, workshop assets, and community content resolve through the same registry with override keys. The registry loads the manifest, applies any override manifests in order, and the last writer wins. This is the entire custom asset system.

---

## Solvability Constants

These are not magic numbers. They are documented constraints derived directly from the bible.

```csharp
// src/core/data/engine/SolvabilityConstants.cs
public static class SolvabilityConstants
{
    // The solvability reference character is Croc (Huge, slow, lower jump arc).
    // Every segment must be passable by a sliding Croc.
    // This is the single most important constraint in the generator.
    public const string ReferenceCharacterKey = "char_croc";

    // Minimum gap width passable by a sliding Croc (in tile units).
    // The generator never produces a gap narrower than this on the critical path.
    public const int MinCriticalPathGapTiles = 1;

    // Maximum difficulty budget ramp between consecutive segments.
    // A segment cannot be more than this much harder than the previous one.
    public const float MaxDifficultyRampPerSegment = 0.25f;

    // Exactly one segment per run must have DestructibleLevel.Heavy.
    // It must always be in Chapter 2 (Dead Station).
    public const int GuaranteedHeavyDestructibleCount = 1;
    public const string GuaranteedHeavyDestructibleChapter = "chapter_dead_station";

    // 50% of runs get one additional destructible segment outside Chapter 2.
    public const float SurpriseDestructibleProbability = 0.5f;
}
```

---

## Schema Versioning

The engine schema carries a `schemaVersion` field. When a breaking change is made to `SegmentData` or `RunData`, increment the version and write a migration in `src/core/data/migration/`. Saved runs from old versions must remain loadable.

The content schema carries no version — it is game-specific and freely changeable. Breaking changes to content schema do not require migration; they require updated content JSON files.

---

## What Is Not in This Document

The following are intentionally deferred to later design sessions per the bible:

- Full weapon list and Tier 3 unlock conditions
- Villain and boss data schemas (villain design session)
- Setpiece event schemas (setpiece design session)
- Fifth character definition
- Workshop/community content schema

When those sessions happen, add the resulting types to this document. This document is the living record of all defined schemas. If a type exists in code but not here, it is undocumented and should be added.
