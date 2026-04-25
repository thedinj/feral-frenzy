using System;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.Weapons;

public partial class WeaponController : Node2D
{
    [Signal]
    public delegate void ProjectileSpawnedEventHandler(Node2D projectile);

    [Export]
    public FFWeaponDefinition? Definition { get; set; }

    private float _fireCooldown;
    private float _rapidFireTimer;

    // assigned in _Ready()
    private FFWeaponDefinition _definition = null!;

    public override void _Ready()
    {
        _definition = Definition
            ?? throw new InvalidOperationException(
                $"{nameof(WeaponController)} '{Name}': Definition not assigned.");
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
        SpawnIfReady(AimDirectionToVector(direction), damageMultiplier);
    }

    public void FireRaw(Vector2 direction, float damageMultiplier)
    {
        SpawnIfReady(direction.Normalized(), damageMultiplier);
    }

    public void ActivateRapidFire()
    {
        _rapidFireTimer = _definition.RapidFireDuration;
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

    private void SpawnIfReady(Vector2 direction, float damageMultiplier)
    {
        if (_fireCooldown > 0f)
        {
            return;
        }

        _fireCooldown = _rapidFireTimer > 0f
            ? _definition.FireRate * _definition.RapidFireMultiplier
            : _definition.FireRate;

        SpawnProjectile(direction, damageMultiplier);
    }

    private void SpawnProjectile(Vector2 direction, float damageMultiplier)
    {
        string projectileKey = !string.IsNullOrEmpty(_definition.ProjectileKey)
            ? _definition.ProjectileKey
            : AssetKeys.SceneProjectile;

        PackedScene? scene = GetNode<AssetRegistry>(AutoloadPaths.AssetRegistry).GetScene(projectileKey);
        if (scene is null)
        {
            return;
        }

        float impact = _definition.BaseImpact * damageMultiplier;
        Area2D node = scene.Instantiate<Area2D>();
        node.GlobalPosition = GlobalPosition;

        if (node is IPlayerProjectile proj)
        {
            PlayerController? firedBy = GetParent()?.GetParent() as PlayerController;
            proj.InitializeFromWeapon(direction, _definition.ProjectileSpeed, impact, firedBy);
        }

        EmitSignal(SignalName.ProjectileSpawned, node);
    }
}
