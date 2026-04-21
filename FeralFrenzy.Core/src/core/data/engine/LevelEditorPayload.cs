namespace FeralFrenzy.Core.Data.Engine;

public record LevelEditorPayload(
    SegmentData? PreloadedSegment = null) : StatePayload;
