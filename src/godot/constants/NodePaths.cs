namespace FeralFrenzy.Godot.Constants;

public static class NodePaths
{
    // Shared character nodes
    public const string AnimatedSprite = "AnimatedSprite2D";
    public const string CollisionShape = "CollisionShape2D";
    public const string WeaponMount = "WeaponMount";

    // Level scene structure
    public const string LevelEntities = "Entities";
    public const string LevelPlayerSpawns = "PlayerSpawns";
    public const string LevelCamera = "CoopCamera";

    // EnemyHost component slot names (fixed child node names, resolved in EnemyHost._Ready)
    public const string EnemyBehavior = "Behavior";
    public const string EnemyGravity = "Gravity";
    public const string EnemyHitStun = "HitStun";
    public const string EnemyDeath = "Death";
    public const string EnemyVisuals = "Visuals";
}
