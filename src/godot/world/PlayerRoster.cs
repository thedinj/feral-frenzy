using System;

namespace FeralFrenzy.Godot.World;

public sealed class PlayerRoster
{
    public enum DownResult
    {
        OneDown,
        AllDown,
    }

    private int _total;
    private int _downCount;

    public int AliveCount => Math.Max(0, _total - _downCount);

    public void Reset(int playerCount)
    {
        _total = playerCount;
        _downCount = 0;
    }

    public DownResult MarkDown()
    {
        _downCount++;
        return _downCount >= _total ? DownResult.AllDown : DownResult.OneDown;
    }

    public void MarkRevived()
    {
        if (_downCount > 0)
        {
            _downCount--;
        }
    }

    public void EliminateDownedPlayers()
    {
        _total = Math.Max(0, _total - _downCount);
        _downCount = 0;
    }
}
