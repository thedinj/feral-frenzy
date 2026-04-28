# Character / Boss Integration Path

For entities using AnimationPlayer + multiple Sprite2D nodes.
Applies to: Bear, Honey Badger, Croc, Hammerhead, PlaceholderBoss, real villains.

Characters do NOT use SpriteFramesBuilder. Their frames are hand-keyed as
region_rect tracks in AnimationPlayer by the developer. This skill's job is
to assign the textures to the Sprite2D nodes and generate the keyframe
reference document that makes hand-keying fast.

---

## Step 1 — Identify Which Sheets Arrived

A character may have up to three sheets:
- `{char}_body_spritesheet.png` — body + head frames (always present)
- `{char}_arm_spritesheet.png` — weapon arm frames (present if arm is a separate sheet)
- `{char}_weapon_spritesheet.png` — weapon frames (usually comes from weapon system, not here)

Check what the developer dropped:
```bash
ls assets/sprites/characters/{char}/
```

Also check if a contract JSON was included — for characters it's optional reference
material, not required for SpriteFramesBuilder. If present, use it to generate
the keyframe reference document (more accurate). If absent, note it in the report.

---

## Step 2 — Verify Import Settings on All Sheets

Same requirement as simple enemies — check every PNG:

```bash
# For each PNG in the character folder:
cat assets/sprites/characters/{char}/{sheet}.png.import
# Should contain: filter=false, mipmaps=false, compress/mode=0
```

If any sheet has wrong settings: fix in Godot Import tab, Reimport, then continue.

---

## Step 3 — Derive and Register Asset Keys

Character sheets use the pattern:
```
spritesheet_{char_key}_body    ← always
spritesheet_{char_key}_arm     ← if arm sheet exists
spritesheet_{char_key}_weapon  ← if weapon sheet exists
```

`char_key` comes from `FFCharacterDefinition.CharacterKey`:
- Bear: `char_bear`
- Honey Badger: `char_honeybadger`
- Croc: `char_croc`
- Hammerhead: `char_hammerhead`

Check for existing keys:
```bash
grep "spritesheet_char" src/godot/constants/AssetKeys.cs
```

Add any missing keys to `AssetKeys.cs` in the Characters section:

```csharp
public const string SpritesheetBearBody        = "spritesheet_bear_body";
public const string SpritesheetBearArm         = "spritesheet_bear_arm";
public const string SpritesheetHoneyBadgerBody = "spritesheet_honeybadger_body";
public const string SpritesheetHoneyBadgerArm  = "spritesheet_honeybadger_arm";
```

Add to `assets_manifest.json`:
```json
"spritesheet_bear_body": "res://assets/sprites/characters/bear/bear_body_spritesheet.png",
"spritesheet_bear_arm":  "res://assets/sprites/characters/bear/bear_arm_spritesheet.png"
```

---

## Step 4 — Assign Textures in the Character .tscn

Find the character scene:
```bash
find scenes/characters -name "{CharName}.tscn"
```

The scene should have this node structure (established in Phase 2.5):
```
{CharName} (CharacterBody2D)
  ├── AnimationPlayer
  ├── BodySprite (Sprite2D)    ← assign body sheet here
  ├── ArmSprite (Sprite2D)     ← assign arm sheet here
  └── WeaponSprite (Sprite2D)  ← weapon sheet (may stay null for now)
```

**Option A — Edit the .tscn directly (preferred for automation):**

Find the `BodySprite` node section in the `.tscn` and update the texture reference:

```
[node name="BodySprite" type="Sprite2D" parent="."]
texture = ExtResource("{id}")
region_enabled = true
region_rect = Rect2(0, 0, 48, 48)
```

Add an ExtResource entry at the top of the .tscn if the texture isn't already referenced:
```
[ext_resource type="Texture2D" path="res://assets/sprites/characters/bear/bear_body_spritesheet.png" id="N"]
```

**Option B — Report what to do in Godot editor:**

If direct .tscn editing is too fragile (e.g. .tscn has complex resource IDs), report the manual steps:
```
In Godot editor:
  1. Open scenes/characters/{CharName}.tscn
  2. Click BodySprite node
  3. Inspector → Texture → Load → select bear_body_spritesheet.png
  4. Region Enabled: On
  5. Region Rect: Rect2(0, 0, 48, 48)  ← first frame, top-left
  Repeat for ArmSprite with arm sheet.
```

Use Option A when the .tscn structure is straightforward. Use Option B when the
.tscn has complex nesting or resource ID collisions that make direct editing risky.
Always note which option was used in the verification report.

---

## Step 5 — Generate Keyframe Reference Document

This is the most valuable output of this skill for characters. The developer
uses this document while hand-keying AnimationPlayer clips in the Godot editor.

If a contract JSON exists, read it for frame coordinates. If not, infer from
the phase 2.5 brief's standard animation set.

### Standard character animation set (from Phase 2.5 brief)

If no contract JSON, use these defaults:

```
Frame size: 48×48px
Standard animations:
  idle:       4 frames
  walk_start: 2 frames
  walk:       6 frames
  jump:       3 frames
  fall:       2 frames
  slide:      2 frames
  death:      5 frames
  hit:        1 frame
```

### Keyframe reference document format

Generate a markdown document at:
`docs/art/{char_key}_keyframe_reference.md`

Content:

```markdown
# {CharName} — AnimationPlayer Keyframe Reference
Generated from: {contract_path or "standard defaults"}
Frame size: {W}×{H}px
Sheet: {sheet_path}

## How to use this document

1. Open {CharName}.tscn in Godot editor
2. Click the AnimationPlayer node
3. For each animation below:
   a. Select or create the animation clip
   b. Add a Property Track → BodySprite:region_rect
   c. For each keyframe row: move playhead to Time, right-click → Insert Key,
      set value to the Rect2 shown
   d. Set loop and speed as shown
   e. Repeat for ArmSprite:position (use the arm position guidance below)

---

## idle
Loop: true | Speed: 6 fps | Duration: {4/6 = 0.667}s

| Time   | BodySprite region_rect        | Notes          |
|--------|-------------------------------|----------------|
| 0.000  | Rect2({x0*48}, {y0*48}, 48, 48) | frame 0      |
| 0.167  | Rect2({x1*48}, {y1*48}, 48, 48) | frame 1      |
| 0.333  | Rect2({x2*48}, {y2*48}, 48, 48) | frame 2      |
| 0.500  | Rect2({x3*48}, {y3*48}, 48, 48) | frame 3      |

## walk_start
Loop: false | Speed: 10 fps | Duration: 0.2s
[One-shot — plays once then state machine transitions to walk]

| Time   | BodySprite region_rect          | Notes          |
|--------|----------------------------------|----------------|
| 0.000  | Rect2({x*48}, {y*48}, 48, 48)   | frame 0        |
| 0.100  | Rect2({x*48}, {y*48}, 48, 48)   | frame 1        |

## walk
Loop: true | Speed: 10 fps | Duration: 0.6s

[... all 6 frames ...]

## jump
Loop: false | Speed: 10 fps | Duration: 0.3s
[Frame 2 holds on descent — AnimationPlayer holds last frame when loop=false]

[... 3 frames ...]

## fall
Loop: true | Speed: 8 fps | Duration: 0.25s

[... 2 frames ...]

## slide
Loop: false | Speed: 10 fps | Duration: 0.35s
[One-shot. Frame 1 holds during slide duration.]
[IMPORTANT: Also add these tracks:]
  CollisionShape2D:disabled    → key at 0.05: true
  SlideCollisionShape2D:disabled → key at 0.05: false

[... 2 frames ...]

## death
Loop: false | Speed: 8 fps | Duration: 0.625s
[One-shot. Frame 4 holds — character stays down until revived.]

[... 5 frames ...]

## hit
Loop: false | Speed: 10 fps | Duration: 0.1s
[One-shot, single frame. Used with modulate hit flash.]

| Time   | BodySprite region_rect          |
|--------|----------------------------------|
| 0.000  | Rect2({x*48}, {y*48}, 48, 48)   |

---

## Arm and Weapon Positioning

Starting positions for ArmSprite and WeaponSprite tracks.
Tune these visually in the editor — these are starting points only.

For idle and walk:
  ArmSprite position:   Vector2(8, -16)   ← right of body, shoulder height
  WeaponSprite position: Vector2(16, -14) ← in front of arm

For slide:
  ArmSprite position:   Vector2(12, -6)   ← arm tucks down during slide
  WeaponSprite position: Vector2(18, -4)

For jump/fall:
  ArmSprite position:   Vector2(8, -14)   ← slightly raised
  WeaponSprite position: Vector2(16, -12)

These are single keyframes at time 0 for each animation (position held throughout).
Add an ArmSprite:position track and WeaponSprite:position track to each animation.

---

## Hitbox tracks for slide animation only

In the slide animation, add two additional boolean tracks:

Track: CollisionShape2D:disabled
  Time 0.00 → false  (standing shape enabled at start)
  Time 0.05 → true   (standing shape off during slide)

Track: SlideCollisionShape2D:disabled
  Time 0.00 → true   (slide shape disabled at start)
  Time 0.05 → false  (slide shape on during slide)

These are the only animations that need hitbox tracks.
All other animations use the standing CollisionShape2D throughout.
```

If a contract JSON was present, replace the Rect2 values with the actual computed
coordinates from the contract (x * frameWidth, y * frameHeight).

If no contract JSON, leave the coordinates as template values with a note that
the developer fills them in while looking at their Aseprite file.

---

## Step 6 — Build and Verify

```bash
dotnet build
```

Must be clean.

---

## Step 7 — Generate Verification Report

```
SPRITE WIRE COMPLETE — {CharName} (Character / AnimationPlayer path)

Import settings:
  {char}_body_spritesheet.png  ✓ Nearest / no mipmaps
  {char}_arm_spritesheet.png   ✓ Nearest / no mipmaps  (if present)

Asset keys added:
  AssetKeys.Spritesheet{PascalChar}Body = "spritesheet_{char}_body"
  AssetKeys.Spritesheet{PascalChar}Arm  = "spritesheet_{char}_arm"  (if present)

Manifest updated:
  "spritesheet_{char}_body" → "res://assets/sprites/characters/{char}/..."

Texture assignment:
  BodySprite.Texture  ✓ assigned  (via {Option A direct edit | Option B — manual step required})
  ArmSprite.Texture   ✓ assigned  (if arm sheet present)

Keyframe reference:
  ✓ Generated at docs/art/{char_key}_keyframe_reference.md
  {N} animations documented, {M} total keyframes

Build: ✓ Clean

─────────────────────────────────────────
DEVELOPER ACTION REQUIRED:

1. Open docs/art/{char_key}_keyframe_reference.md
2. Open scenes/characters/{CharName}.tscn in Godot editor
3. Click AnimationPlayer node
4. Hand-key each animation clip using the reference document
   (one region_rect track per animation, arm/weapon position tracks)
5. Open scenes/debug/HitboxDebugLevel.tscn
6. Press Play (F5), Tab to {CharName}
7. Verify animations play correctly
8. Verify hitbox sits on the sprite — adjust CollisionShape2D size if needed

Hitbox target:
  Standing: {W}×{H}px  (centered, feet at collision shape bottom)
  Slide:    {W}×{H/2}px
─────────────────────────────────────────
```

---

## Character Hitbox Reference

| Character | Standing W | Standing H | Slide H | Notes |
|---|---|---|---|---|
| Bear | 14px | 22px | 11px | Large — fills most of canvas height |
| Honey Badger | 8px | 14px | 7px | Tiny — much smaller than canvas |
| Croc | 18px | 24px | 12px | Huge — widest character |
| Hammerhead | 12px | 20px | 10px | Medium |

Honey Badger `AlwaysFitsGaps` verification:
After hitboxes are set, run the game and confirm:
- Honey Badger walks through the tight gap in Level.tscn without sliding
- Bear cannot walk through the same gap without sliding
- If either check fails, report the gap width and the hitbox width — one needs adjustment
