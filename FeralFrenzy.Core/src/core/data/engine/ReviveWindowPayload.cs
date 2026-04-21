namespace FeralFrenzy.Core.Data.Engine;

public record ReviveWindowPayload(
    int DownPlayerIndex,
    float ReviveWindowSeconds) : StatePayload;
