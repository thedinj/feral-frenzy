namespace FeralFrenzy.Godot.Characters;

public class SpeedUpEffect : StatusEffect, ISpeedModifier
{
    public float SpeedMultiplier => 1.3f;

    public SpeedUpEffect(float duration = 10f)
        : base(duration)
    {
    }
}
