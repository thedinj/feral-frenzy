namespace FeralFrenzy.Core.Data.Engine;

public record BossFightPayload(
    string VillainKey,
    string ChapterKey) : StatePayload;
