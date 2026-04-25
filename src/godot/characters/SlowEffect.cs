namespace FeralFrenzy.Godot.Characters;

public class SlowEffect : StatusEffect, ISpeedModifier
{
    public float SpeedMultiplier => 0.5f;

    public SlowEffect(float duration = 8f)
        : base(duration)
    {
    }
}
