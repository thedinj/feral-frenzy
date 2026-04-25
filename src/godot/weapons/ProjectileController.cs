using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.Enemies;
using Godot;

namespace FeralFrenzy.Godot.Weapons;

public partial class ProjectileController : Area2D, IPlayerProjectile
{
    private const float MaxDistance = 800f;

    private Vector2 _direction;
    private float _speed;
    private float _impact;
    private float _travelledDistance;
    private ProjectileOwner _owner;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public void InitializeFromWeapon(Vector2 direction, float speed, float impact, PlayerController? firedBy)
        => Initialize(direction, speed, impact, ProjectileOwner.Player);

    public void Initialize(Vector2 direction, float speed, float impact, ProjectileOwner owner)
    {
        _direction = direction;
        _speed = speed;
        _impact = impact;
        _owner = owner;

        CollisionMask = owner == ProjectileOwner.Player
            ? LayerMasks.Enemies
            : LayerMasks.Players;
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
        switch (_owner)
        {
            case ProjectileOwner.Player when body is EnemyHost enemy:
                enemy.TakeDamage(_impact);
                break;
            case ProjectileOwner.Enemy when body is PlayerController player:
                player.TakeDamage();
                break;
            default:
                return;
        }

        QueueFree();
    }
}
