using System.Collections.Generic;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.Enemies;
using FeralFrenzy.Godot.World;
using Godot;

namespace FeralFrenzy.Godot.UI;

public partial class HudController : Control
{
    // Initialized in _Ready — Godot does not call _Ready during construction
    private GameStateManager _gameState = null!;

    // Resolved via GetNodeOrNull in _Ready — [Export] Label wiring unreliable in hand-written .tscn
    private Label? _killLabel;
    private Label? _reviveLabel;
    private Label? _berserkerLabel;
    private ProgressBar? _bossHpBar;
    private EnemyHost? _connectedBoss;

    public override void _Ready()
    {
        _killLabel = GetNodeOrNull<Label>("KillLabel");
        _reviveLabel = GetNodeOrNull<Label>("ReviveLabel");
        _berserkerLabel = GetNodeOrNull<Label>("BerserkerLabel");
        _bossHpBar = GetNodeOrNull<ProgressBar>("BossHpBar");

        _gameState = GetNode<GameStateManager>(AutoloadPaths.GameStateManager);
        _gameState.StateChanged += OnStateChanged;

        Visible = _gameState.Current is SegmentState;

        if (_reviveLabel is not null)
        {
            _reviveLabel.Visible = false;
        }

        if (_berserkerLabel is not null)
        {
            _berserkerLabel.Visible = false;
        }

        if (_bossHpBar is not null)
        {
            _bossHpBar.Visible = false;
        }
    }

    public override void _ExitTree()
    {
        _gameState.StateChanged -= OnStateChanged;
        DisconnectBossHpBar();
    }

    public override void _Process(double delta)
    {
        if (!Visible)
        {
            return;
        }

        if (_killLabel is not null)
        {
            _killLabel.Text = $"Kills: {_gameState.KillCount}";
        }

        if (_reviveLabel is not null)
        {
            bool reviveActive = LevelController.Instance?.IsReviveActive ?? false;
            _reviveLabel.Visible = reviveActive;
            if (reviveActive)
            {
                float seconds = LevelController.Instance?.ReviveSecondsRemaining ?? 0f;
                _reviveLabel.Text = $"REVIVE! {Mathf.Max(0f, seconds):F0}s";
            }
        }

        if (_berserkerLabel is not null)
        {
            bool anyBerserk = false;
            IReadOnlyList<PlayerController>? players = LevelController.Instance?.GetPlayers();
            if (players is not null)
            {
                foreach (PlayerController p in players)
                {
                    if (p.IsBerserkerActive)
                    {
                        anyBerserk = true;
                        break;
                    }
                }
            }

            _berserkerLabel.Visible = anyBerserk;
        }
    }

    public void OnBossHpChanged(float current, float max)
    {
        if (_bossHpBar is null)
        {
            return;
        }

        _bossHpBar.MaxValue = max;
        _bossHpBar.Value = current;
    }

    private void OnStateChanged(GameStateNode from, GameStateNode to)
    {
        Visible = to is SegmentState or SegmentRestartState or BossFightState;

        if (to is BossFightState)
        {
            ConnectBossHpBar();
        }
        else
        {
            DisconnectBossHpBar();
            if (_bossHpBar is not null)
            {
                _bossHpBar.Visible = false;
            }
        }
    }

    private void ConnectBossHpBar()
    {
        DisconnectBossHpBar();

        EnemyHost? boss = LevelController.Instance?.ActiveBoss;
        if (boss is null || _bossHpBar is null)
        {
            return;
        }

        _bossHpBar.MaxValue = boss.Definition?.MaxHp ?? 30f;
        _bossHpBar.Value = _bossHpBar.MaxValue;
        _bossHpBar.Visible = true;
        boss.HpChanged += OnBossHpChanged;
        _connectedBoss = boss;
    }

    private void DisconnectBossHpBar()
    {
        if (_connectedBoss is not null && IsInstanceValid(_connectedBoss))
        {
            _connectedBoss.HpChanged -= OnBossHpChanged;
        }

        _connectedBoss = null;
    }
}
