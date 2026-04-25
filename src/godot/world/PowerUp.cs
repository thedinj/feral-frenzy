using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.World;

public partial class PowerUp : Area2D
{
    private static readonly Color PositiveColor = new Color(1.0f, 0.85f, 0.1f);
    private static readonly Color NegativeColor = new Color(1.0f, 0.2f, 0.2f);
    private static readonly Color DoubleEdgedColor = new Color(0.7f, 0.2f, 1.0f);

    [Export]
    public FFPowerUpDefinition? Definition { get; set; }

    public override void _Ready()
    {
        AddToGroup("pickups");
        BodyEntered += OnBodyEntered;

        if (Definition is not null)
        {
            Modulate = Definition.Type switch
            {
                RewardNodeType.Positive => PositiveColor,
                RewardNodeType.Negative => NegativeColor,
                RewardNodeType.DoubleEdged => DoubleEdgedColor,
                _ => Colors.White,
            };
        }
    }

    public void Collect(PlayerController player)
    {
        ApplyEffect(player);
        QueueFree();
    }

    private void OnBodyEntered(Node body)
    {
        if (body is not PlayerController player)
        {
            return;
        }

        Collect(player);
    }

    private void ApplyEffect(PlayerController player)
    {
        if (Definition is null)
        {
            return;
        }

        StatusEffect? effect = Definition.EffectKey switch
        {
            PowerUpEffects.RapidFire => new RapidFireEffect(),
            PowerUpEffects.DamageUp => new DamageUpEffect(),
            PowerUpEffects.SpeedUp => new SpeedUpEffect(),
            PowerUpEffects.HpRestore => new HpRestoreEffect(),
            PowerUpEffects.ReverseControls => new ReverseControlsEffect(),
            PowerUpEffects.Slow => new SlowEffect(),
            PowerUpEffects.Berserker => new BerserkerEffect(),
            PowerUpEffects.Magnet => new MagnetEffect(),
            _ => null,
        };

        if (effect is not null)
        {
            player.ApplyStatusEffect(effect);
        }
    }
}
