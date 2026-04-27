using System;
using System.Collections.Generic;
using FeralFrenzy.Core.Animation;
using FeralFrenzy.Core.Data.Content;
using FeralFrenzy.Godot.Animation;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.Core;
using FeralFrenzy.Godot.Weapons;
using FeralFrenzy.Godot.World;
using Godot;

namespace FeralFrenzy.Godot.Characters;

public partial class PlayerController : GameEntity
{
    private const int MaxJumps = 2;

    [Signal]
    public delegate void WentDownEventHandler();

    [Signal]
    public delegate void WeaponChangedEventHandler(WeaponController weapon);

    [Export]
    public FFCharacterDefinition? Definition { get; set; }

    [Export]
    public int PlayerIndex { get; set; } = 0;

    // assigned in _Ready()
    private FFCharacterDefinition _definition = null!;
    private InputManager _input = null!;
    private CollisionShape2D _collisionShape = null!;

    // Resolved via GetNodeOrNull in _Ready — nullable, may be absent until scenes are updated.
    private Node2D? _weaponMount;
    private Sprite2D? _bodySprite;

    private bool _facingLeft;
    private bool _isSliding;
    private float _slideTimer;
    private float _jumpBufferTimer;
    private float _invincibilityTimer;
    private bool _fireRequested;
    private bool _slideRequested;
    private bool _jumpExecutedThisFrame;
    private bool _tookHitThisFrame;
    private int _jumpsRemaining;
    private WeaponController? _equippedWeapon;
    private AimDirection _aimDirection = AimDirection.Right;

    // Set when gamepad stick provides a precise direction; null means use _aimDirection enum.
    private Vector2? _rawAimVector;
    private StatusEffectController _statusEffects = null!;

    public bool IsDown { get; private set; }

    public bool IsDead { get; private set; }

    public int CurrentHp { get; private set; }

    private bool IsInvincible => _invincibilityTimer > 0f;

    public override void _Ready()
    {
        _definition = Definition
            ?? throw new InvalidOperationException(
                $"{nameof(PlayerController)} '{Name}': Definition not assigned.");

        _collisionShape = GetNode<CollisionShape2D>(NodePaths.CollisionShape);
        _weaponMount = GetNodeOrNull<Node2D>(NodePaths.WeaponMount);
        _bodySprite = GetNodeOrNull<Sprite2D>(NodePaths.BodySprite);

        _input = GetNode<InputManager>(AutoloadPaths.InputManager);

        CurrentHp = _definition.MaxHp;

        _statusEffects = new StatusEffectController();
        AddChild(_statusEffects);

        AddToGroup("players");

        // Animation — Tier 3 (AnimationPlayer). Clips are hand-keyed by the developer.
        // AnimationPlayer may be null until Bear.tscn / HoneyBadger.tscn are updated.
        AnimationPlayer? animPlayer = GetNodeOrNull<AnimationPlayer>(NodePaths.AnimationPlayer);
        if (animPlayer is not null)
        {
            ConfigureAnimation<FFPlayerAnimationState>()
                .WithAnimationPlayer(animPlayer)
                .WithRules(
                    defaultState: FFPlayerAnimationState.Idle,
                    rules: new List<AnimationRule<FFPlayerAnimationState>>
                    {
                        new AnimationRule<FFPlayerAnimationState>((_, i) => i.IsDead, FFPlayerAnimationState.Death),
                        new AnimationRule<FFPlayerAnimationState>((_, i) => i.TookHit, FFPlayerAnimationState.Hit),
                        new AnimationRule<FFPlayerAnimationState>((_, i) => i.IsJumping, FFPlayerAnimationState.Jump),
                        new AnimationRule<FFPlayerAnimationState>((_, i) => i.IsSliding, FFPlayerAnimationState.Slide),
                        new AnimationRule<FFPlayerAnimationState>((_, i) => !i.IsOnFloor && i.VelocityY > 0f, FFPlayerAnimationState.Fall),
                        new AnimationRule<FFPlayerAnimationState>((c, i) => c == FFPlayerAnimationState.Walk && i.IsMoving, FFPlayerAnimationState.Walk),
                        new AnimationRule<FFPlayerAnimationState>((c, i) => c == FFPlayerAnimationState.WalkStart && i.IsMoving, FFPlayerAnimationState.Walk),
                        new AnimationRule<FFPlayerAnimationState>((_, i) => i.IsMoving, FFPlayerAnimationState.WalkStart),
                    })
                .WithOneShots(new List<FFPlayerAnimationState>
                {
                    FFPlayerAnimationState.WalkStart,
                    FFPlayerAnimationState.Slide,
                    FFPlayerAnimationState.Hit,
                    FFPlayerAnimationState.Death,
                })
                .WithClips(new Dictionary<FFPlayerAnimationState, string>
                {
                    [FFPlayerAnimationState.Idle] = AnimationNames.Idle,
                    [FFPlayerAnimationState.WalkStart] = AnimationNames.WalkStart,
                    [FFPlayerAnimationState.Walk] = AnimationNames.Walk,
                    [FFPlayerAnimationState.Jump] = AnimationNames.Jump,
                    [FFPlayerAnimationState.Fall] = AnimationNames.Fall,
                    [FFPlayerAnimationState.Slide] = AnimationNames.Slide,
                    [FFPlayerAnimationState.Death] = AnimationNames.Death,
                    [FFPlayerAnimationState.Hit] = AnimationNames.Hit,
                })
                .WithInput(() => new AnimationInput(
                    IsMoving: Mathf.Abs(Velocity.X) > 0.1f,
                    IsOnFloor: IsOnFloor(),
                    IsOnWall: IsOnWall(),
                    IsJumping: _jumpExecutedThisFrame,
                    IsSliding: _isSliding,
                    IsAttacking: false,
                    IsDead: IsDead || IsDown,
                    TookHit: _tookHitThisFrame,
                    VelocityY: Velocity.Y,
                    VelocityX: Velocity.X))
                .Build();
        }
    }

    // Discrete inputs (jump, slide, fire) are captured here so no press is ever
    // missed between physics ticks. Continuous inputs (move, aim) are polled in
    // OnPhysicsProcess where per-frame state is what matters.
    public override void _Input(InputEvent @event)
    {
        if (IsDown || IsDead)
        {
            return;
        }

        if (_input.IsActionJustPressedFromEvent(@event, PlayerIndex, InputActions.Jump))
        {
            _jumpBufferTimer = _definition.JumpBufferDuration;
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

    protected override void OnPhysicsProcess(float delta)
    {
        if (IsDown || IsDead)
        {
            return;
        }

        // Reset per-frame animation flags before game logic runs.
        _jumpExecutedThisFrame = false;
        _tookHitThisFrame = false;

        TickTimers(delta);

        if (_bodySprite is not null)
        {
            _bodySprite.Visible = !IsInvincible || (Engine.GetFramesDrawn() % 4 < 2);
        }

        ApplyGravity(delta);
        HandleJump();
        HandleHorizontalMovement();
        HandleSlide();
        HandleAiming();
        HandleFiring();

        MoveAndSlide();
        UpdateArmAim();
    }

    public void TakeDamage(int amount = 1)
    {
        if (IsInvincible || IsDown || IsDead)
        {
            return;
        }

        int scaled = Mathf.Max(1, Mathf.RoundToInt(amount * _statusEffects.GetIncomingDamageMultiplier()));
        CurrentHp -= scaled;
        _invincibilityTimer = _definition.InvincibilitySeconds;
        _tookHitThisFrame = true;

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

    public bool IsBerserkerActive => _statusEffects.IsBerserkerActive();

    public void RestoreHp(int amount)
    {
        if (IsDown || IsDead)
        {
            return;
        }

        CurrentHp = Mathf.Min(CurrentHp + amount, _definition.MaxHp);
    }

    public WeaponController? GetEquippedWeapon() => _equippedWeapon;

    public void Revive(bool fullHeal = false)
    {
        IsDown = false;
        CurrentHp = fullHeal ? _definition.MaxHp : 1;
        _invincibilityTimer = _definition.InvincibilitySeconds;
        _statusEffects.SetProcess(true);
        SetPhysicsProcess(true);
        _jumpsRemaining = MaxJumps;
        _isSliding = false;
        _slideRequested = false;
        _fireRequested = false;
        _collisionShape.Scale = Vector2.One;

        if (_bodySprite is not null)
        {
            _bodySprite.Visible = true;
            _bodySprite.Modulate = Colors.White;
        }
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
        EmitSignal(SignalName.WeaponChanged, weapon);
    }

    // Snaps to exact horizontal when the stick elevation is within the threshold angle,
    // preventing accidental floor shots when the player intends to aim sideways.
    private static Vector2 SnapToHorizontalIfClose(Vector2 v)
    {
        float elevationRad = Mathf.Atan2(Mathf.Abs(v.Y), Mathf.Abs(v.X));
        if (elevationRad < Mathf.DegToRad(InputConstants.GamepadHorizontalSnapDeg))
        {
            return new Vector2(Mathf.Sign(v.X), 0f);
        }

        return v.Normalized();
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

        if (_bodySprite is not null)
        {
            _bodySprite.Visible = true;
            _bodySprite.Modulate = Colors.White;
        }

        EmitSignal(SignalName.WentDown);
    }

    private void PlayHitFlash()
    {
        if (_bodySprite is null)
        {
            return;
        }

        _bodySprite.Modulate = new Color(1f, 0.3f, 0.3f);
        GetTree().CreateTimer(0.1f).Timeout += () => _bodySprite.Modulate = Colors.White;
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
            float gravity = LevelController.Instance?.EffectiveGravity ?? PhysicsConstants.Gravity;
            Velocity = Velocity with { Y = Velocity.Y + (gravity * delta) };
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
            Velocity = Velocity with { Y = _definition.JumpVelocity * _definition.JumpArcMultiplier };
            _jumpsRemaining = MaxJumps - 1;
            _jumpBufferTimer = 0f;
            _jumpExecutedThisFrame = true;
        }
        else if (IsOnWall())
        {
            Vector2 wallNormal = GetWallNormal();
            Velocity = new Vector2(
                wallNormal.X * _definition.WallKickVelocityX,
                _definition.JumpVelocity * _definition.JumpArcMultiplier);
            _jumpsRemaining = MaxJumps - 1;
            _jumpBufferTimer = 0f;
            _jumpExecutedThisFrame = true;
        }
        else if (_jumpsRemaining > 0)
        {
            Velocity = Velocity with { Y = _definition.JumpVelocity * _definition.JumpArcMultiplier };
            _jumpsRemaining--;
            _jumpBufferTimer = 0f;
            _jumpExecutedThisFrame = true;
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

        Velocity = Velocity with { X = dir * _definition.MoveSpeed * _statusEffects.GetSpeedMultiplier() };

        if (dir != 0f)
        {
            _facingLeft = dir < 0f;
            if (_bodySprite is not null)
            {
                _bodySprite.FlipH = _facingLeft;
            }
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
        _slideTimer = _definition.SlideDuration;

        float dir = _facingLeft ? -1f : 1f;
        Velocity = Velocity with { X = dir * _definition.MoveSpeed * _definition.SlideSpeedMultiplier };

        // Shrink collision shape to allow gap traversal — the solvability equaliser
        _collisionShape.Scale = new Vector2(1f, 0.5f);
    }

    private void HandleAiming()
    {
        if (PlayerIndex != InputConstants.KeyboardPlayerIndex)
        {
            // Right stick: precise angle autofire.
            Vector2 rightStick = _input.GetRightStickVector(PlayerIndex);
            if (rightStick.Length() > InputConstants.GamepadAimThreshold)
            {
                _rawAimVector = SnapToHorizontalIfClose(rightStick);
                _fireRequested = true;
                return;
            }

            // No stick input — clear raw aim and fall back to face direction.
            _rawAimVector = null;
            _aimDirection = _facingLeft ? AimDirection.Left : AimDirection.Right;
            return;
        }

        // Keyboard: facing direction + optional up/down modifier → 6 directions.
        _rawAimVector = null;
        bool up = _input.IsActionPressed(PlayerIndex, InputActions.AimUp);
        bool down = _input.IsActionPressed(PlayerIndex, InputActions.AimDown);

        _aimDirection = (up, down, _facingLeft) switch
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

        float damage = _definition.WeaponDamageMultiplier * _statusEffects.GetDamageMultiplier();

        if (PlayerIndex != InputConstants.KeyboardPlayerIndex)
        {
            // Right stick raw aim was already captured in HandleAiming.
            // For X-button shots with no right stick, fall back to left stick (also precise).
            if (_rawAimVector is null)
            {
                Vector2 leftStick = _input.GetLeftStickVector(PlayerIndex);
                if (leftStick.Length() > InputConstants.GamepadDeadZone)
                {
                    _rawAimVector = SnapToHorizontalIfClose(leftStick);
                }
            }

            if (_rawAimVector is Vector2 rawDir)
            {
                _equippedWeapon.FireRaw(rawDir, damage);
                return;
            }
        }

        _equippedWeapon.Fire(_aimDirection, damage);
    }

    private void UpdateArmAim()
    {
        // Applies facing-direction rotation to arm/weapon sprites on top of
        // whatever AnimationPlayer has set. Noop until art arrives.
        Sprite2D? armSprite = GetNodeOrNull<Sprite2D>(NodePaths.ArmSprite);
        Sprite2D? weaponSprite = GetNodeOrNull<Sprite2D>(NodePaths.WeaponSprite);

        if (armSprite is null && weaponSprite is null)
        {
            return;
        }

        // Aim direction determines the rotation applied to arm/weapon sprites.
        float rotationRad = _aimDirection switch
        {
            AimDirection.Right => 0f,
            AimDirection.UpRight => -Mathf.Pi / 4f,
            AimDirection.Up => -Mathf.Pi / 2f,
            AimDirection.UpLeft => -3f * Mathf.Pi / 4f,
            AimDirection.Left => Mathf.Pi,
            AimDirection.DownLeft => 3f * Mathf.Pi / 4f,
            AimDirection.Down => Mathf.Pi / 2f,
            AimDirection.DownRight => Mathf.Pi / 4f,
            _ => 0f,
        };

        if (armSprite is not null)
        {
            armSprite.Rotation = rotationRad;
        }

        if (weaponSprite is not null)
        {
            weaponSprite.Rotation = rotationRad;
        }
    }
}
