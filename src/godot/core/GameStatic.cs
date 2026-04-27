using System;
using System.Collections.Generic;
using FeralFrenzy.Godot.Animation;
using Godot;

namespace FeralFrenzy.Godot.Core;

/// <summary>
/// Base class for StaticBody2D entities (destructible geometry, moving platforms).
/// </summary>
public abstract partial class GameStatic : StaticBody2D
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
