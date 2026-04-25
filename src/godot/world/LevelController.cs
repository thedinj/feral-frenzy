using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FeralFrenzy.Core.Data.Content;
using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Camera;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.Enemies;
using FeralFrenzy.Godot.Importer;
using FeralFrenzy.Godot.Weapons;
using Godot;

namespace FeralFrenzy.Godot.World;

public partial class LevelController : Node2D
{
    private const string ChapterJsonPath = "res://data/chapters/chapter_cretaceous.json";
    private const float ReviveProximity = 32f;
    private const float ReviveHoldDuration = 2f;
    private const float ReviveWindowSeconds = 10f;
    private const float BossSpawnX = 160f;
    private const float BossSpawnY = 155f;

    // Global access for WeaponController and enemy projectile spawning
    public static LevelController? Instance { get; private set; }

    // Set when a boss is added to the level; cleared when entities are wiped.
    // HudController reads this directly instead of searching the "bosses" group.
    public EnemyController? ActiveBoss { get; private set; }

    public bool IsReviveActive => _downPlayer is not null && _reviveWindowTimer.TimeLeft > 0;

    public float ReviveSecondsRemaining => (float)_reviveWindowTimer.TimeLeft;

    private readonly List<PlayerController> _players = new List<PlayerController>();
    private readonly PlayerRoster _roster = new PlayerRoster();

    // Resolved in _Ready via GetNode — node-type exports are not reliable in hand-written .tscn files
    private Node2D _entities = null!;
    private Node2D _playerSpawns = null!;
    private CoopCamera? _camera;

    private GameStateManager _gameState = null!;
    private AssetRegistry _registry = null!;
    private EntityPool _entityPool = null!;

    // Initialized in _Ready — Godot does not call _Ready during construction
    private Timer _reviveWindowTimer = null!;

    private PlayerController? _downPlayer;
    private float _reviveHoldTimer;
    private bool _levelActive;

    public override void _Ready()
    {
        _entities = GetNode<Node2D>(NodePaths.LevelEntities);
        _playerSpawns = GetNode<Node2D>(NodePaths.LevelPlayerSpawns);
        _camera = GetNodeOrNull<CoopCamera>(NodePaths.LevelCamera);

        _gameState = GetNode<GameStateManager>(AutoloadPaths.GameStateManager);
        _registry = GetNode<AssetRegistry>(AutoloadPaths.AssetRegistry);
        _entityPool = GetNode<EntityPool>(AutoloadPaths.EntityPool);

        _reviveWindowTimer = new Timer();
        _reviveWindowTimer.OneShot = true;
        _reviveWindowTimer.Timeout += OnReviveWindowTimerExpired;
        AddChild(_reviveWindowTimer);

        Instance = this;

        _gameState.StateChanged += OnStateChanged;

        if (_gameState.Current is SegmentState)
        {
            ActivateLevel(isRestart: false);
        }

        TryBuildParallax();
        PreWarmPool();
    }

    public override void _ExitTree()
    {
        _gameState.StateChanged -= OnStateChanged;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void _Process(double delta)
    {
        if (!_levelActive)
        {
            return;
        }

        if (_downPlayer is not null)
        {
            CheckReviveProximity((float)delta);
        }
    }

    public void AddToEntities(Node entity)
    {
        _entities.AddChild(entity);
        if (entity is EnemyController enemy && enemy.IsInGroup("bosses"))
        {
            ActiveBoss = enemy;
        }
    }

    public IReadOnlyList<PlayerController> GetPlayers() => _players;

    public void HandlePlayerDown(PlayerController player)
    {
        _gameState.NotifyPlayerDeath();
        PlayerRoster.DownResult result = _roster.MarkDown();

        if (result == PlayerRoster.DownResult.AllDown)
        {
            _gameState.TransitionTo<SegmentRestartState>();
        }
        else
        {
            _downPlayer = _players.FirstOrDefault(p => p.IsDown);
            _reviveHoldTimer = 0f;
            _reviveWindowTimer.Start(ReviveWindowSeconds);
        }
    }

    private void OnStateChanged(GameStateNode from, GameStateNode to)
    {
        switch (to)
        {
            case SegmentState:
                ActivateLevel(isRestart: from is SegmentRestartState);
                break;

            case BossFightState when from is SegmentRestartState:
                ClearDynamicEntities();
                RespawnPlayers();
                SpawnBoss();
                _levelActive = true;
                break;

            case SegmentRestartState:
                _levelActive = false;
                _reviveWindowTimer.Stop();
                _downPlayer = null;
                break;

            case RunSummaryState:
            case LoadoutSelectState:
            case TitleState:
                DeactivateLevel();
                break;
        }
    }

    private void ActivateLevel(bool isRestart)
    {
        if (isRestart)
        {
            ClearDynamicEntities();
            RespawnPlayers();
        }
        else
        {
            SpawnPlayers();
            SpawnScaledEnemies();
            PreWarmPool();
        }

        _levelActive = true;
    }

    private void DeactivateLevel()
    {
        _levelActive = false;
        _reviveWindowTimer.Stop();
        _downPlayer = null;
        ClearAllEntities();
        _players.Clear();
        _roster.Reset(0);
        _camera?.ClearPlayers();
    }

    private void SpawnPlayers()
    {
        _players.Clear();

        var spawnPoints = _playerSpawns.GetChildren();
        int count = _gameState.ActivePlayerCount;

        for (int i = 0; i < count && i < spawnPoints.Count; i++)
        {
            PlayerController? player = SpawnPlayer(i);
            if (player is null)
            {
                continue;
            }

            if (spawnPoints[i] is Node2D spawnNode)
            {
                player.GlobalPosition = spawnNode.GlobalPosition;
            }

            _entities.AddChild(player);
            EquipDefaultWeapon(player);
            _players.Add(player);
            _camera?.RegisterPlayer(player);
        }

        _roster.Reset(_players.Count);
    }

    private void RespawnPlayers()
    {
        var spawnPoints = _playerSpawns.GetChildren();

        for (int i = 0; i < _players.Count && i < spawnPoints.Count; i++)
        {
            if (_players[i].IsDown || _players[i].IsDead)
            {
                _players[i].Revive(fullHeal: true);
            }

            if (spawnPoints[i] is Node2D spawnNode)
            {
                _players[i].GlobalPosition = spawnNode.GlobalPosition;
            }
        }

        _roster.Reset(_players.Count);
        _downPlayer = null;
        _reviveHoldTimer = 0f;
    }

    private void OnReviveWindowTimerExpired()
    {
        _roster.EliminateDownedPlayers();
        _downPlayer = null;
        _reviveHoldTimer = 0f;

        if (_roster.AliveCount == 0)
        {
            _gameState.TransitionTo<SegmentRestartState>();
        }
    }

    private void CheckReviveProximity(float delta)
    {
        if (_downPlayer is null)
        {
            return;
        }

        PlayerController? reviver = _players.FirstOrDefault(p =>
            !p.IsDown && !p.IsDead &&
            p.GlobalPosition.DistanceTo(_downPlayer.GlobalPosition) <= ReviveProximity);

        if (reviver is not null
            && reviver.GetNode<InputManager>(AutoloadPaths.InputManager)
                .IsActionPressed(reviver.PlayerIndex, InputActions.PrimaryAttack))
        {
            _reviveHoldTimer += delta;
            if (_reviveHoldTimer >= ReviveHoldDuration)
            {
                _downPlayer.Revive();
                _reviveWindowTimer.Stop();
                _roster.MarkRevived();
                _downPlayer = null;
                _reviveHoldTimer = 0f;
            }
        }
        else
        {
            _reviveHoldTimer = 0f;
        }
    }

    private void EquipDefaultWeapon(PlayerController player)
    {
        FFWeaponDefinition? def = _registry.Load<FFWeaponDefinition>(AssetKeys.WeaponDefDefaultBlaster);
        if (def is null)
        {
            GD.PushWarning("LevelController: DefaultBlaster definition not found — player spawns unarmed.");
            return;
        }

        player.EquipWeapon(new WeaponController { Definition = def });
    }

    private PlayerController? SpawnPlayer(int playerIndex)
    {
        string sceneKey = playerIndex == 0
            ? AssetKeys.SceneCharBear
            : AssetKeys.SceneCharHoneyBadger;

        PackedScene? scene = _registry.GetScene(sceneKey);
        if (scene is null)
        {
            GD.PushWarning($"LevelController: could not load player scene for index {playerIndex}.");
            return null;
        }

        PlayerController player = scene.Instantiate<PlayerController>();
        player.PlayerIndex = playerIndex;
        return player;
    }

    private void ClearDynamicEntities()
    {
        ActiveBoss = null;
        foreach (Node child in _entities.GetChildren())
        {
            if (child is not PlayerController)
            {
                child.QueueFree();
            }
        }
    }

    private void ClearAllEntities()
    {
        ActiveBoss = null;
        foreach (Node child in _entities.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void SpawnBoss()
    {
        PackedScene? scene = _registry.GetScene(AssetKeys.SceneEnemyBoss);
        if (scene is null)
        {
            GD.PushWarning("LevelController: boss scene not found — skipping boss spawn.");
            return;
        }

        Node2D bossNode = scene.Instantiate<Node2D>();
        bossNode.GlobalPosition = new Vector2(BossSpawnX, BossSpawnY);
        _entities.AddChild(bossNode);
        ActiveBoss = bossNode as EnemyController;
    }

    private void SpawnScaledEnemies()
    {
        if (_gameState.ActivePlayerCount < 2)
        {
            return;
        }

        // One extra patroller on the right side for 2-player sessions
        PackedScene? patrollerScene = _registry.GetScene(AssetKeys.SceneEnemyGroundPatroller);
        if (patrollerScene is not null)
        {
            var extra = patrollerScene.Instantiate<Node2D>();
            extra.GlobalPosition = new Vector2(460f, 155f);
            _entities.AddChild(extra);
        }
    }

    private void TryBuildParallax()
    {
        using FileAccess? file = FileAccess.Open(ChapterJsonPath, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            GD.PushWarning("LevelController: chapter_cretaceous.json not found, parallax skipped.");
            return;
        }

        FFChapterDefinition? chapter = JsonSerializer.Deserialize<FFChapterDefinition>(
            file.GetAsText(),
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

        if (chapter is null)
        {
            return;
        }

        Node2D bg = GodotParallaxBuilder.Build(chapter.ParallaxLayers, _registry);
        AddChild(bg);
        MoveChild(bg, 0); // keep behind everything else
    }

    private void PreWarmPool()
    {
        _entityPool.PreWarm(AssetKeys.SceneEnemyGroundPatroller, 8);
        _entityPool.PreWarm(AssetKeys.SceneEnemyAerialDiver, 4);
        _entityPool.PreWarm(AssetKeys.SceneEnemyMountedDino, 2);
        _entityPool.PreWarm(AssetKeys.SceneEnemyPteroBomber, 2);
        _entityPool.PreWarm(AssetKeys.SceneProjectile, 20);
        _entityPool.PreWarm(AssetKeys.SceneSpinningBladeProjectile, 4);
    }
}
