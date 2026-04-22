using System;
using System.Collections.Generic;
using FeralFrenzy.Core.Data.Engine;
using Godot;

namespace FeralFrenzy.Godot.Autoloads;

public partial class GameStateManager : Node
{
    public GameState Current { get; private set; } = GameState.Title;

    private readonly HashSet<int> _registeredPlayers = new HashSet<int>();
    private readonly HashSet<int> _downPlayers = new HashSet<int>();

    private StatePayload? _activeSegmentPayload;

    // LoadoutSelect stores the character choice here for LevelController to read.
    public int Player1CharacterIndex { get; set; } = 0;
    public int Player2CharacterIndex { get; set; } = 1;
    public int ActivePlayerCount { get; set; } = 1;

    // Run stats tracked across the current run.
    public int KillCount { get; private set; }
    public int DeathCount { get; private set; }
    public float RunTimeSeconds { get; private set; }

    // Initialized in _Ready — Godot does not call _Ready during construction
    private Timer _reviveTimer = null!;
    private Timer _restartTimer = null!;

    [Signal]
    public delegate void StateChangedEventHandler(long from, long to);

    public override void _Ready()
    {
        _reviveTimer = new Timer();
        _reviveTimer.OneShot = true;
        _reviveTimer.Timeout += OnReviveTimerExpired;
        AddChild(_reviveTimer);

        _restartTimer = new Timer();
        _restartTimer.OneShot = true;
        _restartTimer.Timeout += OnRestartTimerExpired;
        AddChild(_restartTimer);
    }

    public override void _Process(double delta)
    {
        if (Current == GameState.Segment)
        {
            RunTimeSeconds += (float)delta;
        }
    }

    public void TransitionTo(GameState next, StatePayload? payload = null)
    {
        ValidateTransition(next);

        GD.Print($"[GSM] {Current} → {next}");
        long previous = (long)Current;
        Current = next;

        HandleStateEntry(next, payload);
        EmitSignal(SignalName.StateChanged, previous, (long)next);
    }

    public void RegisterPlayer(int playerIndex)
    {
        _registeredPlayers.Add(playerIndex);
        _downPlayers.Remove(playerIndex);
    }

    public void UnregisterAllPlayers()
    {
        _registeredPlayers.Clear();
        _downPlayers.Clear();
    }

    public void NotifyPlayerDown(int playerIndex)
    {
        _downPlayers.Add(playerIndex);
        DeathCount++;

        bool allDown = _registeredPlayers.Count == 0
            || _downPlayers.Count >= _registeredPlayers.Count;

        if (allDown)
        {
            TransitionTo(GameState.SegmentRestart);
        }
        else
        {
            TransitionTo(
                GameState.ReviveWindow,
                new ReviveWindowPayload(playerIndex, ReviveWindowSeconds: 10f));
        }
    }

    public void RevivePlayer(int playerIndex)
    {
        _downPlayers.Remove(playerIndex);
        _reviveTimer.Stop();
        TransitionTo(GameState.Segment, _activeSegmentPayload);
    }

    public void NotifyEnemyKilled()
    {
        KillCount++;
    }

    public void ResetRunStats()
    {
        KillCount = 0;
        DeathCount = 0;
        RunTimeSeconds = 0f;
    }

    private static bool IsActiveState(GameState state)
    {
        return state is GameState.Title
            or GameState.LoadoutSelect
            or GameState.Segment
            or GameState.ReviveWindow
            or GameState.SegmentRestart
            or GameState.RunSummary;
    }

    private void HandleStateEntry(GameState state, StatePayload? payload)
    {
        switch (state)
        {
            case GameState.Segment:
                _downPlayers.Clear();
                if (payload is SegmentPayload segPayload)
                {
                    _activeSegmentPayload = segPayload;
                }

                break;

            case GameState.ReviveWindow:
                if (payload is ReviveWindowPayload revivePayload)
                {
                    _reviveTimer.Start(revivePayload.ReviveWindowSeconds);
                }

                break;

            case GameState.SegmentRestart:
                _restartTimer.Start(1.5f);
                break;

            case GameState.LoadoutSelect:
                ResetRunStats();
                UnregisterAllPlayers();
                break;

            default:
                if (!IsActiveState(state))
                {
                    GD.PushWarning(
                        $"GameStateManager: entered stub state {state}. No implementation in Phase 1.");
                }

                break;
        }
    }

    private void OnReviveTimerExpired()
    {
        if (Current == GameState.ReviveWindow)
        {
            TransitionTo(GameState.SegmentRestart);
        }
    }

    private void OnRestartTimerExpired()
    {
        if (Current == GameState.SegmentRestart)
        {
            _downPlayers.Clear();
            StatePayload? restartPayload = _activeSegmentPayload is SegmentPayload seg
                ? new SegmentPayload(seg.Segment, IsRestart: true)
                : null;
            TransitionTo(GameState.Segment, restartPayload);
        }
    }

    private void ValidateTransition(GameState next)
    {
        if (!LegalTransitions.TryGetValue(Current, out HashSet<GameState>? legalTargets))
        {
            throw new InvalidOperationException(
                $"GameStateManager: no transitions defined from {Current}.");
        }

        if (!legalTargets.Contains(next))
        {
            throw new InvalidOperationException(
                $"Illegal state transition: {Current} → {next}. " +
                $"Legal transitions from {Current}: {string.Join(", ", legalTargets)}");
        }
    }

    private static readonly Dictionary<GameState, HashSet<GameState>> LegalTransitions =
        new Dictionary<GameState, HashSet<GameState>>
        {
            [GameState.Title] = new HashSet<GameState>
            {
                GameState.Attract,
                GameState.LoadoutSelect,
                GameState.LevelEditor,
                GameState.Credits,
                GameState.WorkshopBrowser,
            },
            [GameState.Attract] = new HashSet<GameState> { GameState.Title },
            [GameState.LoadoutSelect] = new HashSet<GameState>
            {
                GameState.Title,
                GameState.Cinematic,
                GameState.Segment, // Phase 1: skip chapter intro cinematic
            },
            [GameState.Cinematic] = new HashSet<GameState>
            {
                GameState.Segment,
                GameState.BossIntro,
                GameState.BossFight,
                GameState.VillainExit,
                GameState.RunSummary,
                GameState.Title,
            },
            [GameState.Segment] = new HashSet<GameState>
            {
                GameState.ReviveWindow,
                GameState.SegmentRestart,
                GameState.BossIntro,
                GameState.GradiusLevel,
                GameState.BrawlerLevel,
                GameState.Cinematic,
                GameState.Segment,
                GameState.RunSummary, // Phase 1: exit trigger goes directly to RunSummary
            },
            [GameState.ReviveWindow] = new HashSet<GameState>
            {
                GameState.Segment,
                GameState.SegmentRestart,
            },
            [GameState.SegmentRestart] = new HashSet<GameState> { GameState.Segment },
            [GameState.BossIntro] = new HashSet<GameState> { GameState.BossFight },
            [GameState.BossFight] = new HashSet<GameState>
            {
                GameState.ReviveWindow,
                GameState.SegmentRestart,
                GameState.VillainExit,
            },
            [GameState.VillainExit] = new HashSet<GameState>
            {
                GameState.Cinematic,
                GameState.GradiusLevel,
                GameState.BrawlerLevel,
                GameState.Segment,
                GameState.RunSummary,
            },
            [GameState.GradiusLevel] = new HashSet<GameState>
            {
                GameState.Cinematic,
                GameState.RunSummary,
            },
            [GameState.BrawlerLevel] = new HashSet<GameState>
            {
                GameState.Cinematic,
                GameState.RunSummary,
            },
            [GameState.RunSummary] = new HashSet<GameState>
            {
                GameState.Title,
                GameState.LoadoutSelect,
                GameState.Credits,
            },
            [GameState.LevelEditor] = new HashSet<GameState> { GameState.Title },
            [GameState.Credits] = new HashSet<GameState>
            {
                GameState.Title,
                GameState.RunSummary,
            },
            [GameState.WorkshopBrowser] = new HashSet<GameState> { GameState.Title },
        };
}
