namespace FeralFrenzy.Core.Data.Engine;

public record SegmentPayload(
    SegmentData Segment,
    bool IsRestart) : StatePayload;
