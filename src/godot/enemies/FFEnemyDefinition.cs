using Godot;

namespace FeralFrenzy.Godot.Enemies;

[GlobalClass]
public partial class FFEnemyDefinition : Resource
{
    [Export]
    public string EnemyKey { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export]
    public string ChapterKey { get; set; } = string.Empty;

    [Export]
    public float DifficultyWeight { get; set; } = 1.0f;

    [Export]
    public bool IsEliteVariant { get; set; } = false;

    [Export]
    public string SceneKey { get; set; } = string.Empty;

    [Export]
    public int BaseCountPerPlayer { get; set; } = 2;

    [Export]
    public float MaxHp { get; set; } = 3f;

    [Export]
    public float HitStunSeconds { get; set; } = 0.15f;

    // Per-hit invincibility window — prevents rapid-fire from stacking full damage.
    // 0 = no i-frames (default for regular enemies). Boss uses ~0.4.
    [Export]
    public float InvincibilitySeconds { get; set; } = 0f;
}
