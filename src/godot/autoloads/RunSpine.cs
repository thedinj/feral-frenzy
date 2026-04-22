using System.Collections.Generic;
using FeralFrenzy.Core.Data.Engine;
using Godot;

namespace FeralFrenzy.Godot.Autoloads;

public partial class RunSpine : Node
{
    // Phase 1 stub — one hardcoded segment, no generation.
    // Full implementation: Phase 3.
    public (GameState NextState, StatePayload? Payload) Advance()
        => (GameState.RunSummary, new RunSummaryPayload(null, true, new List<string>()));
}
