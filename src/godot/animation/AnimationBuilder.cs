using System;
using System.Collections.Generic;
using FeralFrenzy.Core.Animation;
using FeralFrenzy.Godot.Core;
using Godot;

namespace FeralFrenzy.Godot.Animation;

/// <summary>
/// Fluent builder for animation driver configuration.
/// Obtained via GameEntity.ConfigureAnimation&lt;TState&gt;().
/// Call .Build() to construct, initialize, and register the driver.
/// </summary>
public class AnimationBuilder<TState>
where TState : struct, Enum
{
    private readonly Action<IEntitySubsystem> _register;

    private Func<AnimationInput>? _inputBuilder;
    private Func<TState, AnimationInput, TState>? _evaluate;
    private List<TState>? _oneShotStates;
    private Dictionary<TState, string>? _clipNames;
    private TState _defaultState;
    private AnimatedSprite2D? _sprite;
    private AnimationPlayer? _animPlayer;

    internal AnimationBuilder(Action<IEntitySubsystem> register)
        => _register = register;

    /// <summary>
    /// Use for simple enemies — AnimatedSprite2D rendering path.
    /// </summary>
    public AnimationBuilder<TState> WithSprite(AnimatedSprite2D sprite)
    {
        _sprite = sprite;
        return this;
    }

    /// <summary>
    /// Use for characters and boss — AnimationPlayer rendering path.
    /// Clips must be hand-keyed in the Godot editor by the developer.
    /// </summary>
    public AnimationBuilder<TState> WithAnimationPlayer(AnimationPlayer animPlayer)
    {
        _animPlayer = animPlayer;
        return this;
    }

    /// <summary>
    /// Declarative rule list. Rules evaluated top-to-bottom, first match wins.
    /// </summary>
    public AnimationBuilder<TState> WithRules(
        TState defaultState,
        IEnumerable<AnimationRule<TState>> rules)
    {
        _defaultState = defaultState;
        _evaluate = new AnimationRuleSet<TState>(defaultState, rules).Evaluate;
        return this;
    }

    /// <summary>
    /// Escape hatch for complex state logic that doesn't fit a linear rule list.
    /// Use for boss, MountedDino, or any entity with non-linear transitions.
    /// The evaluator may capture controller state via closure.
    /// </summary>
    public AnimationBuilder<TState> WithCustomEvaluator(
        TState defaultState,
        Func<TState, AnimationInput, TState> evaluate)
    {
        _defaultState = defaultState;
        _evaluate = evaluate;
        return this;
    }

    /// <summary>
    /// States that cannot be interrupted until their animation finishes.
    /// </summary>
    public AnimationBuilder<TState> WithOneShots(IEnumerable<TState> states)
    {
        _oneShotStates = new List<TState>(states);
        return this;
    }

    /// <summary>
    /// Maps state enum values to clip names. Must cover all reachable states.
    /// </summary>
    public AnimationBuilder<TState> WithClips(Dictionary<TState, string> clipNames)
    {
        _clipNames = clipNames;
        return this;
    }

    /// <summary>
    /// Closure that reads entity state and produces AnimationInput.
    /// Called once per physics frame by the driver.
    /// </summary>
    public AnimationBuilder<TState> WithInput(Func<AnimationInput> inputBuilder)
    {
        _inputBuilder = inputBuilder;
        return this;
    }

    /// <summary>
    /// Constructs the AnimationStateMachine and AnimationDriver,
    /// and registers the driver as an entity subsystem.
    /// Call once at the end of _Ready(). Never call again.
    /// </summary>
    public void Build()
    {
        if (_evaluate is null)
        {
            throw new InvalidOperationException(
                "AnimationBuilder: WithRules or WithCustomEvaluator must be called before Build.");
        }

        if (_clipNames is null)
        {
            throw new InvalidOperationException(
                "AnimationBuilder: WithClips must be called before Build.");
        }

        if (_inputBuilder is null)
        {
            throw new InvalidOperationException(
                "AnimationBuilder: WithInput must be called before Build.");
        }

        if (_sprite is null && _animPlayer is null)
        {
            throw new InvalidOperationException(
                "AnimationBuilder: WithSprite or WithAnimationPlayer must be called before Build.");
        }

        AnimationStateMachine<TState> stateMachine = new AnimationStateMachine<TState>(
            initialState: _defaultState,
            evaluate: _evaluate,
            oneShotStates: _oneShotStates);

        AnimationDriver<TState> driver = new AnimationDriver<TState>();
        driver.Initialize(
            stateMachine: stateMachine,
            clipNames: _clipNames,
            inputBuilder: _inputBuilder,
            sprite: _sprite,
            animPlayer: _animPlayer);

        _register(driver);
    }
}
