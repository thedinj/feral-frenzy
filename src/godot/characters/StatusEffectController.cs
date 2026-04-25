using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FeralFrenzy.Godot.Characters;

public partial class StatusEffectController : Node
{
    private readonly List<StatusEffect> _activeEffects = new List<StatusEffect>();
    private PlayerController _player = null!;

    public override void _Ready()
    {
        _player = GetParent<PlayerController>();
    }

    public override void _Process(double delta)
    {
        TickEffects((float)delta);
    }

    public void Apply(StatusEffect effect)
    {
        effect.OnApply(_player);

        if (!effect.IsExpired)
        {
            _activeEffects.Add(effect);
        }
    }

    public float GetDamageMultiplier()
        => _activeEffects.OfType<IDamageModifier>()
            .Aggregate(1f, (acc, e) => acc * e.DamageMultiplier);

    public float GetSpeedMultiplier()
        => _activeEffects.OfType<ISpeedModifier>()
            .Aggregate(1f, (acc, e) => acc * e.SpeedMultiplier);

    public float GetIncomingDamageMultiplier()
        => _activeEffects.OfType<IIncomingDamageModifier>()
            .Aggregate(1f, (acc, e) => acc * e.IncomingDamageMultiplier);

    public bool AreControlsReversed()
        => _activeEffects.OfType<ReverseControlsEffect>().Any();

    private void TickEffects(float delta)
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            _activeEffects[i].OnTick(_player, delta);
            _activeEffects[i].Tick(delta);

            if (_activeEffects[i].IsExpired)
            {
                _activeEffects[i].OnRemove(_player);
                _activeEffects.RemoveAt(i);
            }
        }
    }
}
