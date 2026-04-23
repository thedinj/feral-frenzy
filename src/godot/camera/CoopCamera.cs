using System.Collections.Generic;
using System.Linq;
using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Camera;

public partial class CoopCamera : Camera2D
{
    private readonly List<PlayerController> _players = new List<PlayerController>();

    // <1.0 zooms out — 0.5 shows 640 world-units wide at 320×180 base resolution.
    // Tune this export in the editor until the view reads as a wide battlefield.
    [Export]
    private float _zoomLevel = 0.65f;

    [Export]
    private float _followSpeed = 4f;

    private bool _snapOnNextFrame;

    public override void _Ready()
    {
        Zoom = new Vector2(_zoomLevel, _zoomLevel);
    }

    public override void _Process(double delta)
    {
        List<PlayerController> alive = _players
            .Where(p => !p.IsDown && !p.IsDead)
            .ToList();

        if (alive.Count == 0)
        {
            return;
        }

        Vector2 sum = Vector2.Zero;
        foreach (PlayerController p in alive)
        {
            sum += p.GlobalPosition;
        }

        Vector2 target = sum / alive.Count;

        if (_snapOnNextFrame)
        {
            GlobalPosition = target;
            _snapOnNextFrame = false;
        }
        else
        {
            GlobalPosition = GlobalPosition.Lerp(target, _followSpeed * (float)delta);
        }
    }

    public void RegisterPlayer(PlayerController player)
    {
        if (_players.Contains(player))
        {
            return;
        }

        _players.Add(player);

        if (_players.Count == 1)
        {
            _snapOnNextFrame = true;
        }
    }

    public void UnregisterPlayer(PlayerController player)
    {
        _players.Remove(player);
    }

    public void ClearPlayers()
    {
        _players.Clear();
        _snapOnNextFrame = false;
    }
}
