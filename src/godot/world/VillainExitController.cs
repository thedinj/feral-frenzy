using System.Collections.Generic;
using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.World;

public partial class VillainExitController : Control
{
    private const float HoldDuration = 2.0f;

    private Label? _victoryLabel;
    private GameStateManager _gameState = null!;

    public override void _Ready()
    {
        _gameState = GetNode<GameStateManager>(AutoloadPaths.GameStateManager);
        _gameState.StateChanged += OnStateChanged;
        _victoryLabel = GetNodeOrNull<Label>("VictoryLabel");
        Visible = false;
    }

    public override void _ExitTree()
    {
        _gameState.StateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameStateNode from, GameStateNode to)
    {
        Visible = to is VillainExitState;

        if (to is VillainExitState)
        {
            if (_victoryLabel is not null)
            {
                _victoryLabel.Text = "VICTORY!";
            }

            GetTree().CreateTimer(HoldDuration).Timeout += OnExitComplete;
        }
    }

    private void OnExitComplete()
    {
        if (_gameState.Current is VillainExitState)
        {
            _gameState.TransitionTo<RunSummaryState>(
                new RunSummaryPayload(null, true, new List<string>()));
        }
    }
}
