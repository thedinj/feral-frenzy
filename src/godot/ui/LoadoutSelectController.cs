using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.UI;

public partial class LoadoutSelectController : Control
{
    private static readonly string[] CharacterNames = { "BEAR", "HONEY BADGER" };

    private int _p1Selection;
    private int _p2Selection = 1;
    private bool _p1Ready;
    private bool _p2Ready;

    // Resolved via GetNodeOrNull in _Ready — [Export] node-type wiring unreliable in hand-written .tscn
    private Label? _p1CharacterLabel;
    private Label? _p2CharacterLabel;
    private Label? _p1StatusLabel;
    private Label? _p2StatusLabel;

    // Initialized in _Ready — Godot does not call _Ready during construction
    private GameStateManager _gameState = null!;

    public override void _Ready()
    {
        _gameState = GetNode<GameStateManager>("/root/GameStateManager");
        _gameState.StateChanged += OnStateChanged;

        _p1CharacterLabel = GetNodeOrNull<Label>("P1Label");
        _p2CharacterLabel = GetNodeOrNull<Label>("P2Label");
        _p1StatusLabel = GetNodeOrNull<Label>("P1Status");
        _p2StatusLabel = GetNodeOrNull<Label>("P2Status");

        Visible = _gameState.Current == GameState.LoadoutSelect;
        RefreshDisplay();
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible || _gameState.Current != GameState.LoadoutSelect)
        {
            return;
        }

        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            HandleP1KeyEvent(key);
        }
        else if (@event is InputEventJoypadButton joy && joy.Pressed
            && joy.Device == InputConstants.GamepadDevice)
        {
            HandleP2JoyEvent(joy);
        }
    }

    private void HandleP1KeyEvent(InputEventKey key)
    {
        if (_p1Ready)
        {
            return;
        }

        if (key.IsAction(InputActions.MoveLeft))
        {
            _p1Selection = (_p1Selection + CharacterNames.Length - 1) % CharacterNames.Length;
            RefreshDisplay();
        }
        else if (key.IsAction(InputActions.MoveRight))
        {
            _p1Selection = (_p1Selection + 1) % CharacterNames.Length;
            RefreshDisplay();
        }
        else if (key.IsAction(InputActions.PrimaryAttack))
        {
            _p1Ready = true;
            RefreshDisplay();
            TryStartGame();
        }
    }

    private void HandleP2JoyEvent(InputEventJoypadButton joy)
    {
        if (_p2Ready)
        {
            return;
        }

        switch (joy.ButtonIndex)
        {
            case JoyButton.DpadLeft:
                _p2Selection = (_p2Selection + CharacterNames.Length - 1) % CharacterNames.Length;
                RefreshDisplay();
                break;

            case JoyButton.DpadRight:
                _p2Selection = (_p2Selection + 1) % CharacterNames.Length;
                RefreshDisplay();
                break;

            case JoyButton.X:
                _p2Ready = true;
                RefreshDisplay();
                TryStartGame();
                break;
        }
    }

    private void TryStartGame()
    {
        if (!_p1Ready)
        {
            return;
        }

        // P2 joins by confirming — a connected gamepad alone does not force 2-player mode.
        int playerCount = _p2Ready ? 2 : 1;

        _gameState.Player1CharacterIndex = _p1Selection;
        _gameState.Player2CharacterIndex = _p2Selection;
        _gameState.ActivePlayerCount = playerCount;

        _p1Ready = false;
        _p2Ready = false;

        _gameState.TransitionTo(GameState.Segment);
    }

    private void RefreshDisplay()
    {
        if (_p1CharacterLabel is not null)
        {
            _p1CharacterLabel.Text = CharacterNames[_p1Selection];
        }

        if (_p2CharacterLabel is not null)
        {
            _p2CharacterLabel.Text = CharacterNames[_p2Selection];
        }

        if (_p1StatusLabel is not null)
        {
            _p1StatusLabel.Text = _p1Ready ? "READY" : "Z to confirm";
        }

        if (_p2StatusLabel is not null)
        {
            _p2StatusLabel.Text = _p2Ready ? "READY" : "(X) to confirm";
        }
    }

    private void OnStateChanged(long from, long to)
    {
        Visible = (GameState)to == GameState.LoadoutSelect;
        if (Visible)
        {
            _p1Ready = false;
            _p2Ready = false;
            RefreshDisplay();
        }
    }
}
