using System.Collections.Generic;

namespace FeralFrenzy.Core.Data.Engine;

public record RunSummaryPayload(
    RunData? CompletedRun,
    bool RunCompleted,
    List<string> UnlocksEarned) : StatePayload;
