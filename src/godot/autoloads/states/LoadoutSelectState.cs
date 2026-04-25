using System;
using System.Collections.Generic;
using FeralFrenzy.Core.Data.Engine;

namespace FeralFrenzy.Godot.Autoloads;

public sealed class LoadoutSelectState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(TitleState),
        typeof(CinematicState),
        typeof(SegmentState),
    };

    public override void OnEnter(GameStateContext ctx, GameStateNode from, StatePayload? payload)
    {
        ctx.KillCount = 0;
        ctx.DeathCount = 0;
        ctx.RunTimeSeconds = 0f;
        ctx.WipedFromBossFight = false;
    }
}
