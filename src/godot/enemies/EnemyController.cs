using System;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.Weapons;
using FeralFrenzy.Godot.World;
using Godot;

namespace FeralFrenzy.Godot.Enemies;

public abstract partial class EnemyController : CharacterBody2D
{
    [Export]
    public FFEnemyDefinition? Definition { get; set; }

    [Signal]
    public delegate void HpChangedEventHandler(float current, float max);

    protected float CurrentHp { get; set; }

    protected bool IsDead { get; set; }

    protected bool IsHitStunned { get; set; }

    private float _hitStunTimer;
    private GameStateManager _gameState = null!;
    private AssetRegistry _registry = null!;

    public override void _Ready()
    {
        if (Definition is null)
        {
            throw new InvalidOperationException(
                $"{nameof(EnemyController)} '{Name}': Definition not assigned.");
        }

        _gameState = GetNode<GameStateManager>(AutoloadPaths.GameStateManager);
        _registry = GetNode<AssetRegistry>(AutoloadPaths.AssetRegistry);
        CurrentHp = Definition.MaxHp;
        OnReady();
    }

    public sealed override void _PhysicsProcess(double delta)
    {
        if (IsDead)
        {
            return;
        }

        TickHitStun((float)delta);

        if (UseGravity && !IsOnFloor())
        {
            Velocity = Velocity with { Y = Velocity.Y + (PhysicsConstants.Gravity * (float)delta) };
        }

        if (!IsHitStunned)
        {
            TickBehavior((float)delta);
        }
        else
        {
            OnHitStunned();
        }

        MoveAndSlide();
    }

    protected abstract void TickBehavior(float delta);

    protected virtual bool UseGravity => true;

    protected virtual void OnHitStunned() => Velocity = Velocity with { X = 0f };

    protected void FlipDirectionOnWall(ref float direction)
    {
        if (IsOnWall())
        {
            direction *= -1f;
        }
    }

    public virtual void TakeDamage(float impact)
    {
        if (IsDead)
        {
            return;
        }

        CurrentHp -= impact;
        IsHitStunned = true;
        _hitStunTimer = Definition!.HitStunSeconds;

        PlayHitFlash();

        if (CurrentHp <= 0f)
        {
            Die();
        }
    }

    protected virtual void OnReady()
    {
    }

    protected virtual void Die()
    {
        IsDead = true;

        _gameState.NotifyEnemyKilled();

        AnimatedSprite2D? sprite = GetNodeOrNull<AnimatedSprite2D>(NodePaths.AnimatedSprite);

        if (sprite is not null && sprite.SpriteFrames?.HasAnimation(AnimationNames.Death) == true)
        {
            sprite.SpriteFrames.SetAnimationLoop(AnimationNames.Death, false);
            sprite.Play(AnimationNames.Death);
            sprite.AnimationFinished += () => QueueFree();
        }
        else
        {
            CanvasItem target = sprite ?? (CanvasItem)this;
            Tween tween = GetTree().CreateTween();
            tween.TweenProperty(target, "modulate:a", 0f, 0.3f);
            tween.TweenCallback(Callable.From(QueueFree));
        }
    }

    // For subclasses that have multi-phase death (e.g. rider + dino) and need to
    // report the first phase kill without going through Die().
    protected void NotifyEnemyKilled() => _gameState.NotifyEnemyKilled();

    protected void TickHitStun(float delta)
    {
        if (!IsHitStunned)
        {
            return;
        }

        _hitStunTimer -= delta;
        if (_hitStunTimer <= 0f)
        {
            IsHitStunned = false;
        }
    }

    protected void PlayHitFlash()
    {
        AnimatedSprite2D? sprite = GetNodeOrNull<AnimatedSprite2D>(NodePaths.AnimatedSprite);
        if (sprite is null)
        {
            return;
        }

        sprite.Modulate = Colors.White * 2f;
        GetTree().CreateTimer(0.08f).Timeout += () => sprite.Modulate = Colors.White;
    }

    protected PlayerController? FindNearestPlayer()
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

    protected void SpawnEnemyProjectile(Vector2 direction, float speed, float impact)
    {
        PackedScene? scene = _registry.GetScene(AssetKeys.SceneProjectile);
        if (scene is null)
        {
            return;
        }

        ProjectileController projectile = scene.Instantiate<ProjectileController>();
        projectile.GlobalPosition = GlobalPosition;
        projectile.Initialize(direction, speed, impact, ProjectileOwner.Enemy);
        LevelController.Instance?.AddToEntities(projectile);
    }
}
