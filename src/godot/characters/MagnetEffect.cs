using FeralFrenzy.Godot.World;
using Godot;

namespace FeralFrenzy.Godot.Characters;

public class MagnetEffect : StatusEffect
{
    private const float MagnetRadius = 80f;
    private const float MagnetCatchRadius = 12f;
    private const float MagnetPullSpeed = 200f;

    public MagnetEffect(float duration = 8f)
        : base(duration)
    {
    }

    public override void OnTick(PlayerController player, float delta)
    {
        var pickups = player.GetTree().GetNodesInGroup("pickups");
        foreach (Node node in pickups)
        {
            if (node is not PowerUp pickup)
            {
                continue;
            }

            if (!GodotObject.IsInstanceValid(pickup))
            {
                continue;
            }

            float dist = player.GlobalPosition.DistanceTo(pickup.GlobalPosition);
            if (dist > MagnetRadius)
            {
                continue;
            }

            if (dist < MagnetCatchRadius)
            {
                pickup.Collect(player);
            }
            else
            {
                pickup.GlobalPosition = pickup.GlobalPosition.MoveToward(
                    player.GlobalPosition,
                    MagnetPullSpeed * delta);
            }
        }
    }
}
