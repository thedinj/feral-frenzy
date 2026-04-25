using System;
using System.Collections.Generic;

namespace FeralFrenzy.Godot.Enemies;

public sealed class PatternCycle<T>
{
    private readonly (T Pattern, float Pause)[] _steps;
    private int _index;
    private float _timer;

    public PatternCycle(float initialDelay, params (T Pattern, float Pause)[] steps)
    {
        _steps = steps;
        _timer = initialDelay;
    }

    // Ticks the timer. On expiry, calls tryExecute with the current pattern.
    // true  → pattern fired; advance to next step.
    // false → blocked or vetoed; reset timer and retry same step next tick.
    public void Tick(float delta, Func<T, bool> tryExecute)
    {
        _timer -= delta;
        if (_timer > 0f)
        {
            return;
        }

        if (tryExecute(_steps[_index].Pattern))
        {
            _index = (_index + 1) % _steps.Length;
        }

        _timer = _steps[_index].Pause;
    }

    // Convenience overload for unconditional patterns.
    public void Tick(float delta, Action<T> execute)
    {
        Tick(delta, p =>
        {
            execute(p);
            return true;
        });
    }

    // Jump to a specific pattern — for phase transitions or reactive interrupts.
    // Fires on the next tick (timer = 0).
    public void ForceTo(T pattern)
    {
        for (int i = 0; i < _steps.Length; i++)
        {
            if (EqualityComparer<T>.Default.Equals(_steps[i].Pattern, pattern))
            {
                _index = i;
                _timer = 0f;
                return;
            }
        }
    }
}
