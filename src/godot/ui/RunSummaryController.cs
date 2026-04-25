using System.Collections.Generic;
using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
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
    private Label? _playAgainLabel;
    private Label? _quitLabel;

    private bool _playAgainSelected = true;

    public override void _Ready()
    {
        _killsLabel = GetNodeOrNull<Label>("KillsLabel");
        _deathsLabel = GetNodeOrNull<Label>("DeathsLabel");
        _timeLabel = GetNodeOrNull<Label>("TimeLabel");
        _playAgainLabel = GetNodeOrNull<Label>("PlayAgainLabel");
        _quitLabel = GetNodeOrNull<Label>("QuitLabel");

        _gameState = GetNode<GameStateManager>(AutoloadPaths.GameStateManager);
        _gameState.StateChanged += OnStateChanged;

        Visible = _gameState.Current is RunSummaryState;
    }

    public override void _ExitTree()
    {
        _gameState.StateChanged -= OnStateChanged;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible || _gameState.Current is not RunSummaryState)
        {
            return;
        }

        if (IsNavUp(@event))
        {
            _playAgainSelected = true;
            UpdateCursor();
            GetViewport().SetInputAsHandled();
        }
        else if (IsNavDown(@event))
        {
            _playAgainSelected = false;
            UpdateCursor();
            GetViewport().SetInputAsHandled();
        }
        else if (IsConfirm(@event))
        {
            if (_playAgainSelected)
            {
                _gameState.TransitionTo<LoadoutSelectState>();
            }
            else
            {
                _gameState.TransitionTo<TitleState>();
            }

            GetViewport().SetInputAsHandled();
        }
    }

    private static bool IsNavUp(InputEvent @event)
    {
        return (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Up)
            || (@event is InputEventJoypadButton btn && btn.Pressed
                && btn.ButtonIndex == JoyButton.DpadUp);
    }

    private static bool IsNavDown(InputEvent @event)
    {
        return (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Down)
            || (@event is InputEventJoypadButton btn && btn.Pressed
                && btn.ButtonIndex == JoyButton.DpadDown);
    }

    private static bool IsConfirm(InputEvent @event)
    {
        return (@event is InputEventKey key && key.Pressed
                && (key.Keycode == Key.Enter || key.Keycode == Key.Space
                    || key.Keycode == Key.Z || key.Keycode == Key.X))
            || (@event is InputEventJoypadButton btn && btn.Pressed
                && (btn.ButtonIndex == JoyButton.A || btn.ButtonIndex == JoyButton.Start));
    }

    private void OnStateChanged(GameStateNode from, GameStateNode to)
    {
        Visible = to is RunSummaryState;

        if (!Visible)
        {
            return;
        }

        _playAgainSelected = true;
        UpdateCursor();

        if (_killsLabel is not null)
        {
            _killsLabel.Text = $"Enemies Defeated: {_gameState.KillCount}";
        }

        if (_deathsLabel is not null)
        {
            _deathsLabel.Text = $"Times Wiped: {_gameState.DeathCount}";
        }

        if (_timeLabel is not null)
        {
            int minutes = (int)_gameState.RunTimeSeconds / 60;
            int seconds = (int)_gameState.RunTimeSeconds % 60;
            _timeLabel.Text = $"Time: {minutes:D2}:{seconds:D2}";
        }
    }

    private void UpdateCursor()
    {
        if (_playAgainLabel is not null)
        {
            _playAgainLabel.Text = _playAgainSelected ? "> Play Again" : "  Play Again";
        }

        if (_quitLabel is not null)
        {
            _quitLabel.Text = _playAgainSelected ? "  Quit" : "> Quit";
        }
    }
}
