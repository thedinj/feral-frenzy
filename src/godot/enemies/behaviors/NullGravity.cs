using Godot;

namespace FeralFrenzy.Godot.Enemies.Behaviors;

public partial class NullGravity : Node, IGravityBehavior
{
    public void Apply(EnemyHost host, float delta)
    {
    }
}
