using System;
using FeralFrenzy.Core.Data.Engine;

namespace FeralFrenzy.Godot.Autoloads;

public interface IAutoTransition
{
    float GetDelay(StatePayload? payload);

    (Type Next, StatePayload? Payload)? SelectNext(GameStateContext ctx);
}
