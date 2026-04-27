using System;
using System.Collections.Generic;

namespace FeralFrenzy.Core.Animation;

public class AnimationStateMachine<T>
where T : struct, Enum
{
    public T Current { get; private set; }

    public T Previous { get; private set; }

    public bool JustTransitioned { get; private set; }

    private readonly Func<T, AnimationInput, T> _evaluate;
    private readonly HashSet<T> _oneShotStates;
    private readonly HashSet<T> _finishedOneShots = new HashSet<T>();

    public AnimationStateMachine(
        T initialState,
        Func<T, AnimationInput, T> evaluate,
        IEnumerable<T>? oneShotStates = null)
    {
        Current = initialState;
        Previous = initialState;
        _evaluate = evaluate;
        _oneShotStates = oneShotStates is null
            ? new HashSet<T>()
            : new HashSet<T>(oneShotStates);
    }

    public T Update(AnimationInput input)
    {
        JustTransitioned = false;

        T next = _evaluate(Current, input);

        // Block transition out of a one-shot until it signals finished
        if (_oneShotStates.Contains(Current) &&
            !_finishedOneShots.Contains(Current) &&
            !EqualityComparer<T>.Default.Equals(next, Current))
        {
            return Current;
        }

        if (!EqualityComparer<T>.Default.Equals(next, Current))
        {
            Previous = Current;
            Current = next;
            JustTransitioned = true;
            _finishedOneShots.Remove(Previous);
        }

        return Current;
    }

    /// <summary>
    /// Called by AnimationDriver when a one-shot animation finishes.
    /// Allows the state machine to transition out of the finished state.
    /// </summary>
    public void NotifyFinished(T state) => _finishedOneShots.Add(state);

    /// <summary>
    /// Forcibly resets to the given state. Use for respawn/restart only.
    /// </summary>
    public void Reset(T state)
    {
        Current = state;
        Previous = state;
        JustTransitioned = false;
        _finishedOneShots.Clear();
    }
}
