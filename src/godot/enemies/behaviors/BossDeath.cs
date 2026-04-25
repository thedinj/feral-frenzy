using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.Enemies.Behaviors;

public partial class BossDeath : Node, IDeathBehavior
{
    public void Execute(EnemyHost host)
    {
        host.GetTree().CreateTimer(1.0f).Timeout += () =>
        {
            GameStateManager? gsm = host.GetNodeOrNull<GameStateManager>(AutoloadPaths.GameStateManager);
            if (gsm?.Current is BossFightState)
            {
                gsm.TransitionTo<VillainExitState>();
            }
        };
    }
}
