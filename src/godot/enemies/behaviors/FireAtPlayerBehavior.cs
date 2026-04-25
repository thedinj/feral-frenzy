using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Enemies.Behaviors;

public partial class FireAtPlayerBehavior : Node, ITickBehavior
{
    [Export]
    public float FireRange { get; set; } = 140f;

    [Export]
    public float FireRate { get; set; } = 1.8f;

    [Export]
    public float ProjectileSpeed { get; set; } = 180f;

    [Export]
    public float ProjectileImpact { get; set; } = 1f;

    private float _cooldown;
    private ITickBehavior? _fallback;

    public override void _Ready()
    {
        _fallback = GetNodeOrNull<Node>("Patrol") as ITickBehavior;
    }

    public void Tick(EnemyHost host, float delta)
    {
        PlayerController? target = host.FindNearestPlayer();

        if (target is null || host.GlobalPosition.DistanceTo(target.GlobalPosition) > FireRange)
        {
            _fallback?.Tick(host, delta);
            return;
        }

        host.Velocity = host.Velocity with { X = 0f };
        _cooldown -= delta;

        if (_cooldown > 0f)
        {
            return;
        }

        _cooldown = FireRate;
        Vector2 dir = (target.GlobalPosition - host.GlobalPosition).Normalized();
        host.RequestProjectile(dir, ProjectileSpeed, ProjectileImpact);
    }
}
