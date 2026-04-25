using FeralFrenzy.Core.Data.Engine;
using Godot;

namespace FeralFrenzy.Godot.World;

[GlobalClass]
public partial class FFPowerUpDefinition : Resource
{
    [Export]
    public string PowerUpKey { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export]
    public RewardNodeType Type { get; set; } = RewardNodeType.Positive;

    [Export]
    public string EffectKey { get; set; } = string.Empty;

    [Export]
    public string SpriteKey { get; set; } = string.Empty;
}
