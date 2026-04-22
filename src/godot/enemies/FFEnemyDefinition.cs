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
}
