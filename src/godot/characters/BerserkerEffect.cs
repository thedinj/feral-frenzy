namespace FeralFrenzy.Godot.Characters;

public class BerserkerEffect : StatusEffect, IDamageModifier, IIncomingDamageModifier
{
    public float DamageMultiplier => 2.0f;

    public float IncomingDamageMultiplier => 2.0f;

    public BerserkerEffect(float duration = 12f)
        : base(duration)
    {
    }
}
