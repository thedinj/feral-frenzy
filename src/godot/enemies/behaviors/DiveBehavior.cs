using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Enemies.Behaviors;

public partial class DiveBehavior : Node, ITickBehavior
{
    private const float DiveDetectRangeX = 32f;

    [Export]
    public float PatrolSpeed { get; set; } = 60f;

    [Export]
    public float DiveSpeed { get; set; } = 200f;

    [Export]
    public float PatrolHeight { get; set; } = 60f;

    [Export]
    public float GroundY { get; set; } = 152f;

    private enum DiveState
    {
        Patrolling,
        Diving,
        Returning,
    }

    private DiveState _state = DiveState.Patrolling;
    private float _patrolDirection = 1f;
    private float _targetY;
    private bool _initialized;

    public void Tick(EnemyHost host, float delta)
    {
        if (!_initialized)
        {
            _targetY = host.GlobalPosition.Y - PatrolHeight;
            _initialized = true;
        }

        switch (_state)
        {
            case DiveState.Patrolling:
                DoPatrol(host);
                CheckForDive(host);
                break;

            case DiveState.Diving:
                DoDive(host);
                break;

            case DiveState.Returning:
                DoReturn(host);
                break;
        }
    }

    private void DoPatrol(EnemyHost host)
    {
        PlayerController? nearest = host.FindNearestPlayer();
        if (nearest is not null)
        {
            float dir = Mathf.Sign(nearest.GlobalPosition.X - host.GlobalPosition.X);
            if (dir != 0f)
            {
                _patrolDirection = dir;
            }
        }
        else if (host.GlobalPosition.X < 10f || host.GlobalPosition.X > 790f)
        {
            _patrolDirection *= -1f;
        }

        float heightError = _targetY - host.GlobalPosition.Y;
        host.Velocity = new Vector2(PatrolSpeed * _patrolDirection, heightError * 5f);
    }

    private void CheckForDive(EnemyHost host)
    {
        var players = host.GetTree().GetNodesInGroup("players");
        foreach (Node node in players)
        {
            if (node is not PlayerController player || player.IsDown || player.IsDead)
            {
                continue;
            }

            float dx = Mathf.Abs(player.GlobalPosition.X - host.GlobalPosition.X);
            if (dx < DiveDetectRangeX && player.GlobalPosition.Y > host.GlobalPosition.Y)
            {
                _state = DiveState.Diving;
                return;
            }
        }
    }

    private void DoDive(EnemyHost host)
    {
        host.Velocity = new Vector2(host.Velocity.X * 0.8f, DiveSpeed);

        if (host.GlobalPosition.Y >= GroundY)
        {
            _state = DiveState.Returning;
        }
    }

    private void DoReturn(EnemyHost host)
    {
        float heightError = _targetY - host.GlobalPosition.Y;
        host.Velocity = new Vector2(PatrolSpeed * _patrolDirection, heightError * 8f);

        if (Mathf.Abs(heightError) < 4f)
        {
            _state = DiveState.Patrolling;
        }
    }
}
