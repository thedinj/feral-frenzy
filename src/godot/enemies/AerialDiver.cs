using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Enemies;

public partial class AerialDiver : EnemyController
{
    private const float DiveDetectRangeX = 32f;
    private const float GroundY = 152f; // approximate ground level

    [Export]
    private float _patrolSpeed = 60f;

    [Export]
    private float _diveSpeed = 200f;

    [Export]
    private float _patrolHeight = 60f; // y offset above ground level

    private enum DiveState
    {
        Patrolling,
        Diving,
        Returning,
    }

    private DiveState _diveState = DiveState.Patrolling;
    private float _patrolDirection = 1f;
    private float _targetY;

    protected override void OnReady()
    {
        AddToGroup("enemies");
        _targetY = GlobalPosition.Y - _patrolHeight;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead)
        {
            return;
        }

        // Aerial diver is not affected by gravity — it flies
        switch (_diveState)
        {
            case DiveState.Patrolling:
                DoPatrol((float)delta);
                CheckForDive();
                break;

            case DiveState.Diving:
                DoDive((float)delta);
                break;

            case DiveState.Returning:
                DoReturn((float)delta);
                break;
        }

        MoveAndSlide();
    }

    private void DoPatrol(float delta)
    {
        Velocity = new Vector2(_patrolSpeed * _patrolDirection, 0f);

        // Bounce at screen edges (rough bounds for Phase 1)
        if (GlobalPosition.X < 10f || GlobalPosition.X > 790f)
        {
            _patrolDirection *= -1f;
        }

        // Maintain patrol height
        float heightError = _targetY - GlobalPosition.Y;
        Velocity = Velocity with { Y = heightError * 5f };
    }

    private void CheckForDive()
    {
        var players = GetTree().GetNodesInGroup("players");
        foreach (Node node in players)
        {
            if (node is not PlayerController player || player.IsDown || player.IsDead)
            {
                continue;
            }

            float dx = Mathf.Abs(player.GlobalPosition.X - GlobalPosition.X);
            if (dx < DiveDetectRangeX && player.GlobalPosition.Y > GlobalPosition.Y)
            {
                _diveState = DiveState.Diving;
                return;
            }
        }
    }

    private void DoDive(float delta)
    {
        Velocity = new Vector2(Velocity.X * 0.8f, _diveSpeed);

        if (GlobalPosition.Y >= GroundY)
        {
            _diveState = DiveState.Returning;
        }
    }

    private void DoReturn(float delta)
    {
        float heightError = _targetY - GlobalPosition.Y;
        Velocity = new Vector2(_patrolSpeed * _patrolDirection, heightError * 8f);

        if (Mathf.Abs(heightError) < 4f)
        {
            _diveState = DiveState.Patrolling;
        }
    }
}
