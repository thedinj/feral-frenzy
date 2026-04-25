using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.UI;

public partial class BossIntroController : Control
{
    private const float IntroDuration = 2.5f;

    private Label? _titleLabel;
    private GameStateManager _gameState = null!;

    public override void _Ready()
    {
        _gameState = GetNode<GameStateManager>(AutoloadPaths.GameStateManager);
        _gameState.StateChanged += OnStateChanged;
        _titleLabel = GetNodeOrNull<Label>("TitleLabel");
        Visible = false;
    }

    public override void _ExitTree()
    {
        _gameState.StateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameStateNode from, GameStateNode to)
    {
        Visible = to is BossIntroState;

        if (to is BossIntroState)
        {
            if (_titleLabel is not null)
            {
                _titleLabel.Text = "VILLAIN REX";
            }

            GetTree().CreateTimer(IntroDuration).Timeout += OnIntroComplete;
        }
    }

    private void OnIntroComplete()
    {
        if (_gameState.Current is BossIntroState)
        {
            _gameState.TransitionTo<BossFightState>(
                new BossFightPayload("villain_rex", "chapter_cretaceous"));
        }
    }
}
