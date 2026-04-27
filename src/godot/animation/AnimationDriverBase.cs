using FeralFrenzy.Godot.Core;
using Godot;

namespace FeralFrenzy.Godot.Animation;

public abstract partial class AnimationDriverBase : Node, IEntitySubsystem
{
    public abstract void Tick();
}
