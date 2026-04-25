using System;
using System.Collections.Generic;
using FeralFrenzy.Core.Data.Engine;

namespace FeralFrenzy.Godot.Autoloads;

public sealed class SegmentState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(SegmentRestartState),
        typeof(BossIntroState),
        typeof(GradiusLevelState),
        typeof(BrawlerLevelState),
        typeof(CinematicState),
        typeof(SegmentState),
        typeof(RunSummaryState),
    };

    public override void OnEnter(GameStateContext ctx, GameStateNode from, StatePayload? payload)
    {
        if (payload is SegmentPayload segPayload)
        {
            ctx.ActiveSegmentPayload = segPayload;
        }
    }
}
