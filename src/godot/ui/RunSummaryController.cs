using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using Godot;

namespace FeralFrenzy.Godot.UI;

public partial class RunSummaryController : Control
{
    // Initialized in _Ready — Godot does not call _Ready during construction
    private GameStateManager _gameState = null!;

    // Resolved via GetNodeOrNull in _Ready — [Export] Label wiring unreliable in hand-written .tscn
    private Label? _killsLabel;
    private Label? _deathsLabel;
    private Label? _timeLabel;

    public override void _Ready()
    {
        _killsLabel = GetNodeOrNull<Label>("KillsLabel");
        _deathsLabel = GetNodeOrNull<Label>("DeathsLabel");
        _timeLabel = GetNodeOrNull<Label>("TimeLabel");

        _gameState = GetNode<GameStateManager>("/root/GameStateManager");
        _gameState.StateChanged += OnStateChanged;

        Visible = _gameState.Current == GameState.RunSummary;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible || _gameState.Current != GameState.RunSummary)
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
