using System.Collections.Generic;
using System.Linq;
using FeralFrenzy.Godot.Characters;
using Godot;

namespace FeralFrenzy.Godot.Camera;

public partial class CoopCamera : Camera2D
{
    private readonly List<PlayerController> _players = new List<PlayerController>();

    [Export]
    private float _followSpeed = 4f;

    // <1.0 = zoomed out, gives panoramic battlefield view
    [Export]
    private float _zoomLevel = 0.7f;

    private bool _snapOnNextFrame;

    public override void _Ready()
    {
        Zoom = new Vector2(_zoomLevel, _zoomLevel);
    }

    private float _debugLogTimer;

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

        Vector2 targetPos = sum / alive.Count;

        if (_snapOnNextFrame)
        {
            GlobalPosition = targetPos;
            _snapOnNextFrame = false;
            GD.Print($"[CAM] Snapped to {GlobalPosition}");
        }
        else
        {
            GlobalPosition = GlobalPosition.Lerp(targetPos, _followSpeed * (float)delta);
        }

        _debugLogTimer -= (float)delta;
        if (_debugLogTimer <= 0f)
        {
            GD.Print($"[CAM] pos={GlobalPosition} zoom={Zoom} target={targetPos} players={alive.Count}");
            _debugLogTimer = 3f;
        }
    }

    public void RegisterPlayer(PlayerController player)
    {
        if (!_players.Contains(player))
        {
            _players.Add(player);
            GD.Print($"[CAM] RegisterPlayer: {player.Name} at {player.GlobalPosition}, total={_players.Count}");
            if (_players.Count == 1)
            {
                _snapOnNextFrame = true;
            }
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
