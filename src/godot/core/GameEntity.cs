using System;
using System.Collections.Generic;
using FeralFrenzy.Godot.Animation;
using Godot;

namespace FeralFrenzy.Godot.Core;

/// <summary>
/// Base class for all CharacterBody2D entities.
/// Owns _PhysicsProcess — subclasses implement OnPhysicsProcess.
/// Guarantees subsystems tick after game logic, in registration order.
/// </summary>
public abstract partial class GameEntity : CharacterBody2D
{
    private readonly List<IEntitySubsystem> _subsystems = new List<IEntitySubsystem>();

    protected void RegisterSubsystem(IEntitySubsystem subsystem)
        => _subsystems.Add(subsystem);

    /// <summary>
    /// Entry point for the fluent animation builder.
    /// Called in _Ready(). Never called after _Ready().
    /// </summary>
    protected AnimationBuilder<TState> ConfigureAnimation<TState>()
        where TState : struct, Enum
        => new AnimationBuilder<TState>(RegisterSubsystem);

    /// <summary>
    /// Sealed — subclasses cannot override. Implement OnPhysicsProcess instead.
    /// </summary>
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
