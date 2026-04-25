using Godot;

namespace FeralFrenzy.Godot.Enemies.Behaviors;

public partial class SummonBehavior : Node, ITickBehavior
{
    [Export]
    public string AssetKey { get; set; } = string.Empty;

    [Export]
    public float OffsetX { get; set; } = 40f;

    public void Tick(EnemyHost host, float delta)
    {
    }

    public void Summon(EnemyHost host)
    {
        if (string.IsNullOrEmpty(AssetKey))
        {
            return;
        }

        host.RequestMinions(AssetKey, new Vector2(-OffsetX, 0f), new Vector2(OffsetX, 0f));
    }
}
