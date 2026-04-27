namespace FeralFrenzy.Core.Data.Content;

/// <summary>
/// Shared state enum for all simple enemies.
/// Use a custom enum only if this doesn't fit the enemy's state model.
/// </summary>
public enum FFSimpleEnemyState
{
    Idle,
    Walk,
    Attack,
    Hit,
    Death,
}
