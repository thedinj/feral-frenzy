using Godot;

namespace FeralFrenzy.Godot.Enemies.Behaviors;

public partial class PatrolBehavior : Node, ITickBehavior
{
    [Export]
    public float Speed { get; set; } = 40f;

    private float _dir = 1f;

    public void Tick(EnemyHost host, float delta)
    {
        host.Velocity = host.Velocity with { X = Speed * _dir };
        if (host.IsOnWall())
        {
            _dir *= -1f;
        }
    }
}
