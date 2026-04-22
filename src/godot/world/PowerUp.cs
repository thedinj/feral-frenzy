using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Weapons;
using Godot;

namespace FeralFrenzy.Godot.World;

public partial class PowerUp : Area2D
{
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body)
    {
        if (body is not PlayerController player)
        {
            return;
        }

        // Find equipped weapon on the player and activate rapid fire
        Node? weaponMount = player.GetNodeOrNull("WeaponMount");
        if (weaponMount is not null && weaponMount.GetChildCount() > 0)
        {
            if (weaponMount.GetChild(0) is WeaponController weapon)
            {
                weapon.ActivateRapidFire();
            }
        }

        QueueFree();
    }
}
