using System;
using System.Collections.Generic;
using FeralFrenzy.Core.Animation;
using FeralFrenzy.Core.Data.Content;
using FeralFrenzy.Godot.Animation;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.Core;
using FeralFrenzy.Godot.Enemies.Behaviors;
using FeralFrenzy.Godot.Enemies.Components;
using Godot;

namespace FeralFrenzy.Godot.Enemies;

public partial class EnemyHost : GameEntity
{
    [Export]
    public FFEnemyDefinition? Definition { get; set; }

    [Signal]
    public delegate void HpChangedEventHandler(float current, float max);

    [Signal]
    public delegate void DiedEventHandler();

    [Signal]
    public delegate void ProjectileSpawnRequestedEventHandler(Vector2 spawnPos, Vector2 dir, float speed, float impact);

    [Signal]
    public delegate void MinionSummonRequestedEventHandler(string assetKey, Vector2 offset1, Vector2 offset2);

    public float CurrentHp { get; set; }

    public bool IsDead { get; private set; }

    public bool IsHitStunned => _hitStun.IsActive;

    // Set by behavior nodes when attacking; read by animation input closure to drive Attack state.
    public bool IsAttacking { get; set; }

    // assigned in _Ready()
    private FFEnemyDefinition _definition = null!;
    private GameStateManager _gameState = null!;
    private HitStunComponent _hitStun = null!;
    private ITickBehavior _tick = null!;
    private IGravityBehavior _gravity = null!;
    private IDeathBehavior? _death;
    private IDamageBehavior? _damageBehavior;
    private float _invincibilityTimer;

    public override void _Ready()
    {
        _definition = Definition
            ?? throw new InvalidOperationException($"{Name}: Definition not assigned.");
        CurrentHp = _definition.MaxHp;

        _gameState = GetNode<GameStateManager>(AutoloadPaths.GameStateManager);

        _hitStun = GetNodeOrNull<HitStunComponent>(NodePaths.EnemyHitStun)
            ?? throw new InvalidOperationException(
                $"{Name}: missing HitStunComponent '{NodePaths.EnemyHitStun}'.");

        Node? behaviorNode = GetNodeOrNull<Node>(NodePaths.EnemyBehavior)
            ?? throw new InvalidOperationException(
                $"{Name}: missing behavior node '{NodePaths.EnemyBehavior}'.");
        _tick = behaviorNode as ITickBehavior
            ?? throw new InvalidOperationException(
                $"{Name}: '{NodePaths.EnemyBehavior}' does not implement ITickBehavior.");
        _damageBehavior = behaviorNode as IDamageBehavior;
        IAnimationSetup? animSetup = behaviorNode as IAnimationSetup;

        Node? gravityNode = GetNodeOrNull<Node>(NodePaths.EnemyGravity)
            ?? throw new InvalidOperationException(
                $"{Name}: missing gravity node '{NodePaths.EnemyGravity}'.");
        _gravity = gravityNode as IGravityBehavior
            ?? throw new InvalidOperationException(
                $"{Name}: '{NodePaths.EnemyGravity}' does not implement IGravityBehavior.");

        Node? deathNode = GetNodeOrNull<Node>(NodePaths.EnemyDeath);
        _death = deathNode as IDeathBehavior;

        AddToGroup("enemies");

        // Animation. Behavior node may implement IAnimationSetup to own configuration
        // entirely (custom state enum, custom rules). Otherwise EnemyHost applies the
        // FFSimpleEnemyState defaults — all five states, subset-safe (missing clips are
        // silently skipped by the driver).
        if (animSetup is not null)
        {
            animSetup.Configure(this);
        }
        else
        {
            AnimatedSprite2D? sprite = GetNodeOrNull<AnimatedSprite2D>(NodePaths.AnimatedSprite);
            if (sprite is not null)
            {
                ConfigureAnimation<FFSimpleEnemyState>()
                    .WithSprite(sprite)
                    .WithRules(
                        defaultState: FFSimpleEnemyState.Idle,
                        rules: new List<AnimationRule<FFSimpleEnemyState>>
                        {
                            new AnimationRule<FFSimpleEnemyState>((_, i) => i.IsDead, FFSimpleEnemyState.Death),
                            new AnimationRule<FFSimpleEnemyState>((_, i) => i.TookHit, FFSimpleEnemyState.Hit),
                            new AnimationRule<FFSimpleEnemyState>((_, i) => i.IsAttacking, FFSimpleEnemyState.Attack),
                            new AnimationRule<FFSimpleEnemyState>((_, i) => i.IsMoving, FFSimpleEnemyState.Walk),
                        })
                    .WithOneShots(new List<FFSimpleEnemyState>
                    {
                        FFSimpleEnemyState.Attack,
                        FFSimpleEnemyState.Hit,
                        FFSimpleEnemyState.Death,
                    })
                    .WithClips(new Dictionary<FFSimpleEnemyState, string>
                    {
                        [FFSimpleEnemyState.Idle] = AnimationNames.Idle,
                        [FFSimpleEnemyState.Walk] = AnimationNames.Walk,
                        [FFSimpleEnemyState.Attack] = AnimationNames.Attack,
                        [FFSimpleEnemyState.Hit] = AnimationNames.Hit,
                        [FFSimpleEnemyState.Death] = AnimationNames.Death,
                    })
                    .WithInput(() => new AnimationInput(
                        IsMoving: Mathf.Abs(Velocity.X) > 0.1f,
                        IsOnFloor: IsOnFloor(),
                        IsOnWall: IsOnWall(),
                        IsJumping: false,
                        IsSliding: false,
                        IsAttacking: IsAttacking,
                        IsDead: IsDead,
                        TookHit: IsHitStunned,
                        VelocityY: Velocity.Y,
                        VelocityX: Velocity.X))
                    .Build();
            }
        }
    }

    protected override void OnPhysicsProcess(float delta)
    {
        if (IsDead)
        {
            return;
        }

        if (_invincibilityTimer > 0f)
        {
            _invincibilityTimer -= delta;
        }

        _gravity.Apply(this, delta);
        _hitStun.Tick(delta);

        if (!IsHitStunned)
        {
            _tick.Tick(this, delta);
        }
        else
        {
            Velocity = Velocity with { X = 0f };
        }

        MoveAndSlide();
    }

    public void TakeDamage(float impact)
    {
        if (IsDead || _invincibilityTimer > 0f)
        {
            return;
        }

        // Always stun and flash so hits feel responsive regardless of which phase absorbs damage.
        _hitStun.Activate(_definition.HitStunSeconds);
        if (_definition.InvincibilitySeconds > 0f)
        {
            _invincibilityTimer = _definition.InvincibilitySeconds;
        }

        if (_damageBehavior is not null && !_damageBehavior.HandleDamage(this, impact))
        {
            EmitSignal(SignalName.HpChanged, CurrentHp, _definition.MaxHp);
            return;
        }

        CurrentHp = MathF.Max(0f, CurrentHp - impact);
        EmitSignal(SignalName.HpChanged, CurrentHp, _definition.MaxHp);

        if (CurrentHp <= 0f)
        {
            TriggerDeath();
        }
    }

    public PlayerController? FindNearestPlayer()
    {
        // Use var to avoid Godot.Collections vs FeralFrenzy.Godot namespace collision.
        var players = GetTree().GetNodesInGroup("players");
        PlayerController? nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Node node in players)
        {
            if (node is not PlayerController player || player.IsDown || player.IsDead)
            {
                continue;
            }

            float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = player;
            }
        }

        return nearest;
    }

    // Exposes ConfigureAnimation to IAnimationSetup implementors (behavior nodes).
    // Call only from IAnimationSetup.Configure() — never after _Ready() completes.
    public AnimationBuilder<TState> BuildAnimation<TState>()
        where TState : struct, Enum
        => ConfigureAnimation<TState>();

    public void NotifyEnemyKilled() => _gameState.NotifyEnemyKilled();

    public void RequestProjectile(Vector2 dir, float speed, float impact)
        => EmitSignal(SignalName.ProjectileSpawnRequested, GlobalPosition, dir, speed, impact);

    public void RequestMinions(string assetKey, Vector2 offset1, Vector2 offset2)
        => EmitSignal(SignalName.MinionSummonRequested, assetKey, offset1, offset2);

    private void TriggerDeath()
    {
        IsDead = true;
        SetPhysicsProcess(false);
        _gameState.NotifyEnemyKilled();
        EmitSignal(SignalName.Died);

        if (_death is not null)
        {
            _death.Execute(this);
        }
        else
        {
            GetTree().CreateTimer(0.3f).Timeout += QueueFree;
        }
    }
}
