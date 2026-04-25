using FeralFrenzy.Core.Data.Engine;

namespace FeralFrenzy.Godot.Autoloads;

public sealed class GameStateContext
{
    public int KillCount { get; set; }

    public int DeathCount { get; set; }

    public float RunTimeSeconds { get; set; }

    public bool WipedFromBossFight { get; set; }

    public StatePayload? ActiveSegmentPayload { get; set; }
}
