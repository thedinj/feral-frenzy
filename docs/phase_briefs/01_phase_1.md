# Feral Frenzy — Phase 1 Task Brief

**Version:** 1.0  
**For:** Claude Code  
**Status:** Ready to execute  
**Exit condition:** One level is fun to play with two players. Bear and Honey Badger move, shoot, and die correctly. 30-entity density stress test passes at 60fps. Keyboard + one gamepad both work.

---

## Before You Start

1. Read `CLAUDE.md` completely
2. Read `docs/00_implementation_plan.md` — confirm you are in Phase 1
3. Read `docs/01_schema.md` — content types are referenced throughout
4. Read `docs/02_state_machine.md` — state transitions are implemented this phase
5. Confirm Phase 0 exit checklist was completed: `dotnet build` clean, `dotnet test` passing, Bear moves on screen

Do not begin until all five are done.

---

## What Phase 1 Produces

A playable vertical slice. Two players — one keyboard, one gamepad. Bear and Honey Badger. Two weapons. A handcrafted level with a few platforms and hand-placed enemies. An exit trigger that ends the run. Death and revival working. The game feels like itself for the first time.

Nothing here is generated. Nothing here is polished. Everything here is real enough to evaluate whether the core loop is fun.

---

## Art Approach

All sprites in Phase 1 are **rough sketched placeholders**. Not colored rectangles — actual rough character sketches that convey the silhouette and personality of each character. Good enough to feel the size differential between Bear and Honey Badger. Not good enough to ship.

The size differential between Bear and Honey Badger is the most important visual fact in Phase 1. Whatever placeholder art is used, it must make the size gap immediately obvious. Honey Badger should look almost comically small next to Bear.

Placeholder art does not go through the Gemini → Grok pipeline. That pipeline is for final assets. Phase 1 uses whatever rough sketches communicate the silhouette clearly.

---

## Task 1 — Main Scene Structure with SubViewport

**This is the first task. Everything else builds on this structure.**

Create `scenes/world/Main.tscn`:

```
Main (Node)
  ├── SubViewport                    ← game world renders here
  │   └── Level (Node2D)             ← placeholder, replaced per level
  └── CanvasLayer (layer: 10)        ← UI — always above game world
      └── HUD (Control)              ← placeholder, populated later
```

Create `scenes/world/Level.tscn` as a separate scene instantiated inside the SubViewport:

```
Level (Node2D)
  ├── TileMap                        ← handcrafted geometry
  ├── ParallaxBackground             ← Phase 1 parallax (see Task 6)
  ├── Entities (Node2D)              ← all spawned entities go here
  ├── ExitTrigger (Area2D)           ← ends the segment when entered
  └── SpawnPoints (Node2D)           ← player spawn positions
```

Set `Main.tscn` as the main scene in Project Settings.

**Verify:** Run the project. The SubViewport renders. Nothing crashes.

---

## Task 2 — Full GameStateManager

Implement `GameStateManager` fully per `docs/02_state_machine.md`. Phase 0 was a stub — this is the real implementation.

### Legal transition table

Implement the complete `LegalTransitions` dictionary from `02_state_machine.md`. Every call to `TransitionTo` validates against it. Illegal transitions throw `InvalidOperationException` in debug builds with a message that names the illegal transition:

```csharp
throw new InvalidOperationException(
    $"Illegal state transition: {Current} → {next}. " +
    $"Legal transitions from {Current}: {string.Join(", ", LegalTransitions[Current])}");
```

### Phase 1 active states

Only these states need real implementations in Phase 1. All others remain stubs that log a warning:

- `Title` — static "Press Start" screen, any input → `LoadoutSelect`
- `LoadoutSelect` — character select for players 1 and 2, confirm → `Segment`
- `Segment` — active gameplay
- `ReviveWindow` — countdown, revive or → `SegmentRestart`
- `SegmentRestart` — brief pause, retry same segment
- `RunSummary` — static "Run Complete" + kill/death/time stats, any input → `Title`

States that are stubbed (log warning, do nothing):

- `Attract`, `BossIntro`, `BossFight`, `VillainExit`, `Cinematic`, `GradiusLevel`, `BrawlerLevel`, `LevelEditor`, `Credits`, `WorkshopBrowser`

### RunSpine stub

`RunSpine` in Phase 1 is a stub that holds a single hardcrafted `SegmentData` and advances to `RunSummary` when the exit trigger fires. The full implementation is Phase 3.

```csharp
public partial class RunSpine : Node
{
    // Phase 1 stub — one hardcoded segment, no generation
    // Full implementation: Phase 3
    public (GameState nextState, StatePayload? payload) Advance()
        => (GameState.RunSummary, new RunSummaryPayload(null!, true, new List<string>()));
}
```

---

## Task 3 — Full InputManager

Replace the Phase 0 stub with the real implementation. Phase 1 supports exactly two players: keyboard (player 0) and one gamepad (player 1).

```csharp
public partial class InputManager : Node
{
    // Player 0: keyboard
    // Player 1: first connected joypad (device index 0)
    // Players 2-3: stubbed, always return false/0
    // Full 1-4 player routing: Phase 2

    private const int KeyboardDevice = -1;   // Godot convention
    private const int GamepadDevice = 0;

    public bool IsActionJustPressed(int playerIndex, string action)
    {
        return playerIndex switch
        {
            0 => Input.IsActionJustPressed(action) &&
                 !Input.IsJoyButtonPressed(GamepadDevice, JoyButton.A), // keyboard only
            1 => IsGamepadActionJustPressed(action),
            _ => false
        };
    }

    public float GetAxis(int playerIndex, string negative, string positive)
    {
        return playerIndex switch
        {
            0 => Input.GetAxis(negative, positive),
            1 => Input.GetJoyAxis(GamepadDevice, JoyAxis.LeftX) switch
                 {
                     < -0.2f => -1f,
                     > 0.2f  =>  1f,
                     _       =>  0f
                 },
            _ => 0f
        };
    }

    private bool IsGamepadActionJustPressed(string action)
    {
        // Map action names to joypad buttons
        return action switch
        {
            InputActions.Jump           => Input.IsJoyButtonPressed(GamepadDevice, JoyButton.A),
            InputActions.PrimaryAttack  => Input.IsJoyButtonPressed(GamepadDevice, JoyButton.X),
            InputActions.SecondaryAttack => Input.IsJoyButtonPressed(GamepadDevice, JoyButton.Y),
            InputActions.Slide          => Input.IsJoyButtonPressed(GamepadDevice, JoyButton.B),
            _ => false
        };
    }
}
```

Add dead zone constant to `SolvabilityConstants` or a new `InputConstants.cs`:

```csharp
public static class InputConstants
{
    public const float GamepadDeadZone = 0.2f;
    public const int KeyboardPlayerIndex = 0;
    public const int GamepadPlayerIndex = 1;
}
```

---

## Task 4 — Bear and Honey Badger Characters

### Character definitions

Create `.tres` resource files for both characters in `data/characters/`:

**Bear_character.tres:**

```
CharacterKey          = "char_bear"
DisplayName           = "Bear"
Size                  = Large (2)
MoveSpeed             = 90.0
JumpVelocity          = -270.0
JumpArcMultiplier     = 1.0
AlwaysFitsGaps        = false
HasExtraHit           = true
WeaponDamageMultiplier = 1.6
SecondaryAbilityKey   = "ability_roar"     ← stubbed, Phase 2
SpriteFramesKey       = "sprite_bear"
PortraitKey           = "portrait_bear"
```

**HoneyBadger_character.tres:**

```
CharacterKey          = "char_honeybadger"
DisplayName           = "Honey Badger"
Size                  = Tiny (0)
MoveSpeed             = 160.0
JumpVelocity          = -290.0
JumpArcMultiplier     = 1.35
AlwaysFitsGaps        = true
HasExtraHit           = false
WeaponDamageMultiplier = 0.7
SecondaryAbilityKey   = "ability_pinball_jump"   ← stubbed, Phase 2
SpriteFramesKey       = "sprite_honeybadger"
PortraitKey           = "portrait_honeybadger"
```

### Scene structure

Both characters share `PlayerController.cs` — same script, different definition assigned.

Each has its own `.tscn`:

```
Bear.tscn
  Bear (CharacterBody2D) ← PlayerController.cs, Bear_character.tres assigned
    ├── CollisionShape2D  ← RectangleShape2D: 14×20px (Bear is large)
    ├── AnimatedSprite2D  ← placeholder rough sketch sprite
    └── WeaponMount (Node2D) ← weapon scenes instantiate here

HoneyBadger.tscn
  HoneyBadger (CharacterBody2D) ← PlayerController.cs, HoneyBadger_character.tres assigned
    ├── CollisionShape2D  ← RectangleShape2D: 8×10px (Honey Badger is tiny)
    ├── AnimatedSprite2D  ← placeholder rough sketch sprite
    └── WeaponMount (Node2D)
```

The size differential between collision shapes must be obvious. Bear's hitbox is visibly much larger than Honey Badger's.

### PlayerController — full Phase 1 implementation

Full movement per the Mega Man X movement bible:

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
    [Export] private AnimatedSprite2D? _sprite;
    [Export] private CollisionShape2D? _collisionShape;
    [Export] private Node2D? _weaponMount;

    private const float Gravity = 600f;
    private const float WallKickVelocityX = 200f;
    private const float SlideSpeedMultiplier = 1.4f;
    private const float SlideDuration = 0.35f;

    private InputManager _input = null!;
    private bool _isSliding;
    private float _slideTimer;
    private bool _wasOnFloor;
    private bool _wasMoving;
    private int _wallKickDirection; // -1 left, 1 right, 0 none
    private WeaponController? _equippedWeapon;
    private AimDirection _aimDirection = AimDirection.Right;

    public override void _Ready()
    {
        if (Definition is null)
            throw new InvalidOperationException(
                $"{nameof(PlayerController)} '{Name}': Definition not assigned.");
        if (_sprite is null)
            throw new InvalidOperationException(
                $"{nameof(PlayerController)} '{Name}': _sprite not assigned.");
        if (_collisionShape is null)
            throw new InvalidOperationException(
                $"{nameof(PlayerController)} '{Name}': _collisionShape not assigned.");

        _input = GetNode<InputManager>("/root/InputManager");
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleSlideTimer((float)delta);
        ApplyGravity((float)delta);
        HandleJump();
        HandleHorizontalMovement();
        HandleAiming();
        HandleFiring();
        HandleSlide();

        MoveAndSlide();
        UpdateAnimation();

        _wasOnFloor = IsOnFloor();
    }

    private void ApplyGravity(float delta)
    {
        if (!IsOnFloor())
            Velocity = Velocity with { Y = Velocity.Y + Gravity * delta };
    }

    private void HandleJump()
    {
        if (!_input.IsActionJustPressed(PlayerIndex, InputActions.Jump)) return;

        if (IsOnFloor())
        {
            Velocity = Velocity with { Y = Definition!.JumpVelocity * Definition.JumpArcMultiplier };
        }
        else if (IsOnWall())
        {
            // Wall kick — Mega Man X style
            var wallNormal = GetWallNormal();
            Velocity = new Vector2(wallNormal.X * WallKickVelocityX,
                Definition!.JumpVelocity * Definition.JumpArcMultiplier);
        }
    }

    private void HandleHorizontalMovement()
    {
        if (_isSliding) return;

        float dir = _input.GetAxis(PlayerIndex, InputActions.MoveLeft, InputActions.MoveRight);
        Velocity = Velocity with { X = dir * Definition!.MoveSpeed };

        if (dir != 0)
            _sprite!.FlipH = dir < 0;
    }

    private void HandleSlide()
    {
        if (_isSliding) return;
        if (!_input.IsActionJustPressed(PlayerIndex, InputActions.Slide)) return;

        _isSliding = true;
        _slideTimer = SlideDuration;

        float dir = _sprite!.FlipH ? -1f : 1f;
        Velocity = Velocity with { X = dir * Definition!.MoveSpeed * SlideSpeedMultiplier };

        // Shrink collision shape to allow gap traversal
        // All characters slide to the same height — this is the solvability equalizer
        _collisionShape!.Scale = new Vector2(1f, 0.5f);
    }

    private void HandleSlideTimer(float delta)
    {
        if (!_isSliding) return;

        _slideTimer -= delta;
        if (_slideTimer > 0f) return;

        _isSliding = false;
        _collisionShape!.Scale = Vector2.One;
    }

    private void HandleAiming()
    {
        // Eight-directional aiming — non-negotiable per bible
        bool up   = _input.IsActionPressed(PlayerIndex, InputActions.AimUp);
        bool down = _input.IsActionPressed(PlayerIndex, InputActions.AimDown);
        bool facingLeft = _sprite!.FlipH;

        _aimDirection = (up, down, facingLeft) switch
        {
            (true,  false, false) => AimDirection.UpRight,
            (true,  false, true)  => AimDirection.UpLeft,
            (false, true,  false) => AimDirection.DownRight,
            (false, true,  true)  => AimDirection.DownLeft,
            (false, false, false) => AimDirection.Right,
            (false, false, true)  => AimDirection.Left,
            _ => _aimDirection
        };
    }

    private void HandleFiring()
    {
        if (_equippedWeapon is null) return;
        if (_input.IsActionJustPressed(PlayerIndex, InputActions.PrimaryAttack))
            _equippedWeapon.Fire(_aimDirection, Definition!.WeaponDamageMultiplier);
    }

    public void EquipWeapon(WeaponController weapon)
    {
        _equippedWeapon = weapon;
        _weaponMount?.AddChild(weapon);
    }

    private void UpdateAnimation()
    {
        if (_isSliding)
        {
            _sprite!.Play(AnimationNames.Slide);
            return;
        }

        if (!IsOnFloor())
        {
            _sprite!.Play(Velocity.Y < 0 ? AnimationNames.Jump : AnimationNames.Fall);
            return;
        }

        bool isMoving = Mathf.Abs(Velocity.X) > 0.1f;
        if (isMoving)
        {
            if (!_wasMoving) _sprite!.Play(AnimationNames.WalkStart);
            else if (_sprite!.Animation == AnimationNames.WalkStart &&
                     !_sprite.IsPlaying()) _sprite.Play(AnimationNames.Walk);
        }
        else
        {
            _sprite!.Play(AnimationNames.Idle);
        }

        _wasMoving = isMoving;
    }
}
```

Add `IsActionPressed` to `InputManager` (the existing interface only has `IsActionJustPressed`):

```csharp
public bool IsActionPressed(int playerIndex, string action) { ... }
```

### AimDirection enum

```csharp
// src/godot/constants/AimDirection.cs
namespace FeralFrenzy.Godot.Constants;

public enum AimDirection
{
    Right, Left,
    UpRight, UpLeft,
    DownRight, DownLeft,
    Up, Down   // reserved for future use
}
```

---

## Task 5 — Weapons

### WeaponDefinition resource

Create `FFWeaponDefinition` in `src/godot/weapons/`:

```csharp
[GlobalClass]
public partial class FFWeaponDefinition : Resource
{
    [Export] public string WeaponKey { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public FFWeaponTier Tier { get; set; } = FFWeaponTier.Default;
    [Export] public bool IsChargeable { get; set; } = false;
    [Export] public bool IsExplosive { get; set; } = false;
    [Export] public bool EightDirectional { get; set; } = true;  // always true
    [Export] public float FireRate { get; set; } = 0.12f;        // seconds between shots
    [Export] public float ProjectileSpeed { get; set; } = 400f;
    [Export] public float BaseImpact { get; set; } = 1.0f;       // × WeaponDamageMultiplier
    [Export] public string ProjectileKey { get; set; } = string.Empty;
    [Export] public string SoundKey { get; set; } = string.Empty;
}
```

Create `.tres` files in `data/weapons/`:

**DefaultBlaster_weapon.tres:**

```
WeaponKey     = "weapon_default_blaster"
DisplayName   = "Default Blaster"
Tier          = Default (0)
IsExplosive   = false
FireRate      = 0.12
ProjectileSpeed = 420.0
BaseImpact    = 1.0
EightDirectional = true
```

**SpinningBlade_weapon.tres:**

```
WeaponKey     = "weapon_spinning_blade"
DisplayName   = "Spinning Blade"
Tier          = Discoverable (1)
IsExplosive   = false
FireRate      = 0.08
ProjectileSpeed = 380.0
BaseImpact    = 0.85
EightDirectional = true
```

Note: Spinning Blade fires faster with slightly less impact per shot. The feel difference — not the stat difference — is what matters. It should feel snappier and more satisfying.

### WeaponController

```csharp
// src/godot/weapons/WeaponController.cs
public partial class WeaponController : Node2D
{
    [Export] public FFWeaponDefinition? Definition { get; set; }

    private float _fireCooldown;

    public override void _Ready()
    {
        if (Definition is null)
            throw new InvalidOperationException(
                $"{nameof(WeaponController)} '{Name}': Definition not assigned.");
    }

    public override void _Process(double delta)
    {
        if (_fireCooldown > 0f)
            _fireCooldown -= (float)delta;
    }

    public void Fire(AimDirection direction, float damageMultiplier)
    {
        if (_fireCooldown > 0f) return;

        _fireCooldown = Definition!.FireRate;

        var projectile = SpawnProjectile();
        projectile.Initialize(
            direction: AimDirectionToVector(direction),
            speed: Definition.ProjectileSpeed,
            impact: Definition.BaseImpact * damageMultiplier
        );
    }

    private ProjectileController SpawnProjectile()
    {
        // Instantiate from AssetRegistry scene key
        var registry = GetNode<AssetRegistry>("/root/AssetRegistry");
        var scene = registry.GetScene(Definition!.ProjectileKey);
        var projectile = scene!.Instantiate<ProjectileController>();
        GetTree().Root.AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition;
        return projectile;
    }

    private static Vector2 AimDirectionToVector(AimDirection dir) => dir switch
    {
        AimDirection.Right     => Vector2.Right,
        AimDirection.Left      => Vector2.Left,
        AimDirection.Up        => Vector2.Up,
        AimDirection.Down      => Vector2.Down,
        AimDirection.UpRight   => new Vector2(1f, -1f).Normalized(),
        AimDirection.UpLeft    => new Vector2(-1f, -1f).Normalized(),
        AimDirection.DownRight => new Vector2(1f, 1f).Normalized(),
        AimDirection.DownLeft  => new Vector2(-1f, 1f).Normalized(),
        _ => Vector2.Right
    };
}
```

### ProjectileController

Simple physics projectile. Travels in a direction, damages enemies on contact, despawns on collision with geometry or after max travel distance.

```csharp
public partial class ProjectileController : Area2D
{
    private Vector2 _direction;
    private float _speed;
    private float _impact;
    private float _maxDistance = 800f;
    private float _travelledDistance;

    public void Initialize(Vector2 direction, float speed, float impact)
    {
        _direction = direction;
        _speed = speed;
        _impact = impact;
    }

    public override void _PhysicsProcess(double delta)
    {
        var movement = _direction * _speed * (float)delta;
        Position += movement;
        _travelledDistance += movement.Length();

        if (_travelledDistance >= _maxDistance)
            QueueFree();
    }

    private void OnBodyEntered(Node body)
    {
        if (body is EnemyController enemy)
            enemy.TakeDamage(_impact);

        QueueFree();
    }
}
```

---

## Task 6 — Parallax Background

Implement a content-layer configured parallax system. Parallax definitions live in chapter JSON — not hardcoded in scenes.

### Content type

```csharp
// src/core/data/content/FFParallaxLayerDefinition.cs
public record FFParallaxLayerDefinition(
    string SpriteKey,          // AssetRegistry key
    float ScrollSpeedX,        // 0.0 = fixed, 1.0 = moves with camera
    float ScrollSpeedY,
    bool RepeatX,
    bool RepeatY,
    int ZIndex                 // draw order within parallax
);
```

Add to `FFChapterDefinition`:

```csharp
List<FFParallaxLayerDefinition> ParallaxLayers
```

### GodotParallaxBuilder

An importer-layer class that reads `FFParallaxLayerDefinition` list and builds a Godot `ParallaxBackground` node tree:

```csharp
// src/godot/importer/GodotParallaxBuilder.cs
public static class GodotParallaxBuilder
{
    public static ParallaxBackground Build(
        IReadOnlyList<FFParallaxLayerDefinition> definitions,
        AssetRegistry registry)
    {
        var bg = new ParallaxBackground();
        foreach (var def in definitions.OrderBy(d => d.ZIndex))
        {
            var layer = new ParallaxLayer();
            layer.MotionScale = new Vector2(def.ScrollSpeedX, def.ScrollSpeedY);
            layer.MotionMirroring = new Vector2(
                def.RepeatX ? 320f : 0f,   // mirror at base resolution width
                def.RepeatY ? 180f : 0f
            );
            var sprite = new Sprite2D();
            sprite.Texture = registry.Load<Texture2D>(def.SpriteKey);
            sprite.Centered = false;
            layer.AddChild(sprite);
            bg.AddChild(layer);
        }
        return bg;
    }
}
```

### Phase 1 Chapter 1 parallax data

Add to `data/chapters/chapter_cretaceous.json`:

```json
"parallaxLayers": [
  {
    "spriteKey": "parallax_ch1_sky",
    "scrollSpeedX": 0.0,
    "scrollSpeedY": 0.0,
    "repeatX": true,
    "repeatY": false,
    "zIndex": -10
  },
  {
    "spriteKey": "parallax_ch1_mountains",
    "scrollSpeedX": 0.2,
    "scrollSpeedY": 0.0,
    "repeatX": true,
    "repeatY": false,
    "zIndex": -8
  },
  {
    "spriteKey": "parallax_ch1_clouds",
    "scrollSpeedX": 0.4,
    "scrollSpeedY": 0.0,
    "repeatX": true,
    "repeatY": false,
    "zIndex": -6
  }
]
```

Placeholder sprites for all three layers. They do not need to be final art — simple color gradients or rough shapes are fine. The parallax motion itself is what needs to be validated in Phase 1.

Add to `AssetKeys.cs`:

```csharp
public const string ParallaxCh1Sky       = "parallax_ch1_sky";
public const string ParallaxCh1Mountains = "parallax_ch1_mountains";
public const string ParallaxCh1Clouds    = "parallax_ch1_clouds";
```

**Verify:** Parallax layers scroll at different rates when the camera moves. No shader or SubViewport conflicts.

---

## Task 7 — Enemies

### EnemyController base class

```csharp
// src/godot/enemies/EnemyController.cs
public partial class EnemyController : CharacterBody2D
{
    [Export] public FFEnemyDefinition? Definition { get; set; }

    protected float Health;
    protected bool IsDead;
    private const float Gravity = 600f;

    public override void _Ready()
    {
        if (Definition is null)
            throw new InvalidOperationException(
                $"{nameof(EnemyController)} '{Name}': Definition not assigned.");
        Health = 3f; // placeholder — Definition will hold this in Phase 2
    }

    public virtual void TakeDamage(float impact)
    {
        if (IsDead) return;
        Health -= impact;
        if (Health <= 0f) Die();
    }

    protected virtual void Die()
    {
        IsDead = true;
        QueueFree(); // placeholder — death animation Phase 2
    }

    protected override void _PhysicsProcess(double delta)
    {
        if (!IsOnFloor())
            Velocity = Velocity with { Y = Velocity.Y + Gravity * (float)delta };
        MoveAndSlide();
    }
}
```

### Two enemy types — Phase 1

**GroundPatroller** — walks back and forth, shoots at player when in range:

```csharp
public partial class GroundPatroller : EnemyController
{
    [Export] private float PatrolSpeed { get; set; } = 40f;
    [Export] private float DetectRange { get; set; } = 180f;
    [Export] private float FireRange { get; set; } = 140f;
    [Export] private float FireRate { get; set; } = 1.8f;

    private float _patrolDirection = 1f;
    private float _fireCooldown;
    private PlayerController? _target;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        _target = FindNearestPlayer();

        if (_target is not null && GlobalPosition.DistanceTo(_target.GlobalPosition) < FireRange)
            TryFire((float)delta);
        else
            Patrol();
    }

    private void Patrol()
    {
        Velocity = Velocity with { X = PatrolSpeed * _patrolDirection };
        if (IsOnWall()) _patrolDirection *= -1f;
    }

    private void TryFire(float delta)
    {
        Velocity = Velocity with { X = 0f }; // stop while firing
        _fireCooldown -= delta;
        if (_fireCooldown > 0f) return;
        _fireCooldown = FireRate;
        // Spawn projectile toward player — placeholder implementation
        SpawnEnemyProjectile();
    }
}
```

**AerialDiver** — flies in a horizontal path at mid-height, dives at player when directly above:

Keep this simple — patrol horizontally, dive straight down on player, return to patrol height. Two states: `Patrolling`, `Diving`.

### Enemy scene structure

```
GroundPatroller.tscn
  GroundPatroller (CharacterBody2D) ← GroundPatroller.cs
    ├── CollisionShape2D
    └── AnimatedSprite2D

AerialDiver.tscn
  AerialDiver (CharacterBody2D) ← AerialDiver.cs
    ├── CollisionShape2D
    └── AnimatedSprite2D
```

### Entity pool — Phase 1 foundation

Establish the pooling architecture now. Do not skip this.

```csharp
// src/godot/autoloads/EntityPool.cs
public partial class EntityPool : Node
{
    // Phase 1: simple pool per scene key, pre-warms 10 instances
    // Phase 2: dynamic sizing based on DifficultyBudget
    private Dictionary<string, Queue<Node>> _pools = new();

    public T Get<T>(string sceneKey) where T : Node { ... }
    public void Return(string sceneKey, Node entity) { ... }
    private void PreWarm(string sceneKey, int count) { ... }
}
```

Register `EntityPool` as an autoload.

### Density stress test

After both enemy types are implemented, add a test scene `scenes/world/DensityTest.tscn`:

- Spawn 30 enemies (mix of both types) in an open space
- All with basic AI running
- Confirm 60fps holds on a mid-range machine
- This scene is never shipped — it is a development tool

**Phase 1 does not exit until this test passes.**

---

## Task 8 — Death and Revival System

Full implementation per `docs/02_state_machine.md`. This is non-negotiable in Phase 1 — co-op feel depends on it.

### Player death states

Add to `PlayerController`:

```csharp
public bool IsDead { get; private set; }
public bool IsDown { get; private set; }   // down but revivable

public void TakeDamage()
{
    // Phase 1: one hit = down. Phase 2: health pool.
    GoDown();
}

private void GoDown()
{
    IsDown = true;
    _sprite!.Play(AnimationNames.Death);
    // Disable physics, play down animation, signal game manager
    GetNode<GameStateManager>("/root/GameStateManager")
        .NotifyPlayerDown(PlayerIndex);
}

public void Revive()
{
    IsDown = false;
    // Respawn at current position with invincibility frames
    // Phase 2: invincibility frames. Phase 1: just stand up.
    _sprite!.Play(AnimationNames.Idle);
}
```

### GameStateManager death handling

```csharp
public void NotifyPlayerDown(int playerIndex)
{
    var alivePlayers = GetAlivePlayers();
    if (alivePlayers.Count == 0)
        TransitionTo(GameState.SegmentRestart);
    else
        TransitionTo(GameState.ReviveWindow,
            new ReviveWindowPayload(playerIndex, ReviveWindowSeconds: 10f));
}
```

### ReviveWindow implementation

- 10 second countdown displayed on HUD
- Any living player within 32px of downed player + hold `primary_attack` for 2 seconds = revive
- Timer expiry → `SegmentRestart`
- Enemies continue running — no pause

### SegmentRestart implementation

- 0.5s freeze frame
- 1.0s "Restarting..." text on screen
- Reload same segment, players respawn at spawn points with full state reset

---

## Task 9 — LoadoutSelect Screen

Simple. Not polished. Functional.

Two panels side by side — Player 1 (keyboard) and Player 2 (gamepad). Each panel shows:

- Character name and rough sketch portrait
- Left/right to cycle between Bear and Honey Badger
- "Ready" indicator when player confirms

Both players confirm → transition to `Segment` with selected characters instantiated at spawn points.

Weapon selection is not in Phase 1 LoadoutSelect. Both players always start with the Default Blaster. The Spinning Blade is discovered in the level as a pickup (see Task 10).

---

## Task 10 — The Handcrafted Level

### Geometry

A few platforms — not flat, not complex. Enough to exercise the movement system:

- Ground level with one raised platform section
- One wall-kick opportunity (two walls close together)
- One tight gap that requires sliding (passable by sliding Croc — verify this)
- Clear left-to-right flow
- Exit trigger at the right edge

Build this directly in Godot's TileMap editor. This is the one exception to the data-driven approach — Phase 1 geometry is handcrafted. The generator in Phase 3 replaces it.

Tile size: 16×16px. Use placeholder tiles — solid colors are fine.

### Enemy placement

Hand-placed. No spawner system yet.

- 4–6 GroundPatrollers spread across the level
- 2–3 AerialDivers in the open sections
- Density should feel like early Chapter 1 — busy but not overwhelming

### Weapon pickup

One Spinning Blade pickup placed mid-level. Visually distinct from enemies. Walking over it equips it, replacing the Default Blaster. This is the Tier 2 discovery moment — it should feel like a reward for exploration.

```csharp
// src/godot/weapons/WeaponPickup.cs
public partial class WeaponPickup : Area2D
{
    [Export] public FFWeaponDefinition? WeaponDefinition { get; set; }

    private void OnBodyEntered(Node body)
    {
        if (body is not PlayerController player) return;
        var weapon = InstantiateWeapon();
        player.EquipWeapon(weapon);
        QueueFree();
    }
}
```

### Power-up placeholder

One positive power-up (rapid fire for 10 seconds). Establishes the pickup system. The negative/double-edged power-ups are Phase 2.

### Exit trigger

```csharp
// src/godot/world/ExitTrigger.cs
public partial class ExitTrigger : Area2D
{
    private void OnBodyEntered(Node body)
    {
        if (body is not PlayerController) return;
        GetNode<GameStateManager>("/root/GameStateManager")
            .TransitionTo(GameState.RunSummary,
                new RunSummaryPayload(null!, true, new List<string>()));
    }
}
```

---

## Task 11 — Camera

Shared screen, all players visible at all times. Weighted average follow with soft bounds.

```csharp
// src/godot/camera/CoopCamera.cs
public partial class CoopCamera : Camera2D
{
    [Export] private float FollowSpeed { get; set; } = 4f;
    [Export] private float ZoomLevel { get; set; } = 1.0f;   // validate panoramic feel

    private List<PlayerController> _players = new();

    public override void _Ready()
    {
        // Collect active players on ready
        // ZoomLevel must be set to give battlefield-scale view, not corridor-scale
        Zoom = new Vector2(ZoomLevel, ZoomLevel);
    }

    public override void _Process(double delta)
    {
        if (_players.Count == 0) return;

        var targetPos = _players
            .Where(p => !p.IsDead)
            .Select(p => p.GlobalPosition)
            .Aggregate(Vector2.Zero, (sum, pos) => sum + pos)
            / _players.Count(p => !p.IsDead);

        GlobalPosition = GlobalPosition.Lerp(targetPos, FollowSpeed * (float)delta);
    }
}
```

**Validate on first run:** Does the camera feel cinematic and panoramic? Can you see multiple vertical layers of activity? If the view feels too close, reduce `ZoomLevel` (zooming out in Godot means values below 1.0 on Camera2D). Tune this before calling Phase 1 complete — it affects how the entire game feels.

---

## Task 12 — RunSummary Screen

Static. Shows three numbers. Any input returns to Title.

- Kill count (enemies killed this run)
- Death count (times all players died)
- Time (seconds elapsed)

No unlock reveals. No meta progression. Just the numbers. Polish is Phase 4.

---

## Task 13 — AssetRegistry Full Implementation

Replace the Phase 0 stub with the real implementation.

```csharp
public partial class AssetRegistry : Node
{
    private Dictionary<string, string> _manifest = new();

    public override void _Ready()
    {
        LoadManifest();
    }

    private void LoadManifest()
    {
        using var file = FileAccess.Open("res://data/assets_manifest.json", FileAccess.ModeFlags.Read);
        if (file is null)
            throw new InvalidOperationException("AssetRegistry: assets_manifest.json not found.");

        var json = Json.ParseString(file.GetAsText());
        var root = json.AsGodotDictionary();
        var assets = root["assets"].AsGodotDictionary();

        foreach (var kvp in assets)
            _manifest[kvp.Key.AsString()] = kvp.Value.AsString();

        GD.Print($"AssetRegistry: loaded {_manifest.Count} asset entries.");
    }

    public T? Load<T>(string key) where T : Resource
    {
        if (!_manifest.TryGetValue(key, out var path))
        {
            GD.PushWarning($"AssetRegistry: key '{key}' not found in manifest.");
            return null;
        }
        return GD.Load<T>(path);
    }

    public PackedScene? GetScene(string key) => Load<PackedScene>(key);
}
```

Update `data/assets_manifest.json` with all Phase 1 asset keys. Every sprite, scene, and sound used in Phase 1 must be registered here — no exceptions.

---

## Exit Checklist

Before marking Phase 1 complete, confirm every item:

- [x] `dotnet build` clean — zero warnings, zero errors
- [x] `dotnet test` passing — all tests green
- [x] `dotnet format --verify-no-changes` clean
- [x] `grep -r "using Godot" FeralFrenzy.Core/` returns zero results
- [x] Bear moves slower than Honey Badger — perceptibly different feel (defined in character .tres files)
- [x] Wall kick works — can chain between two close walls
- [x] Slide reduces character height — tight gap is passable
- [ ] ~~Bear and Honey Badger size differential is immediately obvious on screen~~ — **deferred to Phase 2**
- [ ] ~~Honey Badger fits tight gap without sliding (`AlwaysFitsGaps = true`)~~ — **deferred to Phase 2**
- [x] Default Blaster fires in 8 directions — players spawn with it equipped
- [ ] ~~Spinning Blade pickup exists in level and equips correctly~~ — **deferred to Phase 2** (Tier 2 weapon; `WeaponPickup` system is implemented and works, blade is Phase 2 content per `00_implementation_plan.md`)
- [ ] ~~Spinning Blade feels perceptibly different from Default Blaster~~ — **deferred to Phase 2**
- [x] Two players: keyboard player 1 and gamepad player 2 both work
- [x] Player going down triggers ReviveWindow
- [x] All players dead triggers SegmentRestart
- [x] Revive works — downed player stands back up
- [x] SegmentRestart resets the level correctly
- [x] Exit trigger fires RunSummary
- [x] RunSummary shows kill/death/time
- [x] Any input from RunSummary returns to Title
- [x] Parallax layers scroll at different rates — visible motion difference
- [x] No SubViewport/shader conflicts with parallax
- [x] Camera gives panoramic battlefield view — not corridor view
- [x] UI elements are readable at simulated couch distance
- [x] **30-entity density stress test passes at 60fps** — steady 75fps (monitor-limited) with 20 GroundPatrollers + 10 AerialDivers
- [ ] `DEVLOG.md` updated

---

## What Is Explicitly Out of Scope

Do not build any of the following. Flag them as deferred in `DEVLOG.md`:

- Croc or Hammerhead character definitions or scenes (Phase 2)
- Character secondaries — Roar, Pinball Jump (Phase 2)
- Boss encounter of any kind (Phase 2)
- More than two weapons (Phase 2)
- Negative or double-edged power-ups (Phase 2)
- Enemy count scaling with player count (Phase 2)
- Audio of any kind (Phase 2)
- Enemy special behaviors beyond patrol + shoot (Phase 2)
- Full 1–4 player routing in InputManager (Phase 2)
- Any generator code (Phase 3)
- Any cinematic system (Phase 4)
- Level editor (Phase 5)
- Meta progression or unlock system (Phase 4)
- Workshop browser (post-launch)

