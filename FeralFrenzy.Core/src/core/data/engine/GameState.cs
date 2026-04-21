namespace FeralFrenzy.Core.Data.Engine;

public enum GameState
{
    // Top-level modes
    Title,
    Attract,
    LoadoutSelect,

    // Run spine states
    Segment,
    BossIntro,
    BossFight,
    VillainExit,
    GradiusLevel,
    BrawlerLevel,

    // Nested / transient (within Segment or BossFight)
    ReviveWindow,
    SegmentRestart,

    // Cinematic (reentrant)
    Cinematic,

    // Post-run
    RunSummary,

    // Utility / standalone
    LevelEditor,
    Credits,
    WorkshopBrowser,
}
