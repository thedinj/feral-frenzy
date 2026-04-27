using System;
using System.Collections.Generic;
using FeralFrenzy.Godot.Animation;
using Godot;

namespace FeralFrenzy.Godot.Core;

/// <summary>
/// Base class for Area2D entities (flying enemies, triggers, pickups).
/// Same subsystem model as GameEntity.
/// </summary>
public abstract partial class GameArea : Area2D
{
    private readonly List<IEntitySubsystem> _subsystems = new List<IEntitySubsystem>();

    protected void RegisterSubsystem(IEntitySubsystem subsystem)
        => _subsystems.Add(subsystem);

    protected AnimationBuilder<TState> ConfigureAnimation<TState>()
        where TState : struct, Enum
        => new AnimationBuilder<TState>(RegisterSubsystem);

    public sealed override void _PhysicsProcess(double delta)
    {
        OnPhysicsProcess((float)delta);
        foreach (IEntitySubsystem subsystem in _subsystems)
        {
            subsystem.Tick();
        }
    }

    protected abstract void OnPhysicsProcess(float delta);
}
