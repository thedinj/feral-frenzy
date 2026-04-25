using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.World;
using Godot;

namespace FeralFrenzy.Godot.Enemies.Behaviors;

public partial class DownwardGravity : Node, IGravityBehavior
{
    public void Apply(EnemyHost host, float delta)
    {
        if (!host.IsOnFloor())
        {
            float g = LevelController.Instance?.EffectiveGravity ?? PhysicsConstants.Gravity;
            host.Velocity = host.Velocity with { Y = host.Velocity.Y + (g * delta) };
        }
    }
}
