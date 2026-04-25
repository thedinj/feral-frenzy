using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Enemies;

public partial class PteroBomber : EnemyController
{
    private const float BoundaryLeft = 10f;
    private const float BoundaryRight = 790f;
    private const float OverPlayerXThreshold = 24f;

    [Export]
    private float _patrolSpeed = 60f;

    [Export]
    private float _dropCooldown = 3f;

    private float _dropTimer;
    private float _patrolDirection = 1f;

    protected override bool UseGravity => false;

    protected override void OnHitStunned() => Velocity = Vector2.Zero;

    protected override void OnReady()
    {
        AddToGroup("enemies");
        _dropTimer = _dropCooldown;
    }

    protected override void TickBehavior(float delta)
    {
        Velocity = new Vector2(_patrolSpeed * _patrolDirection, 0f);

        if (IsOnWall() || GlobalPosition.X < BoundaryLeft || GlobalPosition.X > BoundaryRight)
        {
            _patrolDirection *= -1f;
        }

        _dropTimer -= delta;
        if (_dropTimer <= 0f && IsOverPlayer())
        {
            SpawnEnemyProjectile(Vector2.Down, speed: 200f, impact: 1f);
            _dropTimer = _dropCooldown;
        }
    }

    private bool IsOverPlayer()
    {
        PlayerController? nearest = FindNearestPlayer();
        if (nearest is null)
        {
            return false;
        }

        return Mathf.Abs(nearest.GlobalPosition.X - GlobalPosition.X) < OverPlayerXThreshold;
    }
}
