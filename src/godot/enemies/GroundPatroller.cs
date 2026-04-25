using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Enemies;

public partial class GroundPatroller : EnemyController
{
    [Export]
    private float _patrolSpeed = 40f;

    [Export]
    private float _fireRange = 140f;

    [Export]
    private float _fireRate = 1.8f;

    private float _patrolDirection = 1f;
    private float _fireCooldown;
    private PlayerController? _target;

    protected override void OnReady()
    {
        AddToGroup("enemies");
    }

    protected override void TickBehavior(float delta)
    {
        _target = FindNearestPlayer();

        if (_target is not null)
        {
            float dist = GlobalPosition.DistanceTo(_target.GlobalPosition);
            if (dist < _fireRange)
            {
                TryFire(delta);
                return;
            }

            float dir = Mathf.Sign(_target.GlobalPosition.X - GlobalPosition.X);
            if (dir != 0f)
            {
                _patrolDirection = dir;
            }
        }

        MoveInPatrolDirection();
    }

    private void MoveInPatrolDirection()
    {
        Velocity = Velocity with { X = _patrolSpeed * _patrolDirection };
        FlipDirectionOnWall(ref _patrolDirection);
    }

    private void TryFire(float delta)
    {
        Velocity = Velocity with { X = 0f };
        _fireCooldown -= delta;

        if (_fireCooldown > 0f)
        {
            return;
        }

        _fireCooldown = _fireRate;
        Vector2 direction = (_target!.GlobalPosition - GlobalPosition).Normalized();
        SpawnEnemyProjectile(direction, speed: 180f, impact: 1f);
    }
}
