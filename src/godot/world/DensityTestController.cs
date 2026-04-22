using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.Enemies;
using Godot;

namespace FeralFrenzy.Godot.World;

public partial class DensityTestController : Node2D
{
    private const int TargetEntityCount = 30;

    private float _fpsLogTimer;

    // Initialized in _Ready — Godot does not call _Ready during construction
    private AssetRegistry _registry = null!;
    private Node2D? _spawnRoot;

    public override void _Ready()
    {
        _registry = GetNode<AssetRegistry>("/root/AssetRegistry");
        _spawnRoot = GetNodeOrNull<Node2D>("Entities");
        SpawnEntities();
    }

    public override void _Process(double delta)
    {
        _fpsLogTimer -= (float)delta;
        if (_fpsLogTimer <= 0f)
        {
            GD.Print($"DensityTest FPS: {Engine.GetFramesPerSecond()}, entities: {TargetEntityCount}");
            _fpsLogTimer = 5f;
        }
    }

    private void SpawnEntities()
    {
        int groundCount = (TargetEntityCount * 2) / 3;
        int aerialCount = TargetEntityCount - groundCount;

        SpawnEnemies(AssetKeys.SceneEnemyGroundPatroller, groundCount, yOffset: 144f);
        SpawnEnemies(AssetKeys.SceneEnemyAerialDiver, aerialCount, yOffset: 80f);
    }

    private void SpawnEnemies(string sceneKey, int count, float yOffset)
    {
        PackedScene? scene = _registry.GetScene(sceneKey);
        if (scene is null)
        {
            return;
        }

        Node spawnParent = _spawnRoot ?? this;
        float spacing = 800f / (count + 1);

        for (int i = 0; i < count; i++)
        {
            EnemyController enemy = scene.Instantiate<EnemyController>();
            enemy.GlobalPosition = new Vector2(spacing * (i + 1), yOffset);
            spawnParent.AddChild(enemy);
        }
    }
}
