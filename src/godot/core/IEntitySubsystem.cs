namespace FeralFrenzy.Godot.Core;

/// <summary>
/// A system that ticks after OnPhysicsProcess each frame.
/// Registered via GameEntity.RegisterSubsystem().
/// Registration order determines tick order.
/// Animation driver must always be registered last.
/// </summary>
public interface IEntitySubsystem
{
    void Tick();
}
