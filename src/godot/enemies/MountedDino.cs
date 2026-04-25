using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Enemies;

public partial class MountedDino : EnemyController
{
    private enum MountedDinoState
    {
        RiderActive,
        DinoCharging,
    }

    [Export]
    private float _riderMaxHp = 3f;

    [Export]
    private float _dinoMaxHp = 2f;

    [Export]
    private float _chargeSpeed = 140f;

    [Export]
    private float _shootRange = 200f;

    [Export]
    private float _fireRate = 2.0f;

    private MountedDinoState _state = MountedDinoState.RiderActive;
    private float _riderCurrentHp;
    private float _fireCooldown;
    private float _chargeDirection = 1f;
    private int _wallBounceCount;

    // Dino coasts through hit stun — no velocity clearing
    protected override void OnHitStunned()
    {
    }

    protected override void OnReady()
    {
        _riderCurrentHp = _riderMaxHp;
        AddToGroup("enemies");
    }

    public override void TakeDamage(float impact)
    {
        if (IsDead)
        {
            return;
        }

        if (_state == MountedDinoState.RiderActive)
        {
            _riderCurrentHp -= impact;
            IsHitStunned = true;
            PlayHitFlash();

            if (_riderCurrentHp <= 0f)
            {
                NotifyEnemyKilled();
                _state = MountedDinoState.DinoCharging;
                CurrentHp = _dinoMaxHp;
            }
        }
        else
        {
            base.TakeDamage(impact);
        }
    }

    protected override void TickBehavior(float delta)
    {
        switch (_state)
        {
            case MountedDinoState.RiderActive:
                DoRiderBehavior(delta);
                break;

            case MountedDinoState.DinoCharging:
                DoChargeBehavior();
                break;
        }
    }

    private void DoRiderBehavior(float delta)
    {
        Velocity = Velocity with { X = 0f };

        _fireCooldown -= delta;
        if (_fireCooldown > 0f)
        {
            return;
        }

        PlayerController? target = FindNearestPlayer();
        if (target is null)
        {
            return;
        }

        float dist = GlobalPosition.DistanceTo(target.GlobalPosition);
        if (dist > _shootRange)
        {
            return;
        }

        _fireCooldown = _fireRate;
        Vector2 direction = (target.GlobalPosition - GlobalPosition).Normalized();
        SpawnEnemyProjectile(direction, speed: 200f, impact: 1f);
    }

    private void DoChargeBehavior()
    {
        Velocity = Velocity with { X = _chargeSpeed * _chargeDirection };

        if (IsOnWall())
        {
            _chargeDirection *= -1f;
            _wallBounceCount++;

            if (_wallBounceCount >= 2)
            {
                Velocity = Velocity with { X = 0f };
            }
        }
    }
}
