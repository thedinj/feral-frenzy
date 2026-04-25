namespace FeralFrenzy.Godot.Enemies.Behaviors;

public interface IGravityBehavior
{
    void Apply(EnemyHost host, float delta);
}
