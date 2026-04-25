using System.Collections.Generic;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.World;

public partial class ReviveSystem : Node
{
    [Signal]
    public delegate void ReviveCompletedEventHandler(PlayerController revived);

    [Signal]
    public delegate void WindowExpiredEventHandler();

    public bool IsActive { get; private set; }

    public float SecondsRemaining => IsActive ? (float)_timer.TimeLeft : 0f;

    // assigned in _Ready()
    private InputManager _input = null!;

    // assigned in _Ready()
    private Timer _timer = null!;

    private LevelConfig? _config;
    private PlayerController? _downPlayer;
    private IReadOnlyList<PlayerController> _allPlayers = new List<PlayerController>();
    private float _holdTimer;

    public override void _Ready()
    {
        _input = GetNode<InputManager>(AutoloadPaths.InputManager);

        _timer = new Timer();
        _timer.OneShot = true;
        _timer.Timeout += OnTimerExpired;
        AddChild(_timer);
    }

    public void Configure(LevelConfig? config)
    {
        _config = config;
    }

    public void StartWindow(PlayerController downPlayer, IReadOnlyList<PlayerController> allPlayers)
    {
        _downPlayer = downPlayer;
        _allPlayers = allPlayers;
        _holdTimer = 0f;
        IsActive = true;
        _timer.Start(_config?.ReviveWindowSeconds ?? 10f);
    }

    public void Cancel()
    {
        IsActive = false;
        _timer.Stop();
        _downPlayer = null;
        _holdTimer = 0f;
    }

    public override void _Process(double delta)
    {
        if (!IsActive || _downPlayer is null)
        {
            return;
        }

        CheckReviveProximity((float)delta);
    }

    private void CheckReviveProximity(float delta)
    {
        float proximity = _config?.ReviveProximityUnits ?? 32f;
        float holdDuration = _config?.ReviveHoldDuration ?? 2f;
        PlayerController? reviver = null;

        foreach (PlayerController player in _allPlayers)
        {
            if (!player.IsDown && !player.IsDead &&
                player.GlobalPosition.DistanceTo(_downPlayer!.GlobalPosition) <= proximity)
            {
                reviver = player;
                break;
            }
        }

        if (reviver is not null && _input.IsActionPressed(reviver.PlayerIndex, InputActions.PrimaryAttack))
        {
            _holdTimer += delta;
            if (_holdTimer >= holdDuration)
            {
                PlayerController revivedPlayer = _downPlayer!;
                Cancel();
                EmitSignal(SignalName.ReviveCompleted, revivedPlayer);
            }
        }
        else
        {
            _holdTimer = 0f;
        }
    }

    private void OnTimerExpired()
    {
        IsActive = false;
        _downPlayer = null;
        _holdTimer = 0f;
        EmitSignal(SignalName.WindowExpired);
    }
}
