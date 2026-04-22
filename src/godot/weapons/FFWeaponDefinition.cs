using FeralFrenzy.Core.Data.Content;
using Godot;

namespace FeralFrenzy.Godot.Weapons;

[GlobalClass]
public partial class FFWeaponDefinition : Resource
{
    [Export]
    public string WeaponKey { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export]
    public FFWeaponTier Tier { get; set; } = FFWeaponTier.Default;

    [Export]
    public bool IsChargeable { get; set; } = false;

    [Export]
    public bool IsExplosive { get; set; } = false;

    // Always true — non-negotiable per bible
    [Export]
    public bool EightDirectional { get; set; } = true;

    [Export]
    public float FireRate { get; set; } = 0.12f;

    [Export]
    public float ProjectileSpeed { get; set; } = 400f;

    [Export]
    public float BaseImpact { get; set; } = 1.0f;

    [Export]
    public string ProjectileKey { get; set; } = string.Empty;

    [Export]
    public string SpriteKey { get; set; } = string.Empty;

    [Export]
    public string SoundKey { get; set; } = string.Empty;
}
