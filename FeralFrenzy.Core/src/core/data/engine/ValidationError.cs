namespace FeralFrenzy.Core.Data.Engine;

public record ValidationError(
    string SegmentId,
    string Rule,
    string Message);
