using Godot;

namespace FeralFrenzy.Godot.World;

[GlobalClass]
public partial class LevelConfig : Resource
{
    [Export]
    public float GravityScale { get; set; } = 1.0f;

    [Export]
    public float ReviveWindowSeconds { get; set; } = 10f;

    [Export]
    public float ReviveHoldDuration { get; set; } = 2f;

    [Export]
    public float ReviveProximityUnits { get; set; } = 32f;

    [Export]
    public Vector2 BossSpawnPosition { get; set; } = new Vector2(160f, 155f);

    [Export]
    public Vector2 ExtraPatrollerSpawnPosition { get; set; } = new Vector2(460f, 155f);

    [Export]
    public int PoolWarmGroundPatroller { get; set; } = 8;

    [Export]
    public int PoolWarmAerialDiver { get; set; } = 4;

    [Export]
    public int PoolWarmMountedDino { get; set; } = 2;

    [Export]
    public int PoolWarmPteroBomber { get; set; } = 2;

    [Export]
    public int PoolWarmProjectile { get; set; } = 20;

    [Export]
    public int PoolWarmSpinningBlade { get; set; } = 4;
}
