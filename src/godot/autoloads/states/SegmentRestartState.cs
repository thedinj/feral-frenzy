using System;
using System.Collections.Generic;
using FeralFrenzy.Core.Data.Engine;

namespace FeralFrenzy.Godot.Autoloads;

public sealed class SegmentRestartState : GameStateNode, IAutoTransition
{
    private const float RestartDelay = 1.5f;

    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(SegmentState),
        typeof(BossFightState),
    };

    public override void OnEnter(GameStateContext ctx, GameStateNode from, StatePayload? payload)
    {
        if (from is BossFightState)
        {
            ctx.WipedFromBossFight = true;
        }
    }

    public float GetDelay(StatePayload? payload) => RestartDelay;

    public (Type Next, StatePayload? Payload)? SelectNext(GameStateContext ctx)
    {
        if (ctx.WipedFromBossFight)
        {
            ctx.WipedFromBossFight = false;
            return (typeof(BossFightState), null);
        }

        StatePayload? payload = ctx.ActiveSegmentPayload is SegmentPayload seg
            ? new SegmentPayload(seg.Segment, IsRestart: true)
            : null;

        return (typeof(SegmentState), payload);
    }
}
