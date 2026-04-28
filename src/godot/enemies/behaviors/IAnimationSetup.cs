namespace FeralFrenzy.Godot.Enemies.Behaviors;

public interface IAnimationSetup
{
    // Called from EnemyHost._Ready() after behavior node is resolved.
    // Implementor calls host.BuildAnimation<TState>()...Build().
    // Responsible for its own null checks on sprite / animPlayer nodes.
    void Configure(EnemyHost host);
}
