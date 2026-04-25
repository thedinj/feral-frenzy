namespace FeralFrenzy.Core.Data.Engine;

public record CinematicPayload(
    string CinematicKey,
    string ReturnStateKey,
    StatePayload? ReturnPayload,
    bool Skippable = true) : StatePayload;
