using System.Collections.Generic;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.Debug;

public partial class HitboxDebugController : Node2D
{
    private readonly List<PackedScene> _scenes = new List<PackedScene>();
    private Node? _activeEntity;
    private int _entityIndex;
    private Label? _label;

    public override void _Ready()
    {
        _label = GetNodeOrNull<Label>("DebugLabel");
        GetTree().DebugCollisionsHint = true;

        AssetRegistry registry = GetNode<AssetRegistry>(AutoloadPaths.AssetRegistry);

        PackedScene? bearScene = registry.GetScene(AssetKeys.SceneCharBear);
        PackedScene? hbScene = registry.GetScene(AssetKeys.SceneCharHoneyBadger);

        if (bearScene is not null)
        {
            _scenes.Add(bearScene);
        }

        if (hbScene is not null)
        {
            _scenes.Add(hbScene);
        }

        if (_scenes.Count > 0)
        {
            SpawnEntity(0);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo
            && key.Keycode == Key.Tab && _scenes.Count > 0)
        {
            _entityIndex = (_entityIndex + 1) % _scenes.Count;
            SpawnEntity(_entityIndex);
        }
    }

    public override void _Process(double delta)
    {
        if (_activeEntity is null || _label is null)
        {
            return;
        }

        AnimationPlayer? animPlayer = _activeEntity.GetNodeOrNull<AnimationPlayer>(NodePaths.AnimationPlayer);
        AnimatedSprite2D? sprite = _activeEntity.GetNodeOrNull<AnimatedSprite2D>(NodePaths.AnimatedSprite);

        string animName = animPlayer?.CurrentAnimation.ToString()
            ?? sprite?.Animation.ToString()
            ?? "none";

        Vector2 position = _activeEntity is Node2D n2d ? n2d.GlobalPosition : Vector2.Zero;

        _label.Text = $"Entity:    {_activeEntity.Name}\n"
            + $"Animation: {animName}\n"
            + $"Position:  {position}\n"
            + "[Tab] cycle entities";
    }

    private void SpawnEntity(int index)
    {
        _activeEntity?.QueueFree();
        _activeEntity = _scenes[index].Instantiate();
        if (_activeEntity is Node2D node2d)
        {
            node2d.GlobalPosition = new Vector2(160f, 80f);
        }

        AddChild(_activeEntity);
    }
}
