using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FeralFrenzy.Core.Data.Content;
using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Camera;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.Importer;
using FeralFrenzy.Godot.Weapons;
using Godot;

namespace FeralFrenzy.Godot.World;

public partial class LevelController : Node2D
{
    private const string ChapterJsonPath = "res://data/chapters/chapter_cretaceous.json";
    private const float ReviveProximity = 32f;
    private const float ReviveHoldDuration = 2f;

    // Global access for WeaponController and enemy projectile spawning
    public static LevelController? Instance { get; private set; }

    private readonly List<PlayerController> _players = new List<PlayerController>();

    // Resolved in _Ready via GetNode — node-type exports are not reliable in hand-written .tscn files
    private Node2D _entities = null!;
    private Node2D _playerSpawns = null!;
    private CoopCamera? _camera;

    private GameStateManager _gameState = null!;
    private AssetRegistry _registry = null!;
    private EntityPool _entityPool = null!;

    private PlayerController? _downPlayer;
    private float _reviveHoldTimer;
    private bool _levelActive;

    public override void _Ready()
    {
        _entities = GetNode<Node2D>(NodePaths.LevelEntities);
        _playerSpawns = GetNode<Node2D>(NodePaths.LevelPlayerSpawns);
        _camera = GetNodeOrNull<CoopCamera>(NodePaths.LevelCamera);

        _gameState = GetNode<GameStateManager>("/root/GameStateManager");
        _registry = GetNode<AssetRegistry>("/root/AssetRegistry");
        _entityPool = GetNode<EntityPool>("/root/EntityPool");

        Instance = this;

        _gameState.StateChanged += OnStateChanged;

        GD.Print($"[LVL] _Ready: state={_gameState.Current}, playerCount={_gameState.ActivePlayerCount}");

        TryBuildParallax();
        PreWarmPool();
    }

    public override void _ExitTree()
    {
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

        if (_gameState.Current == GameState.ReviveWindow)
        {
            CheckReviveProximity((float)delta);
        }
    }

    public void AddToEntities(Node entity)
    {
        _entities.AddChild(entity);
    }

    public IReadOnlyList<PlayerController> GetPlayers() => _players;

    private void OnStateChanged(long from, long to)
    {
        GameState newState = (GameState)to;
        GD.Print($"[LVL] OnStateChanged: {(GameState)from} → {newState}");

        switch (newState)
        {
            case GameState.Segment:
                bool isRestart = (GameState)from == GameState.SegmentRestart;
                ActivateLevel(isRestart);
                break;

            case GameState.SegmentRestart:
                _levelActive = false;
                break;

            case GameState.ReviveWindow:
                FindDownPlayer();
                _reviveHoldTimer = 0f;
                break;

            case GameState.RunSummary:
            case GameState.LoadoutSelect:
            case GameState.Title:
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
            PreWarmPool();
        }

        _levelActive = true;
    }

    private void DeactivateLevel()
    {
        _levelActive = false;
        ClearAllEntities();
        _players.Clear();
        _gameState.UnregisterAllPlayers();
        _camera?.ClearPlayers();
    }

    private void SpawnPlayers()
    {
        _players.Clear();
        _gameState.UnregisterAllPlayers();

        var spawnPoints = _playerSpawns.GetChildren();
        int count = _gameState.ActivePlayerCount;
        GD.Print($"[LVL] SpawnPlayers: count={count}, spawnPoints={spawnPoints.Count}, camera={(_camera is not null ? "ok" : "null")}");

        for (int i = 0; i < count && i < spawnPoints.Count; i++)
        {
            PlayerController? player = SpawnPlayer(i);
            if (player is null)
            {
                GD.Print($"[LVL] SpawnPlayer({i}) returned null");
                continue;
            }

            if (spawnPoints[i] is Node2D spawnNode)
            {
                player.GlobalPosition = spawnNode.GlobalPosition;
            }

            _entities.AddChild(player);
            EquipDefaultWeapon(player);
            _players.Add(player);
            _gameState.RegisterPlayer(i);
            _camera?.RegisterPlayer(player);
            GD.Print($"[LVL] Player {i} spawned at {player.GlobalPosition}");
        }
    }

    private void RespawnPlayers()
    {
        var spawnPoints = _playerSpawns.GetChildren();

        for (int i = 0; i < _players.Count && i < spawnPoints.Count; i++)
        {
            if (_players[i].IsDown || _players[i].IsDead)
            {
                _players[i].Revive();
            }

            if (spawnPoints[i] is Node2D spawnNode)
            {
                _players[i].GlobalPosition = spawnNode.GlobalPosition;
            }
        }

        _gameState.UnregisterAllPlayers();
        for (int i = 0; i < _players.Count; i++)
        {
            _gameState.RegisterPlayer(i);
        }

        _downPlayer = null;
        _reviveHoldTimer = 0f;
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
        foreach (Node child in _entities.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void FindDownPlayer()
    {
        _downPlayer = _players.FirstOrDefault(p => p.IsDown);
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
            && reviver.GetNode<InputManager>("/root/InputManager")
                .IsActionPressed(reviver.PlayerIndex, InputActions.PrimaryAttack))
        {
            _reviveHoldTimer += delta;
            if (_reviveHoldTimer >= ReviveHoldDuration)
            {
                _downPlayer.Revive();
                _gameState.RevivePlayer(_players.IndexOf(_downPlayer));
                _downPlayer = null;
                _reviveHoldTimer = 0f;
            }
        }
        else
        {
            _reviveHoldTimer = 0f;
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
        _entityPool.PreWarm(AssetKeys.SceneProjectile, 20);
    }
}
