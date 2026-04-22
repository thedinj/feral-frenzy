using System;
using Godot;

namespace FeralFrenzy.Godot.Enemies;

public partial class EnemyController : CharacterBody2D
{
    private const float Gravity = 600f;

    [Export]
    public FFEnemyDefinition? Definition { get; set; }

    protected float Health { get; private set; }

    protected bool IsDead { get; private set; }

    public override void _Ready()
    {
        if (Definition is null)
        {
            throw new InvalidOperationException(
                $"{nameof(EnemyController)} '{Name}': Definition not assigned.");
        }

        Health = 3f; // Phase 1 placeholder — Definition will hold HP in Phase 2
        OnReady();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead)
        {
            return;
        }

        if (!IsOnFloor())
        {
            Velocity = Velocity with { Y = Velocity.Y + (Gravity * (float)delta) };
        }

        MoveAndSlide();
    }

    public virtual void TakeDamage(float impact)
    {
        if (IsDead)
        {
            return;
        }

        Health -= impact;
        if (Health <= 0f)
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
        NotifyKilled();
        QueueFree(); // Phase 1 placeholder — death animation: Phase 2
    }

    private void NotifyKilled()
    {
        if (GetNode<Node>("/root/GameStateManager") is Autoloads.GameStateManager gm)
        {
            gm.NotifyEnemyKilled();
        }
    }
}
