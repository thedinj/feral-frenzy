using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.Enemies.Behaviors;

public partial class DefaultDeath : Node, IDeathBehavior
{
    public void Execute(EnemyHost host)
    {
        AnimatedSprite2D? sprite = host.GetNodeOrNull<AnimatedSprite2D>(NodePaths.AnimatedSprite);

        if (sprite is not null && sprite.SpriteFrames?.HasAnimation(AnimationNames.Death) == true)
        {
            sprite.SpriteFrames.SetAnimationLoop(AnimationNames.Death, false);
            sprite.Play(AnimationNames.Death);
            sprite.AnimationFinished += host.QueueFree;
            return;
        }

        CanvasItem target = sprite ?? (CanvasItem)host;
        Tween tween = host.GetTree().CreateTween();
        tween.TweenProperty(target, "modulate:a", 0f, 0.3f);
        tween.TweenCallback(Callable.From(host.QueueFree));
    }
}
