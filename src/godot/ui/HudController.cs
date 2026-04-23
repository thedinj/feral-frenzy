using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using Godot;

namespace FeralFrenzy.Godot.UI;

public partial class HudController : Control
{
    // Initialized in _Ready — Godot does not call _Ready during construction
    private GameStateManager _gameState = null!;

    // Resolved via GetNodeOrNull in _Ready — [Export] Label wiring unreliable in hand-written .tscn
    private Label? _killLabel;
    private Label? _reviveLabel;

    private float _reviveCountdown;

    public override void _Ready()
    {
        _killLabel = GetNodeOrNull<Label>("KillLabel");
        _reviveLabel = GetNodeOrNull<Label>("ReviveLabel");

        _gameState = GetNode<GameStateManager>("/root/GameStateManager");
        _gameState.StateChanged += OnStateChanged;

        Visible = _gameState.Current is GameState.Segment or GameState.ReviveWindow;

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
