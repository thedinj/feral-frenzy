using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Enemies.Behaviors;

public partial class BombDropBehavior : Node, ITickBehavior
{
    private const float OverPlayerXThreshold = 24f;

    [Export]
    public float PatrolSpeed { get; set; } = 60f;

    [Export]
    public float DropCooldown { get; set; } = 3f;

    [Export]
    public float BoundaryLeft { get; set; } = 10f;

    [Export]
    public float BoundaryRight { get; set; } = 790f;

    private float _dropTimer;
    private float _patrolDirection = 1f;
    private bool _initialized;

    public void Tick(EnemyHost host, float delta)
    {
        if (!_initialized)
        {
            _dropTimer = DropCooldown;
            _initialized = true;
        }

        host.Velocity = new Vector2(PatrolSpeed * _patrolDirection, 0f);

        if (host.IsOnWall()
            || host.GlobalPosition.X < BoundaryLeft
            || host.GlobalPosition.X > BoundaryRight)
        {
            _patrolDirection *= -1f;
        }

        _dropTimer -= delta;
        if (_dropTimer <= 0f && IsOverPlayer(host))
        {
            host.RequestProjectile(Vector2.Down, 200f, 1f);
            _dropTimer = DropCooldown;
        }
    }

    private static bool IsOverPlayer(EnemyHost host)
    {
        PlayerController? nearest = host.FindNearestPlayer();
        if (nearest is null)
        {
            return false;
        }

        return Mathf.Abs(nearest.GlobalPosition.X - host.GlobalPosition.X) < OverPlayerXThreshold;
    }
}
