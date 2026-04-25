using System;
using System.Collections.Generic;
using System.Linq;
using FeralFrenzy.Core.Data.Engine;
using Godot;

namespace FeralFrenzy.Godot.Autoloads;

public partial class GameStateManager : Node
{
    private readonly GameStateContext _ctx = new GameStateContext();
    private readonly Dictionary<Type, GameStateNode> _states;
    private GameStateNode _currentNode;

    // Initialized in _Ready — Godot does not call _Ready during construction
    private Timer _autoTimer = null!;

    public GameStateNode Current { get; private set; }

    // LoadoutSelect stores the character choice here for LevelController to read.
    public int Player1CharacterIndex { get; set; } = 0;

    public int Player2CharacterIndex { get; set; } = 1;

    public int ActivePlayerCount { get; set; } = 1;

    public int KillCount => _ctx.KillCount;

    public int DeathCount => _ctx.DeathCount;

    public float RunTimeSeconds => _ctx.RunTimeSeconds;

    public event Action<GameStateNode, GameStateNode>? StateChanged;

    public GameStateManager()
    {
        _states = BuildStates();
        _currentNode = _states[typeof(TitleState)];
        Current = _currentNode;

        foreach ((Type key, GameStateNode node) in _states)
        {
            foreach (Type target in node.LegalTargets)
            {
                if (!_states.ContainsKey(target))
                {
                    throw new InvalidOperationException(
                        $"GameStateManager: {key.Name} has unregistered legal target {target.Name}.");
                }
            }
        }
    }

    public override void _Ready()
    {
        _autoTimer = new Timer();
        _autoTimer.OneShot = true;
        _autoTimer.Timeout += OnAutoTimerFired;
        AddChild(_autoTimer);
    }

    public override void _Process(double delta)
    {
        if (Current is SegmentState)
        {
            _ctx.RunTimeSeconds += (float)delta;
        }
    }

    public void TransitionTo<TState>(StatePayload? payload = null)
        where TState : GameStateNode
        => TransitionTo(typeof(TState), payload);

    public void TransitionTo(Type next, StatePayload? payload = null)
    {
        if (!_currentNode.LegalTargets.Contains(next))
        {
            IEnumerable<string> legal = _currentNode.LegalTargets.Select(t => t.Name);
            throw new InvalidOperationException(
                $"Illegal state transition: {_currentNode.GetType().Name} → {next.Name}. " +
                $"Legal from {_currentNode.GetType().Name}: {string.Join(", ", legal)}");
        }

        _autoTimer.Stop();
        _currentNode.OnExit(_ctx);

        GameStateNode previous = Current;
        _currentNode = _states[next];
        Current = _currentNode;

        _currentNode.OnEnter(_ctx, previous, payload);

        if (_currentNode is IAutoTransition timed)
        {
            _autoTimer.Start(timed.GetDelay(payload));
        }

        StateChanged?.Invoke(previous, Current);
    }

    public void NotifyPlayerDeath()
    {
        _ctx.DeathCount++;
    }

    public void NotifyEnemyKilled()
    {
        _ctx.KillCount++;
    }

    public void ResetRunStats()
    {
        _ctx.KillCount = 0;
        _ctx.DeathCount = 0;
        _ctx.RunTimeSeconds = 0f;
    }

    private static Dictionary<Type, GameStateNode> BuildStates()
    {
        GameStateNode[] nodes =
        {
            new TitleState(),
            new AttractState(),
            new LoadoutSelectState(),
            new CinematicState(),
            new SegmentState(),
            new SegmentRestartState(),
            new BossIntroState(),
            new BossFightState(),
            new VillainExitState(),
            new GradiusLevelState(),
            new BrawlerLevelState(),
            new RunSummaryState(),
            new LevelEditorState(),
            new CreditsState(),
            new WorkshopBrowserState(),
        };

        var dict = new Dictionary<Type, GameStateNode>(nodes.Length);
        foreach (GameStateNode n in nodes)
        {
            dict[n.GetType()] = n;
        }

        return dict;
    }

    private void OnAutoTimerFired()
    {
        if (_currentNode is not IAutoTransition timed)
        {
            return;
        }

        (Type Next, StatePayload? Payload)? result = timed.SelectNext(_ctx);
        if (result is { } r)
        {
            TransitionTo(r.Next, r.Payload);
        }
    }
}
