using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.UI;

public partial class TitleController : Control
{
    // Initialized in _Ready — Godot does not call _Ready during construction
    private GameStateManager _gameState = null!;
    private AudioStreamPlayer _music = null!;
    private float _musicLoopPoint;

    public override void _Ready()
    {
        _gameState = GetNode<GameStateManager>(AutoloadPaths.GameStateManager);
        _gameState.StateChanged += OnStateChanged;

        AssetRegistry registry = GetNode<AssetRegistry>(AutoloadPaths.AssetRegistry);
        AudioStream? stream = registry.Load<AudioStream>(AssetKeys.MusicTitle);
        _musicLoopPoint = registry.GetLoopPoint(AssetKeys.MusicTitle);

        _music = new AudioStreamPlayer();
        _music.Stream = stream;
        _music.Finished += OnMusicFinished;
        AddChild(_music);

        Visible = _gameState.Current is TitleState;
        if (Visible)
        {
            _music.Play();
        }
    }

    public override void _ExitTree()
    {
        _gameState.StateChanged -= OnStateChanged;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible || _gameState.Current is not TitleState)
        {
            return;
        }

        if (@event is InputEventKey { Pressed: true }
            || @event is InputEventJoypadButton { Pressed: true })
        {
            _gameState.TransitionTo<LoadoutSelectState>();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnStateChanged(GameStateNode from, GameStateNode to)
    {
        bool isTitle = to is TitleState;
        Visible = isTitle;

        if (isTitle)
        {
            _music.Play();
        }
        else
        {
            _music.Stop();
        }
    }

    private void OnMusicFinished()
    {
        _music.Play(_musicLoopPoint);
    }
}
