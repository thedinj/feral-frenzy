# Feral Frenzy — Phase 2 Task Brief

**Version:** 1.0  
**For:** Claude Code  
**Status:** Ready to execute  
**Exit condition:** Two players fight through the level with real combat weight, discover the Spinning Blade, encounter negative power-ups and hesitate, reach a placeholder boss, survive or die with meaningful HP, and complete the full state machine loop through VillainExit to RunSummary.

---

## Before You Start

1. Read `CLAUDE.md` completely — use **v2**, not v1
2. Read `docs/00_implementation_plan.md` — confirm you are in Phase 2
3. Read `docs/02_state_machine.md` — BossIntro, BossFight, VillainExit are implemented this phase
4. Read `DEVLOG.md` — understand exactly what Phase 1 built and what it deferred
5. Confirm Phase 1 exit checklist was completed

Do not begin until all five are done. State the current phase and exit condition before writing a single line of code.

---

## What Phase 2 Produces

Combat that has weight. Enemies that require effort to kill. A player that can take a hit. A weapon worth discovering. Power-ups worth hesitating over. A boss that tests the full state machine loop. The game starts feeling like a real run-and-gun.

No new characters. No secondaries. No audio. No generator. Everything here either fixes what exists or adds depth to it.

---

## Task Order

Execute in this order. Each task builds on the previous. Do not start Task 3 before Task 2 is compiling clean.

---

## Task 1 — Combat Foundation

This is the most important task in Phase 2. Nothing else is evaluable until combat has weight.

### Player HP pool

Players currently go down in one hit. Add a hit buffer.

Add to `FFCharacterDefinition`:

```csharp
[Export] public int MaxHp { get; set; } = 3;
[Export] public float InvincibilitySeconds { get; set; } = 1.2f;
```

Default values for Bear and Honey Badger:

| Character    | MaxHp | InvincibilitySeconds |
| ------------ | ----- | -------------------- |
| Bear         | 4     | 1.0                  |
| Honey Badger | 3     | 1.4                  |

Update `.tres` files accordingly.

Add to `PlayerController`:

```csharp
public int CurrentHp { get; private set; }
private float _invincibilityTimer;
private bool IsInvincible => _invincibilityTimer > 0f;

public override void _Ready()
{
    // existing _Ready code...
    CurrentHp = Definition!.MaxHp;
}

public void TakeDamage(int amount = 1)
{
    if (IsInvincible || IsDown || IsDead) return;

    CurrentHp -= amount;
    _invincibilityTimer = Definition!.InvincibilitySeconds;

    if (CurrentHp <= 0)
        GoDown();
    else
        PlayHitFlash(); // visual feedback — see below
}

private void PlayHitFlash()
{
    // Brief sprite flash to confirm damage — placeholder implementation
    // Phase 5: shader-based hit flash
    _sprite!.Modulate = new Color(1f, 0.3f, 0.3f);
    GetTree().CreateTimer(0.1f).Timeout += () => _sprite.Modulate = Colors.White;
}
```

Add invincibility timer to `TickTimers`:

```csharp
private void TickTimers(float delta)
{
    if (_jumpBufferTimer > 0f) _jumpBufferTimer -= delta;
    else _jumpRequested = false;

    if (_invincibilityTimer > 0f) _invincibilityTimer -= delta;

    if (_slideTimer > 0f) _slideTimer -= delta;
    else if (_isSliding) EndSlide();
}
```

During invincibility frames, flash the sprite to signal the state:

```csharp
// In _PhysicsProcess, after TickTimers:
_sprite!.Visible = !IsInvincible || (Engine.GetFramesDrawn() % 4 < 2);
```

### Enemy HP pool

Enemies currently die in one hit regardless of weapon. Replace with a data-driven HP system.

Add to `FFEnemyDefinition`:

```csharp
[Export] public float MaxHp { get; set; } = 3f;
[Export] public float HitStunSeconds { get; set; } = 0.15f;
```

Update enemy `.tres` files:

| Enemy           | MaxHp | HitStunSeconds |
| --------------- | ----- | -------------- |
| GroundPatroller | 3     | 0.15           |
| AerialDiver     | 2     | 0.10           |

Update `EnemyController`:

```csharp
protected float CurrentHp;
protected bool IsHitStunned;
protected float HitStunTimer;

public override void _Ready()
{
    // existing...
    CurrentHp = Definition!.MaxHp;
}

public virtual void TakeDamage(float impact)
{
    if (IsDead) return;

    CurrentHp -= impact;
    IsHitStunned = true;
    HitStunTimer = Definition!.HitStunSeconds;

    PlayHitFlash();

    if (CurrentHp <= 0f) Die();
}

private void PlayHitFlash()
{
    // Modulate-based flash — same pattern as PlayerController
    // Phase 5: shader-based
    var sprite = GetNodeOrNull<AnimatedSprite2D>(NodePaths.AnimatedSprite);
    if (sprite is null) return;
    sprite.Modulate = Colors.White * 2f; // over-bright flash
    GetTree().CreateTimer(0.08f).Timeout += () => sprite.Modulate = Colors.White;
}
```

Add hit stun to EnemyController's `_PhysicsProcess`:

```csharp
protected void TickHitStun(float delta)
{
    if (!IsHitStunned) return;
    HitStunTimer -= delta;
    if (HitStunTimer <= 0f) IsHitStunned = false;
}
```

Enemies do not move or shoot during hit stun.

### Death animations

Enemies currently `QueueFree()` instantly. Add a brief death animation.

```csharp
protected virtual void Die()
{
    IsDead = true;
    var sprite = GetNodeOrNull<AnimatedSprite2D>(NodePaths.AnimatedSprite);

    // Disable physics and AI immediately
    SetPhysicsProcess(false);
    SetProcess(false);

    // Play death animation if it exists, otherwise do a brief flash+fade
    if (sprite is not null && sprite.SpriteFrames?.HasAnimation(AnimationNames.Death) == true)
    {
        sprite.Play(AnimationNames.Death);
        sprite.AnimationFinished += () => QueueFree();
    }
    else
    {
        // Placeholder: scale up and fade out
        var tween = CreateTween();
        tween.TweenProperty(sprite ?? (Node)this, "modulate:a", 0f, 0.3f);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    // Notify GameStateManager for kill count
    GetNode<GameStateManager>("/root/GameStateManager").NotifyEnemyKilled();
}
```

Add `AnimationNames.Death = "death"` — already in constants, confirm it's there.

### Collision layer tuning

Phase 1 used collision layers for player/enemy/projectile separation but did not prevent friendly fire. Fix this.

Layer assignment:

```
Layer 1: World geometry (TileMap, StaticBody2D)
Layer 2: Players
Layer 3: Enemies
Layer 4: Player projectiles (hit enemies, not players)
Layer 5: Enemy projectiles (hit players, not enemies)
```

`ProjectileController` needs to know if it is a player or enemy projectile:

```csharp
public enum ProjectileOwner { Player, Enemy }

public void Initialize(Vector2 direction, float speed, float impact, ProjectileOwner owner)
{
    _direction = direction;
    _speed = speed;
    _impact = impact;
    _owner = owner;

    // Set collision mask based on owner
    CollisionMask = owner == ProjectileOwner.Player
        ? (uint)LayerMasks.Enemies   // player shots hit enemies
        : (uint)LayerMasks.Players;  // enemy shots hit players
}
```

```csharp
// src/godot/constants/LayerMasks.cs
public static class LayerMasks
{
    public const uint World    = 1 << 0;  // layer 1
    public const uint Players  = 1 << 1;  // layer 2
    public const uint Enemies  = 1 << 2;  // layer 3
    public const uint PlayerProjectiles = 1 << 3; // layer 4
    public const uint EnemyProjectiles  = 1 << 4; // layer 5
}
```

Update all `CharacterBody2D` and `Area2D` collision layers in `.tscn` files to match.

### Debug GD.Print cleanup

Remove all `GD.Print` debug calls from `GameStateManager` and `LevelController`. Replace any that are load-bearing (e.g. manifest load count) with `GD.PushWarning` or a debug-only guard:

```csharp
#if DEBUG
GD.Print($"AssetRegistry: loaded {_manifest.Count} asset entries.");
#endif
```

---

## Task 2 — Spinning Blade + Enemy Variety

### Spinning Blade — complete implementation

The Spinning Blade was deferred from Phase 1. `WeaponDefinition` and `.tres` already exist. Implement the projectile behavior that makes it feel different.

Spinning Blade projectile behavior:

- Travels in the fired direction at moderate speed
- Does NOT despawn on first contact — passes through up to 3 enemies before despawning
- Travels a fixed distance (slightly shorter than the blaster) then returns to the player
- On return: can hit enemies again (the return pass)
- Visual: rotates continuously during flight

```csharp
// SpinningBladeProjectile.cs — a separate projectile controller
public partial class SpinningBladeProjectile : Area2D
{
    private Vector2 _direction;
    private float _speed = 320f;
    private float _impact;
    private int _maxPenetrations = 3;
    private int _penetrationCount;
    private float _maxDistance = 600f;
    private float _travelledDistance;
    private bool _returning;
    private PlayerController? _owner;
    private const float RotationSpeed = 12f; // radians per second

    public void Initialize(Vector2 direction, float impact, PlayerController owner)
    {
        _direction = direction.Normalized();
        _impact = impact;
        _owner = owner;
    }

    public override void _PhysicsProcess(double delta)
    {
        var movement = _direction * _speed * (float)delta;
        Position += movement;
        Rotation += RotationSpeed * (float)delta;
        _travelledDistance += movement.Length();

        if (!_returning && _travelledDistance >= _maxDistance)
        {
            _returning = true;
            _direction = (_owner!.GlobalPosition - GlobalPosition).Normalized();
        }

        if (_returning && GlobalPosition.DistanceTo(_owner!.GlobalPosition) < 16f)
            QueueFree();
    }

    private void OnBodyEntered(Node body)
    {
        if (body is EnemyController enemy)
        {
            enemy.TakeDamage(_impact);
            _penetrationCount++;
            if (_penetrationCount >= _maxPenetrations)
                QueueFree();
        }
    }
}
```

The Spinning Blade pickup is already placed in the level from Phase 1. Confirm it equips correctly and the player can feel the difference within the first few seconds of use.

**Feel check:** After implementing, play with both weapons back to back. The blaster should feel precise and punchy. The Spinning Blade should feel satisfying and slightly chaotic — the return pass and penetration are the moment. If it doesn't feel meaningfully different, tune `_maxDistance`, `_speed`, or `_maxPenetrations` until it does.

### Enemy variety — 2 additional types

**MountedDino** — an enemy riding a dinosaur. The rider shoots at range; the mount charges forward when the player is close. Two-phase: kill the rider first (3 HP), then the dino panics and charges erratically (2 HP). Counts as 2 kills.

```csharp
public partial class MountedDino : EnemyController
{
    private enum MountedDinoState { RiderActive, DinoCharging }
    private MountedDinoState _state = MountedDinoState.RiderActive;

    [Export] private float RiderHp { get; set; } = 3f;
    [Export] private float DinoHp { get; set; } = 2f;
    [Export] private float ChargeSpeed { get; set; } = 140f;
    [Export] private float ShootRange { get; set; } = 200f;

    public override void TakeDamage(float impact)
    {
        if (_state == MountedDinoState.RiderActive)
        {
            RiderHp -= impact;
            PlayHitFlash();
            if (RiderHp <= 0f)
            {
                _state = MountedDinoState.DinoCharging;
                CurrentHp = DinoHp;
                GetNode<GameStateManager>("/root/GameStateManager").NotifyEnemyKilled(); // rider kill
            }
        }
        else
        {
            base.TakeDamage(impact); // dino kill handled by base Die()
        }
    }
}
```

**PteroBomber** — flies overhead in a slow arc, drops a projectile directly below when over the player. Does not chase. Requires the player to pay attention to the sky.

```csharp
public partial class PteroBomber : EnemyController
{
    private float _patrolSpeed = 60f;
    private float _dropCooldown = 3f;
    private float _dropTimer;

    public override void _PhysicsProcess(double delta)
    {
        // No gravity — floats
        Velocity = new Vector2(_patrolSpeed, 0f);
        if (IsOnWall()) _patrolSpeed *= -1f;
        MoveAndSlide();

        _dropTimer -= (float)delta;
        if (_dropTimer <= 0f && IsOverPlayer())
        {
            DropBomb();
            _dropTimer = _dropCooldown;
        }
    }

    private bool IsOverPlayer()
    {
        var nearest = FindNearestPlayer();
        if (nearest is null) return false;
        return Mathf.Abs(nearest.GlobalPosition.X - GlobalPosition.X) < 24f;
    }

    private void DropBomb()
    {
        // Spawn enemy projectile falling straight down
    }
}
```

Create `.tres` files for both:

- `MountedDino_enemy.tres` — DifficultyWeight: 0.25 (expensive — counts as 2)
- `PteroBomber_enemy.tres` — DifficultyWeight: 0.15

Add both to `chapter_cretaceous.json` enemy pool.

Update `AssetKeys.cs` and `assets_manifest.json` with new scene keys.

### Enemy count scaling with player count

`EnemyController` base already stores `Definition.BaseCountPerPlayer`. Wire this into `LevelController`'s enemy spawner.

```csharp
// In LevelController, when spawning enemies from the level:
private int GetSpawnCount(FFEnemyDefinition definition)
    => definition.BaseCountPerPlayer * GameStateManager.ActivePlayerCount;
```

`GameStateManager` needs:

```csharp
public int ActivePlayerCount { get; private set; } = 1;

// Set during LoadoutSelect when players confirm characters
public void SetActivePlayerCount(int count) => ActivePlayerCount = count;
```

Two players should feel noticeably busier than one player. Three players should feel like controlled chaos. Tune `BaseCountPerPlayer` per enemy type:

| Enemy           | BaseCountPerPlayer |
| --------------- | ------------------ |
| GroundPatroller | 2                  |
| AerialDiver     | 1                  |
| MountedDino     | 1                  |
| PteroBomber     | 1                  |

---

## Task 3 — Power-Up Depth

### Negative and double-edged power-ups

Phase 1 had one positive power-up (rapid fire). Add negative and double-edged variants. The design goal is hesitation — players should sometimes pause before grabbing.

Add `FFPowerUpDefinition` to Core (it's referenced in `01_schema.md` but not yet implemented):

```csharp
// src/core/data/content/FFPowerUpDefinition.cs
public record FFPowerUpDefinition(
    string PowerUpKey,
    string DisplayName,
    RewardNodeType Type,
    bool AffectsDestructibleBalance,
    string EffectKey,
    string SpriteKey,
    string SoundKey
);
```

Implement these power-up effects in `PowerUp.cs`:

**Positive (already exists — extend):**

- Rapid Fire (existing) — fire rate ×2 for 10 seconds
- Damage Up — base impact ×1.5 for 10 seconds
- Speed Up — MoveSpeed ×1.3 for 10 seconds
- HP Restore — restore 1 HP (capped at MaxHp)

**Negative:**

- Reverse Controls — left/right inverted for 8 seconds. Visually signal with a flipped sprite or color tint. The player knows immediately but it's too late.
- Slow — MoveSpeed ×0.5 for 8 seconds. Brutal in a crowded room.

**Double-edged:**

- Berserker — damage ×2 but take double damage for 12 seconds. Great for skilled players, death sentence for a low-HP player.
- Magnet — automatically collects all pickups in range. Good if pickups are positive, dangerous if they're negative.

```csharp
// PowerUp.cs — effect dispatcher
private void ApplyEffect(PlayerController player)
{
    switch (Definition!.EffectKey)
    {
        case PowerUpEffects.RapidFire:
            player.ApplyStatusEffect(new RapidFireEffect(duration: 10f));
            break;
        case PowerUpEffects.ReverseControls:
            player.ApplyStatusEffect(new ReverseControlsEffect(duration: 8f));
            break;
        case PowerUpEffects.Berserker:
            player.ApplyStatusEffect(new BerserkerEffect(duration: 12f));
            break;
        // etc.
    }
}
```

```csharp
// src/godot/constants/PowerUpEffects.cs
public static class PowerUpEffects
{
    public const string RapidFire       = "effect_rapid_fire";
    public const string DamageUp        = "effect_damage_up";
    public const string SpeedUp         = "effect_speed_up";
    public const string HpRestore       = "effect_hp_restore";
    public const string ReverseControls = "effect_reverse_controls";
    public const string Slow            = "effect_slow";
    public const string Berserker       = "effect_berserker";
    public const string Magnet          = "effect_magnet";
}
```

Add a `StatusEffectController` component to `PlayerController` that holds active effects, ticks them down, and applies/removes their stat modifiers:

```csharp
// src/godot/characters/StatusEffectController.cs
public partial class StatusEffectController : Node
{
    private readonly List<StatusEffect> _activeEffects = new();

    public void Apply(StatusEffect effect) => _activeEffects.Add(effect);

    public void Tick(float delta)
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            _activeEffects[i].Tick(delta);
            if (_activeEffects[i].IsExpired)
            {
                _activeEffects[i].OnRemove();
                _activeEffects.RemoveAt(i);
            }
        }
    }

    public float GetDamageMultiplier()
        => _activeEffects.OfType<IDamageModifier>()
            .Aggregate(1f, (acc, e) => acc * e.DamageMultiplier);

    public float GetSpeedMultiplier()
        => _activeEffects.OfType<ISpeedModifier>()
            .Aggregate(1f, (acc, e) => acc * e.SpeedMultiplier);

    public bool AreControlsReversed()
        => _activeEffects.OfType<ReverseControlsEffect>().Any();
}
```

`PlayerController` reads from `StatusEffectController` when applying movement speed and damage.

### Visual distinction between power-up types

Players must be able to tell positive from negative at a glance. Use color tinting on the pickup sprite:

- Positive: bright yellow/gold tint
- Negative: red/dark tint
- Double-edged: purple tint

This is placeholder until final art arrives. It is enough for Phase 2 — the hesitation mechanic depends on visual recognition, not on reading text.

Place 2–3 additional power-ups in the handcrafted level — at least one negative and one double-edged. Position the negative one in a spot that's tempting (near enemies, in a tight corridor where players might grab it accidentally).

---

## Task 4 — 4-Player Input Routing

Replace the Phase 1 2-player InputManager stub with full 1–4 player support.

```csharp
public partial class InputManager : Node
{
    // Player 0: keyboard (device -1 in Godot convention)
    // Players 1-3: connected joypads (device index 0, 1, 2)
    // Unconnected player indices return false/0 silently

    private const int KeyboardPlayerIndex = 0;
    private const float DeadZone = InputConstants.GamepadDeadZone;

    public bool IsActionJustPressed(int playerIndex, string action)
    {
        if (playerIndex == KeyboardPlayerIndex)
            return _keyboardJustPressed.Contains(action);

        int device = playerIndex - 1; // player 1 = device 0, player 2 = device 1, etc.
        if (!Input.IsJoyKnown(device)) return false;
        return GetGamepadAction(device, action, justPressed: true);
    }

    public bool IsActionPressed(int playerIndex, string action)
    {
        if (playerIndex == KeyboardPlayerIndex)
            return Input.IsActionPressed(action);

        int device = playerIndex - 1;
        if (!Input.IsJoyKnown(device)) return false;
        return GetGamepadAction(device, action, justPressed: false);
    }

    public float GetAxis(int playerIndex, string negative, string positive)
    {
        if (playerIndex == KeyboardPlayerIndex)
            return Input.GetAxis(negative, positive);

        int device = playerIndex - 1;
        if (!Input.IsJoyKnown(device)) return 0f;

        float axis = Input.GetJoyAxis(device, JoyAxis.LeftX);
        return Mathf.Abs(axis) < DeadZone ? 0f : Mathf.Sign(axis);
    }

    public Vector2 GetLeftStickVector(int playerIndex)
    {
        if (playerIndex == KeyboardPlayerIndex)
        {
            // Synthesize from keyboard input actions
            return new Vector2(
                Input.GetAxis(InputActions.MoveLeft, InputActions.MoveRight),
                Input.GetAxis(InputActions.AimUp, InputActions.AimDown)
            );
        }

        int device = playerIndex - 1;
        if (!Input.IsJoyKnown(device)) return Vector2.Zero;

        var raw = new Vector2(
            Input.GetJoyAxis(device, JoyAxis.LeftX),
            Input.GetJoyAxis(device, JoyAxis.LeftY)
        );
        return raw.Length() < DeadZone ? Vector2.Zero : raw;
    }

    public Vector2 GetRightStickVector(int playerIndex)
    {
        if (playerIndex == KeyboardPlayerIndex) return Vector2.Zero; // keyboard has no right stick

        int device = playerIndex - 1;
        if (!Input.IsJoyKnown(device)) return Vector2.Zero;

        var raw = new Vector2(
            Input.GetJoyAxis(device, JoyAxis.RightX),
            Input.GetJoyAxis(device, JoyAxis.RightY)
        );
        return raw.Length() < DeadZone ? Vector2.Zero : raw;
    }

    // Existing: IsActionJustPressedFromEvent, GetGamepadAction, VectorToAimDirection
    // These remain unchanged from Phase 1
}
```

Update `LoadoutSelectController` to detect connected controllers and allow up to 4 players to join. A player joins by pressing any button on an unassigned controller. A player leaves by pressing the back/select button before confirming.

Update `GameStateManager.SetActivePlayerCount` to be called from `LoadoutSelectController` on confirm.

---

## Task 5 — Placeholder Boss

### BossIntro state

A title card. No cinematic yet. Just enough to mark the transition and build a beat of anticipation.

```csharp
// src/godot/ui/BossIntroController.cs
public partial class BossIntroController : Control
{
    [Export] private Label? _bossNameLabel;
    [Export] private Label? _chapterLabel;

    public void Show(string bossName, string chapterName)
    {
        _bossNameLabel = GetNodeOrNull<Label>("BossName")
            ?? throw new InvalidOperationException("BossName label not found");
        _chapterLabel = GetNodeOrNull<Label>("ChapterName")
            ?? throw new InvalidOperationException("ChapterName label not found");

        _bossNameLabel.Text = bossName;
        _chapterLabel.Text = chapterName;

        // Display for 2.5 seconds then advance
        GetTree().CreateTimer(2.5f).Timeout += OnIntroComplete;
    }

    private void OnIntroComplete()
        => GetNode<GameStateManager>("/root/GameStateManager")
            .TransitionTo(GameState.BossFight,
                new BossFightPayload("villain_baroness_cretacia", "chapter_cretaceous"));
}
```

### Placeholder boss — PlaceholderBoss.cs

A CharacterBody2D boss with a HP bar, 3 attack patterns, and a defeat sequence. Not Baroness Cretacia — that's Phase 4 with full art and patterns. This is a mechanical stand-in that exercises the full BossFight → VillainExit → RunSummary loop.

**Stats:**

- HP: 30
- Takes damage from all player projectiles
- ReviveWindow and SegmentRestart work exactly the same as in Segment state

**Three attack patterns, cycling:**

1. **Ground Charge** — rushes at player X position at high speed, bounces off walls twice then returns to center
2. **Projectile Burst** — fires 5 projectiles in a spread toward the nearest player
3. **Summon Minions** — spawns 2 GroundPatrollers from the pool

Pattern cycle: 1 → 2 → 3 → 1 → 2 → 3... with 1.5s pause between patterns.

```csharp
public partial class PlaceholderBoss : EnemyController
{
    private enum BossPattern { Charge, Burst, Summon }
    private BossPattern _currentPattern = BossPattern.Charge;
    private float _patternTimer = 2f; // delay before first attack
    private bool _isAttacking;

    [Export] private float ChargeSpeed { get; set; } = 200f;
    [Export] private float PatternPauseDuration { get; set; } = 1.5f;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        TickPattern((float)delta);
    }

    private void TickPattern(float delta)
    {
        if (_isAttacking) return;

        _patternTimer -= delta;
        if (_patternTimer > 0f) return;

        ExecutePattern(_currentPattern);
        _currentPattern = (BossPattern)(((int)_currentPattern + 1) % 3);
        _patternTimer = PatternPauseDuration;
    }

    protected override void Die()
    {
        IsDead = true;
        SetPhysicsProcess(false);
        // Brief pause, then signal defeat
        GetTree().CreateTimer(1.0f).Timeout += () =>
            GetNode<GameStateManager>("/root/GameStateManager")
                .TransitionTo(GameState.VillainExit,
                    new StatePayload[] { }); // VillainExit payload — see below
    }
}
```

### Boss HP bar UI

Add a simple HP bar to the HUD that appears only during BossFight state:

```csharp
// In HudController, listen for StateChanged signal:
// BossFight → show boss HP bar
// !BossFight → hide boss HP bar
```

The HP bar updates when `PlaceholderBoss.TakeDamage` is called. Wire via a signal on `PlaceholderBoss`:

```csharp
[Signal]
public delegate void HpChangedEventHandler(float currentHp, float maxHp);
```

### Boss scene

```
Boss.tscn
  PlaceholderBoss (CharacterBody2D) ← PlaceholderBoss.cs
    ├── CollisionShape2D ← large rectangle (boss is big)
    ├── AnimatedSprite2D ← placeholder — large colored rectangle
    └── AttackOrigin (Node2D) ← projectiles spawn here
```

Boss scene is placed at the end of the level, behind a door that opens when all regular enemies in the final room are dead.

**Room trigger:**

```csharp
// BossRoomTrigger.cs — Area2D
// On player entry: seal the room (close door), spawn boss, TransitionTo BossIntro
```

### VillainExit state — stub

A brief hold before RunSummary. No animation, no The Laugh yet — those are Phase 4.

```csharp
// VillainExitController.cs
// Display "Victory!" for 2 seconds, then TransitionTo RunSummary
```

The Laugh beat is Phase 4. Do not implement it here. The stub exists to confirm the state machine transitions correctly through VillainExit without skipping it.

### Add BossIntro and VillainExit to GameStateManager

These states were stubs in Phase 1. Implement them fully:

- `BossIntro` → receives `BossFightPayload`, loads `BossIntroController`, advances to `BossFight` after intro duration
- `BossFight` → loads boss scene, monitors boss death signal, player death rules same as `Segment`
- `VillainExit` → brief stub hold, advances to `RunSummary` via `RunSpine.Advance()`

---

## Task 6 — RunSummary Polish

The stats exist from Phase 1 (kill count, death count, time). Surface them properly.

- Kills: displayed as a count with a label ("Enemies Defeated: 12")
- Deaths: displayed as segment restart count ("Times Wiped: 3")
- Time: formatted as MM:SS ("Run Time: 02:34")
- A "Play Again" prompt that goes to LoadoutSelect
- A "Quit" prompt that goes to Title

No unlock reveals yet. No meta progression. Just clean presentation of the three numbers that already exist.

---

## Honey Badger AlwaysFitsGaps — Verify When Sprites Arrive

This item is in the exit checklist but marked as art-dependent. When the developer provides real Honey Badger sprites, verify:

1. Honey Badger walks through the tight gap in the level without sliding
2. Bear cannot walk through the same gap without sliding
3. The `AlwaysFitsGaps` flag in the definition is what controls this — not a hardcoded character check

The implementation should already be correct from Phase 1 (collision shape is smaller). This is a visual and feel verification, not a code task.

---

## Exit Checklist

Before marking Phase 2 complete, confirm every item:

- [x] `dotnet build` clean — zero warnings, zero errors
- [x] `dotnet test` passing — all tests green
- [x] `dotnet format --verify-no-changes` clean
- [x] `grep -r "using Godot" FeralFrenzy.Core/` returns zero results
- [x] Bear takes 4 hits before going down
- [x] Honey Badger takes 3 hits before going down
- [x] Hit flash fires on damage — visually readable
- [x] Invincibility frames prevent rapid re-damage
- [x] Enemies require multiple hits to kill
- [x] Enemy hit stun is visible — brief pause in movement on hit
- [x] Enemies fade/dissolve on death rather than instant QueueFree
- [x] Kill count increments correctly in RunSummary
- [x] Player projectiles do not damage players
- [x] Enemy projectiles do not damage enemies
- [x] Spinning Blade penetrates multiple enemies
- [x] Spinning Blade returns to player after max distance
- [x] Spinning Blade feels meaningfully different from Default Blaster
- [ ] MountedDino has two phases — rider dies, dino charges
- [ ] PteroBomber drops bombs when over the player
- [x] Enemy count with 2 players is visibly busier than with 1 player
- [x] Rapid Fire power-up works (existing)
- [x] At least one negative power-up causes a visible penalty
- [ ] Berserker double-edged effect increases damage AND incoming damage
- [x] Power-up color tinting distinguishes positive/negative/double-edged at a glance
- [x] 4-player input: keyboard + 3 gamepads all route correctly
- [x] Unconnected player indices return no input silently
- [x] Player join/leave works in LoadoutSelect
- [x] Boss room trigger seals the room and spawns the boss
- [x] BossIntro title card displays for 2.5 seconds
- [x] Boss cycles through 3 attack patterns
- [x] Boss HP bar appears and depletes correctly
- [x] Boss defeat triggers VillainExit
- [x] VillainExit stub holds briefly then transitions to RunSummary
- [x] RunSummary shows kills, deaths, time in formatted display
- [x] "Play Again" returns to LoadoutSelect
- [x] "Quit" returns to Title
- [x] Full loop playable: Title → Loadout → Level → Boss → VillainExit → RunSummary → Title
- [ ] `DEVLOG.md` updated

---

## What Is Explicitly Out of Scope

Do not build any of the following. Flag as deferred in DEVLOG.md:

- Croc and Hammerhead (Phase 3)
- Character secondaries — Roar, Pinball Jump (Phase 3)
- Audio of any kind (Phase 4)
- Cinematics — chapter intro, The Laugh, full VillainExit (Phase 4)
- Real Baroness Cretacia boss with full art and patterns (Phase 4)
- Any generator code (Phase 3)
- Meta progression or unlock system (Phase 4)
- Tier 3 weapons (Phase 4)
- Destructible geometry (Phase 4)
- Workshop browser (post-launch)
- Any Chapter 2 or Chapter 3 content

