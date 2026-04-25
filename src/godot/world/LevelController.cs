using System.Collections.Generic;
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

    // Global access for HudController (read-only display state) and BossRoomTrigger (entity addition).
    public static LevelController? Instance { get; private set; }

    [Export]
    public LevelConfig? Config { get; set; }

    // Set when a boss is added to the level; cleared when entities are wiped.
    // HudController reads this directly instead of searching the "bosses" group.
    public EnemyHost? ActiveBoss { get; private set; }

    public bool IsReviveActive => _reviveSystem.IsActive;

    public float ReviveSecondsRemaining => _reviveSystem.SecondsRemaining;

    public float EffectiveGravity => PhysicsConstants.Gravity * (Config?.GravityScale ?? 1.0f);

    private readonly List<PlayerController> _players = new List<PlayerController>();
    private readonly PlayerRoster _roster = new PlayerRoster();

    // Resolved in _Ready via GetNode — node-type exports are not reliable in hand-written .tscn files
    private Node2D _entities = null!; // assigned in _Ready()
    private Node2D _playerSpawns = null!; // assigned in _Ready()
    private CoopCamera? _camera;

    private GameStateManager _gameState = null!; // assigned in _Ready()
    private AssetRegistry _registry = null!; // assigned in _Ready()
    private EntityPool _entityPool = null!; // assigned in _Ready()
    private ReviveSystem _reviveSystem = null!; // assigned in _Ready()

    private float _savedGravity;
    private bool _gravityOverrideApplied;
    private bool _levelActive;

    public override void _Ready()
    {
        _entities = GetNode<Node2D>(NodePaths.LevelEntities);
        _playerSpawns = GetNode<Node2D>(NodePaths.LevelPlayerSpawns);
        _camera = GetNodeOrNull<CoopCamera>(NodePaths.LevelCamera);

        _gameState = GetNode<GameStateManager>(AutoloadPaths.GameStateManager);
        _registry = GetNode<AssetRegistry>(AutoloadPaths.AssetRegistry);
        _entityPool = GetNode<EntityPool>(AutoloadPaths.EntityPool);

        _reviveSystem = new ReviveSystem();
        AddChild(_reviveSystem);
        _reviveSystem.Configure(Config);
        _reviveSystem.ReviveCompleted += OnReviveCompleted;
        _reviveSystem.WindowExpired += OnReviveWindowExpired;

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
        RestoreGravity();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void _Process(double delta)
    {
        _ = delta;
    }

    // Called by BossRoomTrigger for normal boss entry flow.
    public void AddToEntities(Node entity)
    {
        _entities.AddChild(entity);
        if (entity is EnemyHost enemy)
        {
            if (enemy.IsInGroup("bosses"))
            {
                ActiveBoss = enemy;
            }

            enemy.ProjectileSpawnRequested += OnEntityProjectileSpawnRequested;
            enemy.MinionSummonRequested += OnMinionSummonRequested;
        }
    }

    public IReadOnlyList<PlayerController> GetPlayers() => _players;

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
                _reviveSystem.Cancel();
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
        ApplyGravityOverride();

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
        _reviveSystem.Cancel();
        RestoreGravity();
        ClearAllEntities();
        _players.Clear();
        _roster.Reset(0);
        _camera?.ClearPlayers();
    }

    private void OnPlayerWentDown(PlayerController player)
    {
        _gameState.NotifyPlayerDeath();
        PlayerRoster.DownResult result = _roster.MarkDown();

        if (result == PlayerRoster.DownResult.AllDown)
        {
            _gameState.TransitionTo<SegmentRestartState>();
        }
        else
        {
            _reviveSystem.StartWindow(player, _players);
        }
    }

    private void OnReviveCompleted(PlayerController revived)
    {
        revived.Revive();
        _roster.MarkRevived();
    }

    private void OnReviveWindowExpired()
    {
        _roster.EliminateDownedPlayers();
        if (_roster.AliveCount == 0)
        {
            _gameState.TransitionTo<SegmentRestartState>();
        }
    }

    private void OnEntityProjectileSpawned(Node2D projectile)
    {
        _entities.AddChild(projectile);
    }

    private void OnEntityProjectileSpawnRequested(Vector2 dir, float speed, float impact)
    {
        PackedScene? scene = _registry.GetScene(AssetKeys.SceneProjectile);
        if (scene is null)
        {
            return;
        }

        ProjectileController projectile = scene.Instantiate<ProjectileController>();
        projectile.Initialize(dir, speed, impact, ProjectileOwner.Enemy);
        _entities.AddChild(projectile);
    }

    private void OnMinionSummonRequested(string assetKey, Vector2 offset1, Vector2 offset2)
    {
        foreach (Vector2 offset in new[] { offset1, offset2 })
        {
            EnemyHost minion = _entityPool.Get<EnemyHost>(assetKey);
            if (ActiveBoss is not null)
            {
                minion.GlobalPosition = ActiveBoss.GlobalPosition + offset;
            }

            AddToEntities(minion);
        }
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

            player.WentDown += () => OnPlayerWentDown(player);

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

        Vector2 spawnPos = Config?.BossSpawnPosition ?? new Vector2(160f, 155f);
        EnemyHost bossNode = scene.Instantiate<EnemyHost>();
        bossNode.GlobalPosition = spawnPos;
        _entities.AddChild(bossNode);
        ActiveBoss = bossNode;
        ActiveBoss.ProjectileSpawnRequested += OnEntityProjectileSpawnRequested;
        ActiveBoss.MinionSummonRequested += OnMinionSummonRequested;
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
            var extra = patrollerScene.Instantiate<EnemyHost>();
            extra.GlobalPosition = Config?.ExtraPatrollerSpawnPosition ?? new Vector2(460f, 155f);
            _entities.AddChild(extra);
            extra.ProjectileSpawnRequested += OnEntityProjectileSpawnRequested;
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

        var weapon = new WeaponController { Definition = def };
        weapon.ProjectileSpawned += OnEntityProjectileSpawned;
        player.EquipWeapon(weapon);
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
        _entityPool.PreWarm(AssetKeys.SceneEnemyGroundPatroller, Config?.PoolWarmGroundPatroller ?? 8);
        _entityPool.PreWarm(AssetKeys.SceneEnemyAerialDiver, Config?.PoolWarmAerialDiver ?? 4);
        _entityPool.PreWarm(AssetKeys.SceneEnemyMountedDino, Config?.PoolWarmMountedDino ?? 2);
        _entityPool.PreWarm(AssetKeys.SceneEnemyPteroBomber, Config?.PoolWarmPteroBomber ?? 2);
        _entityPool.PreWarm(AssetKeys.SceneProjectile, Config?.PoolWarmProjectile ?? 20);
        _entityPool.PreWarm(AssetKeys.SceneSpinningBladeProjectile, Config?.PoolWarmSpinningBlade ?? 4);
    }

    private void ApplyGravityOverride()
    {
        float scale = Config?.GravityScale ?? 1.0f;
        if (Mathf.IsEqualApprox(scale, 1.0f))
        {
            return;
        }

        // Override the physics world's default gravity so Area2D and RigidBody2D nodes
        // are also affected, in addition to the manual gravity in character physics processes.
        _savedGravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
        PhysicsServer2D.AreaSetParam(
            GetWorld2D().Space,
            PhysicsServer2D.AreaParameter.Gravity,
            _savedGravity * scale);
        _gravityOverrideApplied = true;
    }

    private void RestoreGravity()
    {
        if (!_gravityOverrideApplied)
        {
            return;
        }

        PhysicsServer2D.AreaSetParam(
            GetWorld2D().Space,
            PhysicsServer2D.AreaParameter.Gravity,
            _savedGravity);
        _gravityOverrideApplied = false;
    }
}
