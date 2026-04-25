namespace FeralFrenzy.Godot.Enemies.Behaviors;

public interface ITickBehavior
{
    void Tick(EnemyHost host, float delta);
}
