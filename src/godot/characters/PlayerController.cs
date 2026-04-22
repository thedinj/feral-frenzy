using System;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.Weapons;
using Godot;

namespace FeralFrenzy.Godot.Characters;

public partial class PlayerController : CharacterBody2D
{
    private const float Gravity = 600f;
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
    private GameStateManager _gameState = null!;

    // Resolved via GetNode in _Ready — node-type exports unreliable in hand-written .tscn
    private AnimatedSprite2D? _sprite;
    private CollisionShape2D _collisionShape = null!;
    private Node2D? _weaponMount;

    private bool _isSliding;
    private float _slideTimer;
    private float _jumpBufferTimer;
    private bool _fireRequested;
    private bool _slideRequested;
    private bool _wasMoving;
    private int _jumpsRemaining;
    private WeaponController? _equippedWeapon;
    private AimDirection _aimDirection = AimDirection.Right;

    public bool IsDown { get; private set; }
    public bool IsDead { get; private set; }

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

        _input = GetNode<InputManager>("/root/InputManager");
        _gameState = GetNode<GameStateManager>("/root/GameStateManager");

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
        ApplyGravity(dt);
        HandleJump();
        HandleHorizontalMovement();
        HandleSlide();
        HandleAiming();
        HandleFiring();

        MoveAndSlide();
        UpdateAnimation();
    }

    public void TakeDamage()
    {
        if (IsDown || IsDead)
        {
            return;
        }

        GoDown();
    }

    public void Revive()
    {
        IsDown = false;
        SetPhysicsProcess(true);
        _jumpsRemaining = MaxJumps;
        _isSliding = false;
        _slideRequested = false;
        _fireRequested = false;
        _collisionShape.Scale = Vector2.One;
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

    private void GoDown()
    {
        IsDown = true;
        Velocity = Vector2.Zero;
        SetPhysicsProcess(false);
        TryPlayAnimation(AnimationNames.Death);
        _gameState.NotifyPlayerDown(PlayerIndex);
    }

    private void TickTimers(float delta)
    {
        _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - delta);

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
            Velocity = Velocity with { Y = Velocity.Y + (Gravity * delta) };
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
        Velocity = Velocity with { X = dir * Definition!.MoveSpeed };

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

        _equippedWeapon.Fire(_aimDirection, Definition!.WeaponDamageMultiplier);
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
