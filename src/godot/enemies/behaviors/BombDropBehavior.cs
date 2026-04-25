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
    private bool _timerFiredOnce;

    public void Tick(EnemyHost host, float delta)
    {
        if (!_initialized)
        {
            _dropTimer = DropCooldown;
            _initialized = true;
        }

        if (host.IsOnWall()
            || (host.GlobalPosition.X < BoundaryLeft && _patrolDirection < 0f)
            || (host.GlobalPosition.X > BoundaryRight && _patrolDirection > 0f))
        {
            _patrolDirection *= -1f;
        }

        host.Velocity = new Vector2(PatrolSpeed * _patrolDirection, 0f);

        _dropTimer -= delta;
        if (_dropTimer <= 0f)
        {
            if (!_timerFiredOnce)
            {
                _timerFiredOnce = true;
            }

            PlayerController? nearest = host.FindNearestPlayer();
            if (nearest is null)
            {
            }
            else
            {
                float xDiff = Mathf.Abs(nearest.GlobalPosition.X - host.GlobalPosition.X);

                if (xDiff < OverPlayerXThreshold)
                {
                    host.RequestProjectile(Vector2.Down, 200f, 1f);
                    _dropTimer = DropCooldown;
                    _timerFiredOnce = false;
                }
            }
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
