#pragma warning disable SA1402 // grouped per docs/02_state_machine.md: all SpineStep variants defined together as a single architectural unit
namespace FeralFrenzy.Core.Data.Engine;

public abstract record SpineStep;

public record PlaySegmentStep(SegmentData Segment) : SpineStep;

public record PlayBossStep(string VillainKey, string ChapterKey) : SpineStep;

public record PlayGenreLevelStep(GameState GenreLevel) : SpineStep;

public record PlayCinematicStep(string CinematicKey) : SpineStep;

public record EndRunStep : SpineStep;
#pragma warning restore SA1402
