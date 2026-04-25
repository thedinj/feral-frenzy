#pragma warning disable SA1402 // all simple state classes grouped in one file
using System;
using System.Collections.Generic;

namespace FeralFrenzy.Godot.Autoloads;

public sealed class TitleState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(AttractState),
        typeof(LoadoutSelectState),
        typeof(LevelEditorState),
        typeof(CreditsState),
        typeof(WorkshopBrowserState),
    };
}

public sealed class AttractState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(TitleState),
    };
}

public sealed class CinematicState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(SegmentState),
        typeof(BossIntroState),
        typeof(BossFightState),
        typeof(VillainExitState),
        typeof(RunSummaryState),
        typeof(TitleState),
    };
}

public sealed class BossIntroState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(BossFightState),
    };
}

public sealed class BossFightState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(SegmentRestartState),
        typeof(VillainExitState),
    };
}

public sealed class VillainExitState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(CinematicState),
        typeof(GradiusLevelState),
        typeof(BrawlerLevelState),
        typeof(SegmentState),
        typeof(RunSummaryState),
    };
}

public sealed class GradiusLevelState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(CinematicState),
        typeof(RunSummaryState),
    };
}

public sealed class BrawlerLevelState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(CinematicState),
        typeof(RunSummaryState),
    };
}

public sealed class RunSummaryState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(TitleState),
        typeof(LoadoutSelectState),
        typeof(CreditsState),
    };
}

public sealed class LevelEditorState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(TitleState),
    };
}

public sealed class CreditsState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(TitleState),
        typeof(RunSummaryState),
    };
}

public sealed class WorkshopBrowserState : GameStateNode
{
    public override IReadOnlySet<Type> LegalTargets { get; } = new HashSet<Type>
    {
        typeof(TitleState),
    };
}
#pragma warning restore SA1402
