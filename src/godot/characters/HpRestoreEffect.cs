namespace FeralFrenzy.Godot.Characters;

public class HpRestoreEffect : StatusEffect
{
    public HpRestoreEffect()
        : base(0f)
    {
    }

    public override void OnApply(PlayerController player)
    {
        player.RestoreHp(1);
    }
}
