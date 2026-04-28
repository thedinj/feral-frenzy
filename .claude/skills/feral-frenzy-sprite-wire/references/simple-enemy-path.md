# Simple Enemy Integration Path

For entities using AnimatedSprite2D + SpriteFramesBuilder + contract JSON.
Applies to: GroundPatroller, AerialDiver, MountedDino, PteroBomber, and any future simple enemy.

---

## Step 1 — Verify Import Settings

```bash
# In Godot FileSystem, click the PNG → Import tab
# Confirm:
#   Filter Mode: Nearest
#   Mipmaps > Generate: Off
#   Compress > Mode: Lossless
# If wrong: fix, Reimport, continue
```

If running from command line, check the `.import` file exists and has correct settings:
```bash
cat assets/sprites/enemies/{entity}/  {entity}_spritesheet.png.import
# Should contain: compress/mode=0, filter=false, mipmaps=false
```

---

## Step 2 — Validate the Contract JSON

Read the contract file the developer dropped in. Validate:

```
Required fields:
  entityKey    — string, matches the entity's key in FFEnemyDefinition
  frameWidth   — integer, pixels
  frameHeight  — integer, pixels
  animations   — array, at least one entry

Per animation:
  name         — must match an AnimationNames constant
  loop         — boolean
  fps          — float, > 0
  frames       — array of {x, y} objects, at least one frame

Frame coordinates:
  x, y are TILE indices, not pixel coordinates
  Verify: no frame index exceeds sheet dimensions
  sheet columns = sheetWidth / frameWidth
  sheet columns = sheetHeight / frameHeight
  All x values < sheet columns
  All y values < sheet rows
```

If validation fails, report exactly which field is wrong and what the correct value should be. Do not proceed until the contract is valid.

Check animation names against `AnimationNames.cs`:
```bash
grep -n "public const string" src/godot/constants/AnimationNames.cs
```

If the contract uses an animation name not in `AnimationNames.cs`, add it:
```csharp
// In AnimationNames.cs — add missing constant
public const string {PascalName} = "{snake_name}";
```

---

## Step 3 — Derive Asset Key

From the contract's `entityKey`, derive the spritesheet asset key:

```
entityKey:    "enemy_ground_patroller"
spritesheet key: "spritesheet_ground_patroller"
```

Pattern: strip `"enemy_"` prefix if present, prepend `"spritesheet_"`.

Check `AssetKeys.cs` for existing key:
```bash
grep "spritesheet_{derived_key}" src/godot/constants/AssetKeys.cs
```

---

## Step 4 — Add Asset Key to AssetKeys.cs

If the key doesn't exist, add it to the Enemies region:

```csharp
// In src/godot/constants/AssetKeys.cs
// Find the enemy spritesheets section and add:
public const string Spritesheet{PascalEntityName} = "spritesheet_{entity_key}";
```

Example:
```csharp
public const string SpritesheetGroundPatroller = "spritesheet_ground_patroller";
```

---

## Step 5 — Add Entry to assets_manifest.json

```bash
# Read current manifest
cat data/assets_manifest.json
```

Add the new entry in the assets object:

```json
"spritesheet_{entity_key}": "res://assets/sprites/enemies/{entity_folder}/{entity}_spritesheet.png"
```

The path must be `res://` relative and point to the exact PNG filename. Verify the file exists at that path:

```bash
ls assets/sprites/enemies/{entity_folder}/
```

---

## Step 6 — Wire SpriteFramesBuilder in Controller

Find the controller file:
```bash
find src/godot/enemies -name "*{EntityName}*Controller.cs"
```

Look for the commented-out SpriteFramesBuilder block in `_Ready()`. It looks like:

```csharp
// When developer drops in spritesheet + contract:
//   var contract = SpriteFramesBuilder.LoadContract("res://...");
//   var sheet = _assetRegistry.Load<Texture2D>(AssetKeys.Spritesheet{Entity});
//   sprite.SpriteFrames = SpriteFramesBuilder.Build(sheet, contract);
```

Uncomment and fill in the correct paths:

```csharp
var contract = SpriteFramesBuilder.LoadContract(
    "res://assets/sprites/enemies/{entity_folder}/{entity}_sprite_contract.json");
var sheet = _assetRegistry.Load<Texture2D>(AssetKeys.Spritesheet{PascalEntity});
if (sheet is not null)
    sprite.SpriteFrames = SpriteFramesBuilder.Build(sheet, contract);
else
    GD.PushWarning($"{Name}: spritesheet not found for key {AssetKeys.Spritesheet{PascalEntity}}");
```

If the block doesn't exist (new enemy added after Phase 2.5), add it directly after the sprite node lookup in `_Ready()`.

---

## Step 7 — Build and Verify

```bash
dotnet build
```

Must be clean. If build errors, fix before continuing.

---

## Step 8 — Generate Verification Report

Output a concise report for the developer:

```
SPRITE WIRE COMPLETE — {EntityName}

Import settings:    ✓ Nearest / no mipmaps
Contract valid:     ✓ {N} animations, {M} total frames
Asset key added:    AssetKeys.Spritesheet{PascalEntity} = "{key}"
Manifest updated:   ✓ "{key}" → "{path}"
Builder wired:      ✓ {ControllerName}._Ready()
Build:              ✓ Clean

Animations wired:
  {name} — {frameCount} frames, loop={loop}, {fps}fps
  ...

Next step for developer:
  1. Open HitboxDebugLevel.tscn in Godot
  2. Press Play (F5)
  3. Tab to {EntityName}
  4. Verify frames load and animate correctly
  5. Confirm hitbox sits correctly on the sprite
  6. If hitbox needs adjustment: modify CollisionShape2D size in {EntityName}.tscn
     Standing: target {W}×{H}px
```

If anything failed, list exactly what needs fixing.

---

## Hitbox Reference Values

Standard hitbox targets per entity type. These are starting points — developer adjusts visually.

| Entity | Standing W | Standing H | Notes |
|---|---|---|---|
| GroundPatroller | 12px | 18px | Medium humanoid |
| AerialDiver | 14px | 10px | Wide, low — flying |
| MountedDino | 28px | 20px | Rider + mount width |
| PteroBomber | 16px | 12px | Wide flying silhouette |
| PlaceholderBoss | 36px | 32px | Large — adjust per art |

For new enemies not in this table, estimate from frame dimensions:
- Width: ~60% of frameWidth
- Height: ~75% of frameHeight
- Always smaller than the visible sprite — generous to player
