using System;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using FeralFrenzy.Godot.World;
using Godot;

namespace FeralFrenzy.Godot.Weapons;

public partial class WeaponController : Node2D
{
    private const float RapidFireMultiplier = 0.3f;
    private const float RapidFireDuration = 10f;

    [Export]
    public FFWeaponDefinition? Definition { get; set; }

    private float _fireCooldown;
    private float _rapidFireTimer;

    public override void _Ready()
    {
        if (Definition is null)
        {
            throw new InvalidOperationException(
                $"{nameof(WeaponController)} '{Name}': Definition not assigned.");
        }
    }

    public override void _Process(double delta)
    {
        if (_fireCooldown > 0f)
        {
            _fireCooldown -= (float)delta;
        }

        if (_rapidFireTimer > 0f)
        {
            _rapidFireTimer -= (float)delta;
        }
    }

    public void Fire(AimDirection direction, float damageMultiplier)
    {
        if (_fireCooldown > 0f)
        {
            return;
        }

        float rate = _rapidFireTimer > 0f
            ? Definition!.FireRate * RapidFireMultiplier
            : Definition!.FireRate;

        _fireCooldown = rate;
        SpawnProjectile(direction, damageMultiplier);
    }

    public void ActivateRapidFire()
    {
        _rapidFireTimer = RapidFireDuration;
    }

    private static Vector2 AimDirectionToVector(AimDirection dir)
    {
        return dir switch
        {
            AimDirection.Right => Vector2.Right,
            AimDirection.Left => Vector2.Left,
            AimDirection.Up => Vector2.Up,
            AimDirection.Down => Vector2.Down,
            AimDirection.UpRight => new Vector2(1f, -1f).Normalized(),
            AimDirection.UpLeft => new Vector2(-1f, -1f).Normalized(),
            AimDirection.DownRight => new Vector2(1f, 1f).Normalized(),
            AimDirection.DownLeft => new Vector2(-1f, 1f).Normalized(),
            _ => Vector2.Right,
        };
    }

    private void SpawnProjectile(AimDirection direction, float damageMultiplier)
    {
        string projectileKey = !string.IsNullOrEmpty(Definition!.ProjectileKey)
            ? Definition.ProjectileKey
            : AssetKeys.SceneProjectile;

        PackedScene? scene = GetNode<AssetRegistry>(AutoloadPaths.AssetRegistry).GetScene(projectileKey);
        if (scene is null)
        {
            return;
        }

        Vector2 dir = AimDirectionToVector(direction);
        float impact = Definition.BaseImpact * damageMultiplier;
        Area2D node = scene.Instantiate<Area2D>();
        node.GlobalPosition = GlobalPosition;

        if (node is IPlayerProjectile proj)
        {
            PlayerController? firedBy = GetParent()?.GetParent() as PlayerController;
            proj.InitializeFromWeapon(dir, Definition.ProjectileSpeed, impact, firedBy);
        }

        if (LevelController.Instance is not null)
        {
            LevelController.Instance.AddToEntities(node);
        }
        else
        {
            GetTree().Root.AddChild(node);
        }
    }
}
