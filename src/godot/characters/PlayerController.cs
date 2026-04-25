using System;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.Weapons;
using FeralFrenzy.Godot.World;
using Godot;

namespace FeralFrenzy.Godot.Characters;

public partial class PlayerController : CharacterBody2D
{
    private const float WallKickVelocityX = 200f;
    private const float SlideSpeedMultiplier = 1.4f;
    private const float SlideDuration = 0.35f;
    private const int MaxJumps = 2;
    private const float JumpBufferDuration = 0.12f;

    [Export]
    public FFCharacterDefinition? Definition { get; set; }

    [Export]
    public int PlayerIndex { get; set; } = 0;

    // Initialized in _Ready — Godot does not call _Ready during construction
    private InputManager _input = null!;

    // Resolved via GetNode in _Ready — node-type exports unreliable in hand-written .tscn
    private AnimatedSprite2D? _sprite;
    private CollisionShape2D _collisionShape = null!;
    private Node2D? _weaponMount;

    private bool _isSliding;
    private float _slideTimer;
    private float _jumpBufferTimer;
    private float _invincibilityTimer;
    private bool _fireRequested;
    private bool _slideRequested;
    private bool _wasMoving;
    private int _jumpsRemaining;
    private WeaponController? _equippedWeapon;
    private AimDirection _aimDirection = AimDirection.Right;
    private StatusEffectController _statusEffects = null!;

    public bool IsDown { get; private set; }
    public bool IsDead { get; private set; }
    public int CurrentHp { get; private set; }

    private bool IsInvincible => _invincibilityTimer > 0f;

    public override void _Ready()
    {
        if (Definition is null)
        {
            throw new InvalidOperationException(
                $"{nameof(PlayerController)} '{Name}': Definition not assigned.");
        }

        _sprite = GetNodeOrNull<AnimatedSprite2D>(NodePaths.AnimatedSprite);
        _collisionShape = GetNode<CollisionShape2D>(NodePaths.CollisionShape);
        _weaponMount = GetNodeOrNull<Node2D>(NodePaths.WeaponMount);

        _input = GetNode<InputManager>(AutoloadPaths.InputManager);

        CurrentHp = Definition.MaxHp;

        _statusEffects = new StatusEffectController();
        AddChild(_statusEffects);

        AddToGroup("players");
    }

    // Discrete inputs (jump, slide, fire) are captured here so no press is ever
    // missed between physics ticks. Continuous inputs (move, aim) are polled in
    // _PhysicsProcess where per-frame state is what matters.
    public override void _Input(InputEvent @event)
    {
        if (IsDown || IsDead)
        {
            return;
        }

        if (_input.IsActionJustPressedFromEvent(@event, PlayerIndex, InputActions.Jump))
        {
            _jumpBufferTimer = JumpBufferDuration;
        }

        if (_input.IsActionJustPressedFromEvent(@event, PlayerIndex, InputActions.Slide))
        {
            _slideRequested = true;
        }

        if (_input.IsActionJustPressedFromEvent(@event, PlayerIndex, InputActions.PrimaryAttack))
        {
            _fireRequested = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDown || IsDead)
        {
            return;
        }

        float dt = (float)delta;

        TickTimers(dt);

        if (_sprite is not null)
        {
            _sprite.Visible = !IsInvincible || (Engine.GetFramesDrawn() % 4 < 2);
        }

        ApplyGravity(dt);
        HandleJump();
        HandleHorizontalMovement();
        HandleSlide();
        HandleAiming();
        HandleFiring();

        MoveAndSlide();
        UpdateAnimation();
    }

    public void TakeDamage(int amount = 1)
    {
        if (IsInvincible || IsDown || IsDead)
        {
            return;
        }

        int scaled = Mathf.Max(1, Mathf.RoundToInt(amount * _statusEffects.GetIncomingDamageMultiplier()));
        CurrentHp -= scaled;
        _invincibilityTimer = Definition!.InvincibilitySeconds;

        if (CurrentHp <= 0)
        {
            GoDown();
        }
        else
        {
            PlayHitFlash();
        }
    }

    public void ApplyStatusEffect(StatusEffect effect)
    {
        _statusEffects.Apply(effect);
    }

    public void RestoreHp(int amount)
    {
        if (IsDown || IsDead)
        {
            return;
        }

        CurrentHp = Mathf.Min(CurrentHp + amount, Definition!.MaxHp);
    }

    public WeaponController? GetEquippedWeapon() => _equippedWeapon;

    public void Revive(bool fullHeal = false)
    {
        IsDown = false;
        CurrentHp = fullHeal ? Definition!.MaxHp : 1;
        _invincibilityTimer = Definition!.InvincibilitySeconds;
        _statusEffects.SetProcess(true);
        SetPhysicsProcess(true);
        _jumpsRemaining = MaxJumps;
        _isSliding = false;
        _slideRequested = false;
        _fireRequested = false;
        _collisionShape.Scale = Vector2.One;

        if (_sprite is not null)
        {
            _sprite.Visible = true;
            _sprite.Modulate = Colors.White;
        }

        TryPlayAnimation(AnimationNames.Idle);
    }

    public void EquipWeapon(WeaponController weapon)
    {
        if (_equippedWeapon is not null && IsInstanceValid(_equippedWeapon))
        {
            _weaponMount?.RemoveChild(_equippedWeapon);
            _equippedWeapon.QueueFree();
        }

        _equippedWeapon = weapon;
        _weaponMount?.AddChild(weapon);
    }

    private static AimDirection VectorToAimDirection(Vector2 v)
    {
        float angle = Mathf.Atan2(v.Y, v.X);
        int sector = ((Mathf.RoundToInt(angle / (Mathf.Pi / 4f)) % 8) + 8) % 8;
        return sector switch
        {
            0 => AimDirection.Right,
            1 => AimDirection.DownRight,
            2 => AimDirection.Down,
            3 => AimDirection.DownLeft,
            4 => AimDirection.Left,
            5 => AimDirection.UpLeft,
            6 => AimDirection.Up,
            7 => AimDirection.UpRight,
            _ => AimDirection.Right,
        };
    }

    private void GoDown()
    {
        IsDown = true;
        Velocity = Vector2.Zero;
        _statusEffects.SetProcess(false);
        SetPhysicsProcess(false);

        if (_sprite is not null)
        {
            _sprite.Visible = true;
            _sprite.Modulate = Colors.White;
        }

        TryPlayAnimation(AnimationNames.Death);
        LevelController.Instance?.HandlePlayerDown(this);
    }

    private void PlayHitFlash()
    {
        if (_sprite is null)
        {
            return;
        }

        _sprite.Modulate = new Color(1f, 0.3f, 0.3f);
        GetTree().CreateTimer(0.1f).Timeout += () => _sprite.Modulate = Colors.White;
    }

    private void TickTimers(float delta)
    {
        _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - delta);

        if (_invincibilityTimer > 0f)
        {
            _invincibilityTimer -= delta;
        }

        if (!_isSliding)
        {
            return;
        }

        _slideTimer -= delta;
        if (_slideTimer <= 0f)
        {
            _isSliding = false;
            _collisionShape.Scale = Vector2.One;
        }
    }

    private void ApplyGravity(float delta)
    {
        if (!IsOnFloor())
        {
            Velocity = Velocity with { Y = Velocity.Y + (PhysicsConstants.Gravity * delta) };
        }
    }

    private void HandleJump()
    {
        if (IsOnFloor())
        {
            _jumpsRemaining = MaxJumps;
        }

        if (_jumpBufferTimer <= 0f)
        {
            return;
        }

        if (IsOnFloor())
        {
            Velocity = Velocity with { Y = Definition!.JumpVelocity * Definition.JumpArcMultiplier };
            _jumpsRemaining = MaxJumps - 1;
            _jumpBufferTimer = 0f;
        }
        else if (IsOnWall())
        {
            Vector2 wallNormal = GetWallNormal();
            Velocity = new Vector2(
                wallNormal.X * WallKickVelocityX,
                Definition!.JumpVelocity * Definition.JumpArcMultiplier);
            _jumpsRemaining = MaxJumps - 1;
            _jumpBufferTimer = 0f;
        }
        else if (_jumpsRemaining > 0)
        {
            Velocity = Velocity with { Y = Definition!.JumpVelocity * Definition.JumpArcMultiplier };
            _jumpsRemaining--;
            _jumpBufferTimer = 0f;
        }
    }

    private void HandleHorizontalMovement()
    {
        if (_isSliding)
        {
            return;
        }

        float dir = _input.GetAxis(PlayerIndex, InputActions.MoveLeft, InputActions.MoveRight);

        if (_statusEffects.AreControlsReversed())
        {
            dir = -dir;
        }

        Velocity = Velocity with { X = dir * Definition!.MoveSpeed * _statusEffects.GetSpeedMultiplier() };

        if (dir != 0f && _sprite is not null)
        {
            _sprite.FlipH = dir < 0f;
        }
    }

    private void HandleSlide()
    {
        bool requested = _slideRequested;
        _slideRequested = false;

        if (_isSliding || !requested)
        {
            return;
        }

        _isSliding = true;
        _slideTimer = SlideDuration;

        float dir = _sprite?.FlipH ?? false ? -1f : 1f;
        Velocity = Velocity with { X = dir * Definition!.MoveSpeed * SlideSpeedMultiplier };

        // Shrink collision shape to allow gap traversal — the solvability equaliser
        _collisionShape.Scale = new Vector2(1f, 0.5f);
    }

    private void HandleAiming()
    {
        if (PlayerIndex != InputConstants.KeyboardPlayerIndex)
        {
            // Right stick: aim in any of 8 directions and auto-fire.
            Vector2 rightStick = _input.GetRightStickVector(PlayerIndex);
            if (rightStick.Length() > InputConstants.GamepadAimThreshold)
            {
                _aimDirection = VectorToAimDirection(rightStick);
                _fireRequested = true;
                return;
            }

            // Fallback: face direction.
            _aimDirection = _sprite?.FlipH ?? false ? AimDirection.Left : AimDirection.Right;
            return;
        }

        // Keyboard P1: facing direction + optional up/down modifier → 6 directions.
        bool up = _input.IsActionPressed(PlayerIndex, InputActions.AimUp);
        bool down = _input.IsActionPressed(PlayerIndex, InputActions.AimDown);
        bool facingLeft = _sprite?.FlipH ?? false;

        _aimDirection = (up, down, facingLeft) switch
        {
            (true, false, false) => AimDirection.UpRight,
            (true, false, true) => AimDirection.UpLeft,
            (false, true, false) => AimDirection.DownRight,
            (false, true, true) => AimDirection.DownLeft,
            (false, false, false) => AimDirection.Right,
            (false, false, true) => AimDirection.Left,
            _ => _aimDirection,
        };
    }

    private void HandleFiring()
    {
        bool requested = _fireRequested;
        _fireRequested = false;

        if (!requested || _equippedWeapon is null)
        {
            return;
        }

        if (PlayerIndex != InputConstants.KeyboardPlayerIndex)
        {
            Vector2 leftStick = _input.GetLeftStickVector(PlayerIndex);
            if (leftStick.Length() > InputConstants.GamepadDeadZone)
            {
                _aimDirection = VectorToAimDirection(leftStick);
            }
        }

        _equippedWeapon.Fire(_aimDirection, Definition!.WeaponDamageMultiplier * _statusEffects.GetDamageMultiplier());
    }

    private void UpdateAnimation()
    {
        if (_isSliding)
        {
            TryPlayAnimation(AnimationNames.Slide);
            return;
        }

        if (!IsOnFloor())
        {
            TryPlayAnimation(Velocity.Y < 0f ? AnimationNames.Jump : AnimationNames.Fall);
            return;
        }

        bool isMoving = Mathf.Abs(Velocity.X) > 0.1f;
        if (isMoving)
        {
            if (!_wasMoving)
            {
                TryPlayAnimation(AnimationNames.WalkStart);
            }
            else if (_sprite?.Animation == AnimationNames.WalkStart && !_sprite.IsPlaying())
            {
                TryPlayAnimation(AnimationNames.Walk);
            }
        }
        else
        {
            TryPlayAnimation(AnimationNames.Idle);
        }

        _wasMoving = isMoving;
    }

    private void TryPlayAnimation(string animName)
    {
        if (_sprite?.SpriteFrames is not null && _sprite.SpriteFrames.HasAnimation(animName))
        {
            _sprite.Play(animName);
        }
    }
}
