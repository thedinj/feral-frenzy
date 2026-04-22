using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using Godot;

namespace FeralFrenzy.Godot.UI;

public partial class HudController : Control
{
    [Export]
    private Label? _killLabel;

    [Export]
    private Label? _reviveLabel;

    [Export]
    private float _reviveCountdown;

    // Initialized in _Ready — Godot does not call _Ready during construction
    private GameStateManager _gameState = null!;

    public override void _Ready()
    {
        _gameState = GetNode<GameStateManager>("/root/GameStateManager");
        _gameState.StateChanged += OnStateChanged;
        Visible = _gameState.Current == GameState.Segment
            || _gameState.Current == GameState.ReviveWindow;

        if (_reviveLabel is not null)
        {
            _reviveLabel.Visible = false;
        }
    }

    public override void _Process(double delta)
    {
        if (!Visible)
        {
            return;
        }

        if (_killLabel is not null)
        {
            _killLabel.Text = $"Kills: {_gameState.KillCount}";
        }

        if (_gameState.Current == GameState.ReviveWindow)
        {
            _reviveCountdown -= (float)delta;
            if (_reviveLabel is not null)
            {
                _reviveLabel.Text = $"REVIVE! {Mathf.Max(0f, _reviveCountdown):F0}s";
            }
        }
    }

    private void OnStateChanged(long from, long to)
    {
        GameState newState = (GameState)to;
        Visible = newState is GameState.Segment or GameState.ReviveWindow or GameState.SegmentRestart;

        if (newState == GameState.ReviveWindow)
        {
            _reviveCountdown = 10f;
            if (_reviveLabel is not null)
            {
                _reviveLabel.Visible = true;
            }
        }
        else
        {
            if (_reviveLabel is not null)
            {
                _reviveLabel.Visible = false;
            }
        }
    }
}
