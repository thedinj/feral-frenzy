using FeralFrenzy.Core.Data.Engine;
using Godot;

namespace FeralFrenzy.Godot.Autoloads;

public partial class GameStateManager : Node
{
    public GameState Current { get; private set; } = GameState.Title;

    [Signal]
    public delegate void StateChangedEventHandler(long from, long to);

    // Phase 0 stub — no transition validation, full implementation: docs/02_state_machine.md
    public void TransitionTo(GameState next, StatePayload? payload = null)
    {
        long previous = (long)Current;
        Current = next;
        EmitSignal(SignalName.StateChanged, previous, (long)next);
    }
}
