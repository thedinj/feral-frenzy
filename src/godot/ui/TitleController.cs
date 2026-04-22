using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using Godot;

namespace FeralFrenzy.Godot.UI;

public partial class TitleController : Control
{
    // Initialized in _Ready — Godot does not call _Ready during construction
    private GameStateManager _gameState = null!;

    public override void _Ready()
    {
        _gameState = GetNode<GameStateManager>("/root/GameStateManager");
        _gameState.StateChanged += OnStateChanged;
        Visible = _gameState.Current == GameState.Title;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible)
        {
            return;
        }

        if (_gameState.Current != GameState.Title)
        {
            return;
        }

        if (@event is InputEventKey { Pressed: true }
            || @event is InputEventJoypadButton { Pressed: true })
        {
            _gameState.TransitionTo(GameState.LoadoutSelect);
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnStateChanged(long from, long to)
    {
        Visible = (GameState)to == GameState.Title;
    }
}
