using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Enemies.Behaviors;

public partial class BurstFireBehavior : Node, ITickBehavior
{
    [Export]
    public int Count { get; set; } = 5;

    [Export]
    public float AngleStep { get; set; } = 0.3f;

    [Export]
    public float ProjectileSpeed { get; set; } = 200f;

    [Export]
    public float ProjectileImpact { get; set; } = 1f;

    public void Tick(EnemyHost host, float delta)
    {
    }

    public void FireBurst(EnemyHost host)
    {
        PlayerController? target = host.FindNearestPlayer();
        if (target is null)
        {
            return;
        }

        Vector2 baseDir = (target.GlobalPosition - host.GlobalPosition).Normalized();

        for (int i = -(Count / 2); i <= Count / 2; i++)
        {
            Vector2 dir = baseDir.Rotated(i * AngleStep);
            host.RequestProjectile(dir, ProjectileSpeed, ProjectileImpact);
        }
    }
}
