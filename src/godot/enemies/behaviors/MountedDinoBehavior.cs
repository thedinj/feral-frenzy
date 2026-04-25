using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Enemies.Behaviors;

public partial class MountedDinoBehavior : Node, ITickBehavior, IDamageBehavior
{
    private enum Phase
    {
        Rider,
        Dino,
    }

    [Export]
    public float RiderMaxHp { get; set; } = 3f;

    [Export]
    public float DinoMaxHp { get; set; } = 2f;

    [Export]
    public float ChargeSpeed { get; set; } = 140f;

    [Export]
    public float ShootRange { get; set; } = 200f;

    [Export]
    public float FireRate { get; set; } = 2.0f;

    private Phase _phase = Phase.Rider;
    private float _riderCurrentHp;
    private float _fireCooldown;
    private float _chargeDirection = 1f;
    private int _wallBounceCount;
    private bool _initialized;

    public bool HandleDamage(EnemyHost host, float impact)
    {
        if (_phase == Phase.Rider)
        {
            if (!_initialized)
            {
                _riderCurrentHp = RiderMaxHp;
                _initialized = true;
            }

            _riderCurrentHp -= impact;
            host.GetNodeOrNull<Components.HitStunComponent>("../HitStun")?.Activate(0.15f);

            if (_riderCurrentHp <= 0f)
            {
                host.NotifyEnemyKilled();
                _phase = Phase.Dino;
                host.CurrentHp = DinoMaxHp;
            }

            return false;
        }

        return true;
    }

    public void Tick(EnemyHost host, float delta)
    {
        if (!_initialized)
        {
            _riderCurrentHp = RiderMaxHp;
            _initialized = true;
        }

        switch (_phase)
        {
            case Phase.Rider:
                DoRiderBehavior(host, delta);
                break;

            case Phase.Dino:
                DoChargeBehavior(host);
                break;
        }
    }

    private void DoRiderBehavior(EnemyHost host, float delta)
    {
        host.Velocity = host.Velocity with { X = 0f };

        _fireCooldown -= delta;
        if (_fireCooldown > 0f)
        {
            return;
        }

        PlayerController? target = host.FindNearestPlayer();
        if (target is null || host.GlobalPosition.DistanceTo(target.GlobalPosition) > ShootRange)
        {
            return;
        }

        _fireCooldown = FireRate;
        Vector2 direction = (target.GlobalPosition - host.GlobalPosition).Normalized();
        host.RequestProjectile(direction, 200f, 1f);
    }

    private void DoChargeBehavior(EnemyHost host)
    {
        host.Velocity = host.Velocity with { X = ChargeSpeed * _chargeDirection };

        if (host.IsOnWall())
        {
            _chargeDirection *= -1f;
            _wallBounceCount++;

            if (_wallBounceCount >= 2)
            {
                host.Velocity = host.Velocity with { X = 0f };
            }
        }
    }
}
