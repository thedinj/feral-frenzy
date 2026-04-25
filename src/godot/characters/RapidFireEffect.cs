namespace FeralFrenzy.Godot.Characters;

public class RapidFireEffect : StatusEffect
{
    public RapidFireEffect(float duration = 10f)
        : base(duration)
    {
    }

    public override void OnApply(PlayerController player)
    {
        player.GetEquippedWeapon()?.ActivateRapidFire();
    }
}
