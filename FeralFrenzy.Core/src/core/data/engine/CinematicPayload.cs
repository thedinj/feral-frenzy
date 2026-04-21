namespace FeralFrenzy.Core.Data.Engine;

public record CinematicPayload(
    string CinematicKey,
    GameState ReturnState,
    StatePayload? ReturnPayload,
    bool Skippable = true) : StatePayload;
