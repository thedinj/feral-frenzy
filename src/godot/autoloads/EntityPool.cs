using System;
using System.Collections.Generic;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.Autoloads;

public partial class EntityPool : Node
{
    // Phase 1: lazy pool, pre-warms on explicit request.
    // Phase 2: dynamic sizing based on DifficultyBudget.
    private readonly Dictionary<string, Queue<Node>> _pools = new Dictionary<string, Queue<Node>>();

    // Initialized in _Ready — Godot does not call _Ready during construction
    private AssetRegistry _registry = null!;

    public override void _Ready()
    {
        _registry = GetNode<AssetRegistry>(AutoloadPaths.AssetRegistry);
    }

    public T Get<T>(string sceneKey)
        where T : Node
    {
        if (_pools.TryGetValue(sceneKey, out Queue<Node>? pool) && pool.Count > 0)
        {
            T entity = (T)pool.Dequeue();
            entity.ProcessMode = ProcessModeEnum.Inherit;
            return entity;
        }

        return InstantiateNew<T>(sceneKey);
    }

    public void Return(string sceneKey, Node entity)
    {
        entity.ProcessMode = ProcessModeEnum.Disabled;

        if (entity.GetParent() is not null)
        {
            entity.GetParent().RemoveChild(entity);
        }

        if (!_pools.ContainsKey(sceneKey))
        {
            _pools[sceneKey] = new Queue<Node>();
        }

        _pools[sceneKey].Enqueue(entity);
    }

    public void PreWarm(string sceneKey, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Node entity = InstantiateNew<Node>(sceneKey);
            Return(sceneKey, entity);
        }
    }

    private T InstantiateNew<T>(string sceneKey)
        where T : Node
    {
        PackedScene? scene = _registry.GetScene(sceneKey);
        if (scene is null)
        {
            throw new InvalidOperationException(
                $"EntityPool: scene key '{sceneKey}' not found in AssetRegistry.");
        }

        return scene.Instantiate<T>();
    }
}
