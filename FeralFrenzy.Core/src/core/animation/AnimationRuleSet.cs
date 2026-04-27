using System.Collections.Generic;

namespace FeralFrenzy.Core.Animation;

public class AnimationRuleSet<T>
where T : struct, System.Enum
{
    private readonly List<AnimationRule<T>> _rules;
    private readonly T _defaultState;

    public AnimationRuleSet(T defaultState, IEnumerable<AnimationRule<T>> rules)
    {
        _defaultState = defaultState;
        _rules = new List<AnimationRule<T>>(rules);
    }

    /// <summary>
    /// Evaluates rules top-to-bottom. First matching rule wins.
    /// Returns defaultState if no rule matches.
    /// </summary>
    public T Evaluate(T current, AnimationInput input)
    {
        foreach (AnimationRule<T> rule in _rules)
        {
            if (rule.Condition(current, input))
            {
                return rule.TargetState;
            }
        }

        return _defaultState;
    }
}
