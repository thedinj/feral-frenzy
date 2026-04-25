using System;
using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Enemies.Behaviors;

public partial class ChargeBehavior : Node, ITickBehavior
{
    [Export]
    public float Speed { get; set; } = 200f;

    [Export]
    public float Duration { get; set; } = 1.5f;

    public void Tick(EnemyHost host, float delta)
    {
    }

    public void BeginCharge(EnemyHost host, Action onComplete)
    {
        PlayerController? target = host.FindNearestPlayer();
        if (target is null)
        {
            onComplete();
            return;
        }

        float dir = Mathf.Sign(target.GlobalPosition.X - host.GlobalPosition.X);
        host.Velocity = host.Velocity with { X = Speed * dir };

        host.GetTree().CreateTimer(Duration).Timeout += () =>
        {
            if (!host.IsDead)
            {
                host.Velocity = host.Velocity with { X = 0f };
            }

            onComplete();
        };
    }
}
