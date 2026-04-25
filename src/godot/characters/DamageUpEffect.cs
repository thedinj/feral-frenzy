namespace FeralFrenzy.Godot.Characters;

public class DamageUpEffect : StatusEffect, IDamageModifier
{
    public float DamageMultiplier => 1.5f;

    public DamageUpEffect(float duration = 10f)
        : base(duration)
    {
    }
}
