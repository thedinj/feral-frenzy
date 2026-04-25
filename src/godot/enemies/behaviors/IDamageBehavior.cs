namespace FeralFrenzy.Godot.Enemies.Behaviors;

public interface IDamageBehavior
{
    /// <returns>True to continue applying damage to host HP; false to absorb it.</returns>
    bool HandleDamage(EnemyHost host, float impact);
}
