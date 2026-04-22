using System;
using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Weapons;

public partial class WeaponPickup : Area2D
{
    [Export]
    public FFWeaponDefinition? WeaponDefinition { get; set; }

    public override void _Ready()
    {
        if (WeaponDefinition is null)
        {
            throw new InvalidOperationException(
                $"{nameof(WeaponPickup)} '{Name}': WeaponDefinition not assigned.");
        }

        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body)
    {
        if (body is not PlayerController player)
        {
            return;
        }

        // Create a bare WeaponController node and assign the definition before
        // AddChild fires _Ready — Definition is validated there.
        WeaponController weapon = new WeaponController
        {
            Definition = WeaponDefinition,
        };

        player.EquipWeapon(weapon);
        QueueFree();
    }
}
