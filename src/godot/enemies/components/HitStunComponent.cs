using Godot;

namespace FeralFrenzy.Godot.Enemies.Components;

public partial class HitStunComponent : Node
{
    public bool IsActive { get; private set; }

    private float _timer;

    public void Activate(float seconds)
    {
        IsActive = true;
        _timer = seconds;
    }

    public void Tick(float delta)
    {
        if (!IsActive)
        {
            return;
        }

        _timer -= delta;
        if (_timer <= 0f)
        {
            IsActive = false;
        }
    }
}
