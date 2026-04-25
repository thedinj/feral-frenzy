using System;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.Enemies.Behaviors;
using FeralFrenzy.Godot.Enemies.Components;
using Godot;

namespace FeralFrenzy.Godot.Enemies;

public partial class EnemyHost : CharacterBody2D
{
    [Export]
    public FFEnemyDefinition? Definition { get; set; }

    [Signal]
    public delegate void HpChangedEventHandler(float current, float max);

    [Signal]
    public delegate void DiedEventHandler();

    [Signal]
    public delegate void ProjectileSpawnRequestedEventHandler(Vector2 dir, float speed, float impact);

    [Signal]
    public delegate void MinionSummonRequestedEventHandler(string assetKey, Vector2 offset1, Vector2 offset2);

    public float CurrentHp { get; set; }

    public bool IsDead { get; private set; }

    public bool IsHitStunned => _hitStun.IsActive;

    // assigned in _Ready()
    private FFEnemyDefinition _definition = null!;
    private GameStateManager _gameState = null!;
    private HitStunComponent _hitStun = null!;
    private ITickBehavior _tick = null!;
    private IGravityBehavior _gravity = null!;
    private IDeathBehavior? _death;
    private IDamageBehavior? _damageBehavior;

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

        Node? gravityNode = GetNodeOrNull<Node>(NodePaths.EnemyGravity)
            ?? throw new InvalidOperationException(
                $"{Name}: missing gravity node '{NodePaths.EnemyGravity}'.");
        _gravity = gravityNode as IGravityBehavior
            ?? throw new InvalidOperationException(
                $"{Name}: '{NodePaths.EnemyGravity}' does not implement IGravityBehavior.");

        Node? deathNode = GetNodeOrNull<Node>(NodePaths.EnemyDeath);
        _death = deathNode as IDeathBehavior;

        AddToGroup("enemies");
    }

    public sealed override void _PhysicsProcess(double delta)
    {
        if (IsDead)
        {
            return;
        }

        float f = (float)delta;
        _gravity.Apply(this, f);
        _hitStun.Tick(f);

        if (!IsHitStunned)
        {
            _tick.Tick(this, f);
        }
        else
        {
            Velocity = Velocity with { X = 0f };
        }

        MoveAndSlide();
    }

    public void TakeDamage(float impact)
    {
        if (IsDead)
        {
            return;
        }

        if (_damageBehavior is not null && !_damageBehavior.HandleDamage(this, impact))
        {
            return;
        }

        CurrentHp = MathF.Max(0f, CurrentHp - impact);
        _hitStun.Activate(_definition.HitStunSeconds);
        EmitSignal(SignalName.HpChanged, CurrentHp, _definition.MaxHp);

        if (CurrentHp <= 0f)
        {
            TriggerDeath();
        }
    }

    public PlayerController? FindNearestPlayer()
    {
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

    public void NotifyEnemyKilled() => _gameState.NotifyEnemyKilled();

    public void RequestProjectile(Vector2 dir, float speed, float impact)
        => EmitSignal(SignalName.ProjectileSpawnRequested, dir, speed, impact);

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
