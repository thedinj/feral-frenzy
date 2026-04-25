using System;
using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.Enemies.Behaviors;

public partial class PatternCycleBehavior : Node, ITickBehavior
{
    private enum BossPattern
    {
        Charge,
        Burst,
        Summon,
    }

    [Export]
    public float InitialDelay { get; set; } = 2f;

    private PatternCycle<BossPattern>? _cycle;
    private ChargeBehavior? _charge;
    private BurstFireBehavior? _burst;
    private SummonBehavior? _summon;
    private GameStateManager? _gameState;
    private bool _isAttacking;

    public override void _Ready()
    {
        _charge = GetNodeOrNull<ChargeBehavior>("Charge");
        _burst = GetNodeOrNull<BurstFireBehavior>("Burst");
        _summon = GetNodeOrNull<SummonBehavior>("Summon");
        _gameState = GetNode<GameStateManager>(AutoloadPaths.GameStateManager);

        _cycle = new PatternCycle<BossPattern>(
            InitialDelay,
            (BossPattern.Charge, 1.5f),
            (BossPattern.Burst, 1.5f),
            (BossPattern.Summon, 2.0f));
    }

    public void Tick(EnemyHost host, float delta)
    {
        if (_gameState?.Current is not BossFightState)
        {
            host.Velocity = host.Velocity with { X = 0f };
            return;
        }

        _cycle?.Tick(delta, pattern =>
        {
            if (_isAttacking)
            {
                return false;
            }

            ExecutePattern(host, pattern);
            return true;
        });
    }

    private void ExecutePattern(EnemyHost host, BossPattern pattern)
    {
        switch (pattern)
        {
            case BossPattern.Charge:
                _isAttacking = true;
                _charge?.BeginCharge(host, () => _isAttacking = false);
                break;

            case BossPattern.Burst:
                _burst?.FireBurst(host);
                break;

            case BossPattern.Summon:
                _summon?.Summon(host);
                break;
        }
    }
}
