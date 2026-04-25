using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.UI;

public partial class LoadoutSelectController : Control
{
    private const int MaxPlayers = 4;
    private static readonly string[] CharacterNames = { "BEAR", "HONEY BADGER" };

    private readonly int[] _selections = new int[MaxPlayers];
    private readonly bool[] _joined = new bool[MaxPlayers];
    private readonly bool[] _ready = new bool[MaxPlayers];

    // Resolved via GetNodeOrNull in _Ready
    private readonly Label?[] _charLabels = new Label?[MaxPlayers];
    private readonly Label?[] _statusLabels = new Label?[MaxPlayers];

    private GameStateManager _gameState = null!;
    private InputManager _input = null!;

    public override void _Ready()
    {
        _gameState = GetNode<GameStateManager>(AutoloadPaths.GameStateManager);
        _input = GetNode<InputManager>(AutoloadPaths.InputManager);
        _gameState.StateChanged += OnStateChanged;

        _charLabels[0] = GetNodeOrNull<Label>("P1Label");
        _charLabels[1] = GetNodeOrNull<Label>("P2Label");
        _charLabels[2] = GetNodeOrNull<Label>("P3Label");
        _charLabels[3] = GetNodeOrNull<Label>("P4Label");
        _statusLabels[0] = GetNodeOrNull<Label>("P1Status");
        _statusLabels[1] = GetNodeOrNull<Label>("P2Status");
        _statusLabels[2] = GetNodeOrNull<Label>("P3Status");
        _statusLabels[3] = GetNodeOrNull<Label>("P4Status");

        // Keyboard P1 is always joined
        _joined[0] = true;
        _selections[1] = 1;

        Visible = _gameState.Current is LoadoutSelectState;
        RefreshDisplay();
    }

    public override void _ExitTree()
    {
        _gameState.StateChanged -= OnStateChanged;
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible || _gameState.Current is not LoadoutSelectState)
        {
            return;
        }

        HandleKeyboardInput(@event);
        HandleGamepadJoinInput(@event);
        HandleGamepadPlayerInput(@event);
    }

    private void HandleKeyboardInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        if (_ready[0])
        {
            return;
        }

        if (key.IsAction(InputActions.MoveLeft))
        {
            _selections[0] = (_selections[0] + CharacterNames.Length - 1) % CharacterNames.Length;
            RefreshDisplay();
        }
        else if (key.IsAction(InputActions.MoveRight))
        {
            _selections[0] = (_selections[0] + 1) % CharacterNames.Length;
            RefreshDisplay();
        }
        else if (key.IsAction(InputActions.PrimaryAttack))
        {
            _ready[0] = true;
            RefreshDisplay();
            TryStartGame();
        }
    }

    private void HandleGamepadJoinInput(InputEvent @event)
    {
        if (@event is not InputEventJoypadButton joy || !joy.Pressed)
        {
            return;
        }

        int playerIndex = joy.Device + 1;
        if (playerIndex < 1 || playerIndex >= MaxPlayers)
        {
            return;
        }

        if (!_joined[playerIndex])
        {
            // Any button joins
            _joined[playerIndex] = true;
            RefreshDisplay();
            return;
        }

        if (!_ready[playerIndex] && joy.ButtonIndex == JoyButton.Back)
        {
            // Select/Back unjoins before confirming
            _joined[playerIndex] = false;
            RefreshDisplay();
        }
    }

    private void HandleGamepadPlayerInput(InputEvent @event)
    {
        if (@event is not InputEventJoypadButton joy || !joy.Pressed)
        {
            return;
        }

        int playerIndex = joy.Device + 1;
        if (playerIndex < 1 || playerIndex >= MaxPlayers || !_joined[playerIndex] || _ready[playerIndex])
        {
            return;
        }

        switch (joy.ButtonIndex)
        {
            case JoyButton.DpadLeft:
                _selections[playerIndex] = (_selections[playerIndex] + CharacterNames.Length - 1) % CharacterNames.Length;
                RefreshDisplay();
                break;

            case JoyButton.DpadRight:
                _selections[playerIndex] = (_selections[playerIndex] + 1) % CharacterNames.Length;
                RefreshDisplay();
                break;

            case JoyButton.X:
                _ready[playerIndex] = true;
                RefreshDisplay();
                TryStartGame();
                break;
        }
    }

    private void TryStartGame()
    {
        if (!_ready[0])
        {
            return;
        }

        int playerCount = 0;
        for (int i = 0; i < MaxPlayers; i++)
        {
            if (_joined[i] && _ready[i])
            {
                playerCount++;
            }
        }

        _gameState.Player1CharacterIndex = _selections[0];
        _gameState.Player2CharacterIndex = _selections[1];
        _gameState.ActivePlayerCount = playerCount;

        for (int i = 0; i < MaxPlayers; i++)
        {
            _ready[i] = false;
        }

        _gameState.TransitionTo<SegmentState>();
        GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
    }

    private void RefreshDisplay()
    {
        for (int i = 0; i < MaxPlayers; i++)
        {
            if (_charLabels[i] is null && _statusLabels[i] is null)
            {
                continue;
            }

            string charText = _joined[i]
                ? $"P{i + 1}: {CharacterNames[_selections[i]]}"
                : $"P{i + 1}: ---";

            string statusText;
            if (!_joined[i])
            {
                statusText = i == 0 ? "Z to confirm" : "Press any button";
            }
            else if (_ready[i])
            {
                statusText = "READY";
            }
            else
            {
                statusText = i == 0 ? "Z to confirm" : "(X) to confirm";
            }

            if (_charLabels[i] is Label charLabel)
            {
                charLabel.Text = charText;
            }

            if (_statusLabels[i] is Label statusLabel)
            {
                statusLabel.Text = statusText;
            }
        }
    }

    private void OnStateChanged(GameStateNode from, GameStateNode to)
    {
        Visible = to is LoadoutSelectState;

        if (Visible)
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                _ready[i] = false;
                _joined[i] = false;
                _selections[i] = i % CharacterNames.Length;
            }

            _joined[0] = true;

            RefreshDisplay();
        }
    }
}
