using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Enemies;
using Godot;

namespace FeralFrenzy.Godot.Weapons;

public partial class ProjectileController : Area2D
{
    private const float MaxDistance = 800f;

    private Vector2 _direction;
    private float _speed;
    private float _impact;
    private float _travelledDistance;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public void Initialize(Vector2 direction, float speed, float impact, uint collisionMask)
    {
        _direction = direction;
        _speed = speed;
        _impact = impact;
        CollisionMask = collisionMask;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 movement = _direction * _speed * (float)delta;
        Position += movement;
        _travelledDistance += movement.Length();

        if (_travelledDistance >= MaxDistance)
        {
            QueueFree();
        }
    }

    private void OnBodyEntered(Node body)
    {
        if (body is EnemyController enemy)
        {
            enemy.TakeDamage(_impact);
            QueueFree();
        }
        else if (body is PlayerController player)
        {
            player.TakeDamage();
            QueueFree();
        }
    }
}
