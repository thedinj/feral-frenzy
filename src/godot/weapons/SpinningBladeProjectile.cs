using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.Enemies;
using Godot;

namespace FeralFrenzy.Godot.Weapons;

public partial class SpinningBladeProjectile : Area2D, IPlayerProjectile
{
    private const float Speed = 320f;
    private const float MaxDistance = 600f;
    private const float ReturnCatchRadius = 16f;
    private const float RotationSpeed = 12f;
    private const int MaxPenetrations = 3;

    private Vector2 _direction;
    private float _impact;
    private float _travelledDistance;
    private bool _returning;
    private int _penetrationCount;
    private PlayerController? _owner;

    public override void _Ready()
    {
        CollisionLayer = LayerMasks.PlayerProjectiles;
        CollisionMask = LayerMasks.Enemies;
        BodyEntered += OnBodyEntered;
    }

    public void InitializeFromWeapon(Vector2 direction, float speed, float impact, PlayerController? firedBy)
    {
        if (firedBy is not null)
        {
            Initialize(direction, impact, firedBy);
        }
    }

    public void Initialize(Vector2 direction, float impact, PlayerController owner)
    {
        _direction = direction.Normalized();
        _impact = impact;
        _owner = owner;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (_returning)
        {
            if (!IsInstanceValid(_owner) || _owner!.IsDown || _owner.IsDead)
            {
                QueueFree();
                return;
            }

            _direction = (_owner.GlobalPosition - GlobalPosition).Normalized();

            if (GlobalPosition.DistanceTo(_owner.GlobalPosition) < ReturnCatchRadius)
            {
                QueueFree();
                return;
            }
        }

        Vector2 movement = _direction * Speed * dt;
        Position += movement;
        Rotation += RotationSpeed * dt;
        _travelledDistance += movement.Length();

        if (!_returning && _travelledDistance >= MaxDistance)
        {
            _returning = true;
        }
    }

    private void OnBodyEntered(Node body)
    {
        if (body is not EnemyHost enemy)
        {
            return;
        }

        enemy.TakeDamage(_impact);
        _penetrationCount++;

        if (_penetrationCount >= MaxPenetrations)
        {
            QueueFree();
        }
    }
}
