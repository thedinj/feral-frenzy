using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.Enemies.Components;

public partial class EnemyVisuals : Node
{
    public override void _Ready()
    {
        EnemyHost host = GetParent<EnemyHost>();
        host.HpChanged += OnHpChanged;
    }

    private void OnHpChanged(float current, float max)
    {
        AnimatedSprite2D? sprite = GetNodeOrNull<AnimatedSprite2D>(
            "../" + NodePaths.AnimatedSprite);
        if (sprite is null)
        {
            return;
        }

        sprite.Modulate = Colors.White * 2f;
        GetTree().CreateTimer(0.08f).Timeout += () => sprite.Modulate = Colors.White;
    }
}
