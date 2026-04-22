using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using Godot;

namespace FeralFrenzy.Godot.UI;

public partial class MainController : Node
{
    // Initialized in _Ready — Godot does not call _Ready during construction
    private GameStateManager _gameState = null!;

    public override void _Ready()
    {
        _gameState = GetNode<GameStateManager>("/root/GameStateManager");

        // Start in Title state — no transition needed, just ensure state is correct.
        // GameStateManager defaults to Title on construction.
    }
}
