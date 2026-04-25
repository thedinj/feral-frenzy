using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Weapons;

public interface IPlayerProjectile
{
    void InitializeFromWeapon(Vector2 direction, float speed, float impact, PlayerController? firedBy);
}
