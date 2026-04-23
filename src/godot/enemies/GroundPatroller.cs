using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.World;
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

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead)
        {
            return;
        }

        base._PhysicsProcess(delta);

        _target = FindNearestPlayer();

        if (_target is not null)
        {
            float dist = GlobalPosition.DistanceTo(_target.GlobalPosition);
            if (dist < _fireRange)
            {
                TryFire((float)delta);
            }
            else
            {
                ChaseTarget(_target);
            }
        }
        else
        {
            Patrol();
        }
    }

    protected override void OnReady()
    {
        AddToGroup("enemies");
    }

    private void ChaseTarget(PlayerController target)
    {
        float dir = Mathf.Sign(target.GlobalPosition.X - GlobalPosition.X);
        if (dir != 0f)
        {
            _patrolDirection = dir;
        }

        Velocity = Velocity with { X = _patrolSpeed * _patrolDirection };
        if (IsOnWall())
        {
            _patrolDirection *= -1f;
        }
    }

    private void Patrol()
    {
        Velocity = Velocity with { X = _patrolSpeed * _patrolDirection };
        if (IsOnWall())
        {
            _patrolDirection *= -1f;
        }
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
        SpawnProjectileToward(_target!.GlobalPosition);
    }

    private void SpawnProjectileToward(Vector2 targetPos)
    {
        PackedScene? scene = GetNode<AssetRegistry>("/root/AssetRegistry")
            .GetScene(AssetKeys.SceneProjectile);

        if (scene is null)
        {
            return;
        }

        Weapons.ProjectileController projectile = scene.Instantiate<Weapons.ProjectileController>();
        projectile.GlobalPosition = GlobalPosition;

        Vector2 direction = (targetPos - GlobalPosition).Normalized();
        projectile.Initialize(direction, speed: 180f, impact: 1f, collisionMask: 2u); // layer 2 = players

        if (LevelController.Instance is not null)
        {
            LevelController.Instance.AddToEntities(projectile);
        }
    }

    private PlayerController? FindNearestPlayer()
    {
        var players = GetTree().GetNodesInGroup("players");
        PlayerController? nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Node node in players)
        {
            if (node is not PlayerController player || player.IsDown || player.IsDead)
            {
                continue;
            }

            float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = player;
            }
        }

        return nearest;
    }
}
