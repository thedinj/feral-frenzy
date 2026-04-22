using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using Godot;

namespace FeralFrenzy.Godot.UI;

public partial class RunSummaryController : Control
{
    [Export]
    private Label? _killsLabel;

    [Export]
    private Label? _deathsLabel;

    [Export]
    private Label? _timeLabel;

    // Initialized in _Ready — Godot does not call _Ready during construction
    private GameStateManager _gameState = null!;

    public override void _Ready()
    {
        _gameState = GetNode<GameStateManager>("/root/GameStateManager");
        _gameState.StateChanged += OnStateChanged;
        Visible = _gameState.Current == GameState.RunSummary;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible)
        {
            return;
        }

        if (_gameState.Current != GameState.RunSummary)
        {
            return;
        }

        if (@event is InputEventKey { Pressed: true }
            || @event is InputEventJoypadButton { Pressed: true })
        {
            _gameState.TransitionTo(GameState.Title);
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnStateChanged(long from, long to)
    {
        Visible = (GameState)to == GameState.RunSummary;

        if (!Visible)
        {
            return;
        }

        if (_killsLabel is not null)
        {
            _killsLabel.Text = $"Kills: {_gameState.KillCount}";
        }

        if (_deathsLabel is not null)
        {
            _deathsLabel.Text = $"Deaths: {_gameState.DeathCount}";
        }

        if (_timeLabel is not null)
        {
            int minutes = (int)_gameState.RunTimeSeconds / 60;
            int seconds = (int)_gameState.RunTimeSeconds % 60;
            _timeLabel.Text = $"Time: {minutes}:{seconds:D2}";
        }
    }
}
