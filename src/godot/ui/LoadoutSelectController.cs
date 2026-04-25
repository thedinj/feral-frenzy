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

        // Gamepad P1 is always joined; keyboard P2 is optional
        _joined[InputConstants.GamepadPlayerIndex] = true;

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

    // Maps a Godot joypad device index to a player index, skipping the keyboard slot.
    private static int DeviceToPlayerIndex(int device) =>
        device < InputConstants.KeyboardPlayerIndex ? device : device + 1;

    private void HandleKeyboardInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        int kbIdx = InputConstants.KeyboardPlayerIndex;

        if (!_joined[kbIdx])
        {
            // Any key press joins the keyboard player
            _joined[kbIdx] = true;
            RefreshDisplay();
            return;
        }

        if (_ready[kbIdx])
        {
            return;
        }

        if (key.IsAction(InputActions.MoveLeft))
        {
            _selections[kbIdx] = (_selections[kbIdx] + CharacterNames.Length - 1) % CharacterNames.Length;
            RefreshDisplay();
        }
        else if (key.IsAction(InputActions.MoveRight))
        {
            _selections[kbIdx] = (_selections[kbIdx] + 1) % CharacterNames.Length;
            RefreshDisplay();
        }
        else if (key.IsAction(InputActions.PrimaryAttack))
        {
            _ready[kbIdx] = true;
            RefreshDisplay();
            TryStartGame();
        }
        else if (key.Keycode == Key.Escape)
        {
            _joined[kbIdx] = false;
            RefreshDisplay();
        }
    }

    private void HandleGamepadJoinInput(InputEvent @event)
    {
        if (@event is not InputEventJoypadButton joy || !joy.Pressed)
        {
            return;
        }

        int playerIndex = DeviceToPlayerIndex(joy.Device);
        if (playerIndex < 0 || playerIndex >= MaxPlayers)
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
            // Select/Back unjoins before confirming — gamepad P1 cannot unjoin
            if (playerIndex != InputConstants.GamepadPlayerIndex)
            {
                _joined[playerIndex] = false;
                RefreshDisplay();
            }
        }
    }

    private void HandleGamepadPlayerInput(InputEvent @event)
    {
        if (@event is not InputEventJoypadButton joy || !joy.Pressed)
        {
            return;
        }

        int playerIndex = DeviceToPlayerIndex(joy.Device);
        if (playerIndex < 0 || playerIndex >= MaxPlayers || !_joined[playerIndex] || _ready[playerIndex])
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
        // Gamepad P1 must be ready; keyboard P2 is optional
        if (!_ready[InputConstants.GamepadPlayerIndex])
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

        _gameState.Player1CharacterIndex = _selections[InputConstants.GamepadPlayerIndex];
        _gameState.Player2CharacterIndex = _selections[InputConstants.KeyboardPlayerIndex];
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

            bool isKeyboard = i == InputConstants.KeyboardPlayerIndex;
            string statusText;
            if (!_joined[i])
            {
                statusText = isKeyboard ? "Press any key to join" : "Press any button";
            }
            else if (_ready[i])
            {
                statusText = "READY";
            }
            else
            {
                statusText = isKeyboard ? "Z to confirm" : "(X) to confirm";
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

            // Gamepad P1 is always in; keyboard P2 must opt in
            _joined[InputConstants.GamepadPlayerIndex] = true;

            RefreshDisplay();
        }
    }
}
