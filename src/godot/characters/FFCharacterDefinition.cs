using FeralFrenzy.Core.Data.Content;
using Godot;

namespace FeralFrenzy.Godot.Characters;

[GlobalClass]
public partial class FFCharacterDefinition : Resource
{
    [Export]
    public string CharacterKey { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export]
    public FFCharacterSize Size { get; set; } = FFCharacterSize.Medium;

    [Export]
    public float MoveSpeed { get; set; } = 120f;

    [Export]
    public float JumpVelocity { get; set; } = -280f;

    [Export]
    public float JumpArcMultiplier { get; set; } = 1.0f;

    [Export]
    public bool AlwaysFitsGaps { get; set; } = false;

    [Export]
    public bool HasExtraHit { get; set; } = false;

    [Export]
    public float WeaponDamageMultiplier { get; set; } = 1.0f;

    [Export]
    public string SecondaryAbilityKey { get; set; } = string.Empty;

    [Export]
    public string SpriteFramesKey { get; set; } = string.Empty;

    [Export]
    public string PortraitKey { get; set; } = string.Empty;

    [Export]
    public int MaxHp { get; set; } = 3;

    [Export]
    public float InvincibilitySeconds { get; set; } = 1.2f;

    [Export]
    public float WallKickVelocityX { get; set; } = 200f;

    [Export]
    public float SlideSpeedMultiplier { get; set; } = 1.4f;

    [Export]
    public float SlideDuration { get; set; } = 0.35f;

    [Export]
    public float JumpBufferDuration { get; set; } = 0.12f;
}
