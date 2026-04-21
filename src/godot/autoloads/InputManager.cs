using Godot;

namespace FeralFrenzy.Godot.Autoloads;

public partial class InputManager : Node
{
    // Phase 0 stub — player 1 only, keyboard only; full implementation: Phase 1, multi-device routing for 1–4 players
    public bool IsActionPressed(int playerIndex, string action)
    {
        if (playerIndex != 0)
        {
            return false;
        }

        return Input.IsActionPressed(action);
    }

    public bool IsActionJustPressed(int playerIndex, string action)
    {
        if (playerIndex != 0)
        {
            return false;
        }

        return Input.IsActionJustPressed(action);
    }

    public float GetAxis(int playerIndex, string negativeAction, string positiveAction)
    {
        if (playerIndex != 0)
        {
            return 0f;
        }

        return Input.GetAxis(negativeAction, positiveAction);
    }
}
