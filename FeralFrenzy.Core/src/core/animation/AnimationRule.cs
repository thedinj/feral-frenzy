using System;

namespace FeralFrenzy.Core.Animation;

public record AnimationRule<T>(
    Func<T, AnimationInput, bool> Condition,
    T TargetState
)
where T : struct, Enum;
