using System;
using System.Collections.Generic;
using FeralFrenzy.Core.Data.Engine;

namespace FeralFrenzy.Godot.Autoloads;

public abstract class GameStateNode
{
    public abstract IReadOnlySet<Type> LegalTargets { get; }

    public virtual void OnEnter(GameStateContext ctx, GameStateNode from, StatePayload? payload)
    {
    }

    public virtual void OnExit(GameStateContext ctx)
    {
    }
}
