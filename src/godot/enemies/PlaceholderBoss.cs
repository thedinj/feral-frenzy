using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.World;
using Godot;

namespace FeralFrenzy.Godot.Enemies;

public partial class PlaceholderBoss : EnemyController
{
    private const float BurstAngleStep = 0.3f;
    private const int BurstCount = 5;
    private const int SummonCount = 2;
    private const float SummonOffsetX = 40f;

    private enum BossPattern
    {
        Charge,
        Burst,
        Summon,
    }

    private readonly PatternCycle<BossPattern> _cycle = new PatternCycle<BossPattern>(
        initialDelay: 2f,
        (BossPattern.Charge, 1.5f),
        (BossPattern.Burst, 1.5f),
        (BossPattern.Summon, 2.0f));

    [Export]
    private float _chargeSpeed = 200f;

    private bool _isAttacking;
    private GameStateManager _gameState2 = null!;

    protected override void OnReady()
    {
        AddToGroup("enemies");
        AddToGroup("bosses");
        _gameState2 = GetNode<GameStateManager>(AutoloadPaths.GameStateManager);
    }

    public override void TakeDamage(float impact)
    {
        base.TakeDamage(impact);
        EmitSignal(EnemyController.SignalName.HpChanged, Mathf.Max(0f, CurrentHp), Definition!.MaxHp);
    }

    protected override void TickBehavior(float delta)
    {
        if (_gameState2.Current is not BossFightState)
        {
            Velocity = Velocity with { X = 0f };
            return;
        }

        _cycle.Tick(delta, pattern =>
        {
            if (_isAttacking)
            {
                return false;
            }

            ExecutePattern(pattern);
            return true;
        });
    }

    protected override void Die()
    {
        IsDead = true;
        SetPhysicsProcess(false);
        SetProcess(false);

        GetTree().CreateTimer(1.0f).Timeout += OnDeathPauseComplete;
    }

    private void OnDeathPauseComplete()
    {
        if (_gameState2.Current is BossFightState)
        {
            _gameState2.TransitionTo<VillainExitState>();
        }
    }

    private void ExecutePattern(BossPattern pattern)
    {
        switch (pattern)
        {
            case BossPattern.Charge:
                DoCharge();
                break;

            case BossPattern.Burst:
                DoBurst();
                break;

            case BossPattern.Summon:
                DoSummon();
                break;
        }
    }

    private void DoCharge()
    {
        PlayerController? target = FindNearestPlayer();
        if (target is null)
        {
            return;
        }

        _isAttacking = true;
        float dir = Mathf.Sign(target.GlobalPosition.X - GlobalPosition.X);
        Velocity = Velocity with { X = _chargeSpeed * dir };

        GetTree().CreateTimer(1.5f).Timeout += () =>
        {
            if (!IsDead)
            {
                Velocity = Velocity with { X = 0f };
            }

            _isAttacking = false;
        };
    }

    private void DoBurst()
    {
        PlayerController? target = FindNearestPlayer();
        if (target is null)
        {
            return;
        }

        Vector2 baseDir = (target.GlobalPosition - GlobalPosition).Normalized();

        for (int i = -(BurstCount / 2); i <= BurstCount / 2; i++)
        {
            Vector2 dir = baseDir.Rotated(i * BurstAngleStep);
            SpawnEnemyProjectile(dir, speed: 200f, impact: 1f);
        }
    }

    private void DoSummon()
    {
        EntityPool pool = GetNode<EntityPool>(AutoloadPaths.EntityPool);

        for (int i = 0; i < SummonCount; i++)
        {
            var minion = pool.Get<CharacterBody2D>(AssetKeys.SceneEnemyGroundPatroller);
            float offsetX = i == 0 ? -SummonOffsetX : SummonOffsetX;
            minion.GlobalPosition = GlobalPosition + new Vector2(offsetX, 0f);
            LevelController.Instance?.AddToEntities(minion);
        }
    }
}
