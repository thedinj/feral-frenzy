namespace FeralFrenzy.Godot.Characters;

public interface IDamageModifier
{
    float DamageMultiplier { get; }
}

public interface ISpeedModifier
{
    float SpeedMultiplier { get; }
}

public interface IIncomingDamageModifier
{
    float IncomingDamageMultiplier { get; }
}

public abstract class StatusEffect
{
    private float _remaining;

    public bool IsExpired => _remaining <= 0f;

    protected StatusEffect(float duration)
    {
        _remaining = duration;
    }

    public void Tick(float delta)
    {
        if (_remaining > 0f)
        {
            _remaining -= delta;
        }
    }

    public virtual void OnTick(PlayerController player, float delta)
    {
    }

    public virtual void OnApply(PlayerController player)
    {
    }

    public virtual void OnRemove(PlayerController player)
    {
    }
}
