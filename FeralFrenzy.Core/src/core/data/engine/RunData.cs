using System.Collections.Generic;

namespace FeralFrenzy.Core.Data.Engine;

public record RunData(
    string RunId,
    int Seed,
    string SchemaVersion,
    List<SegmentData> Segments,
    bool HasSurpriseDestructible);
