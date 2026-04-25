using System.Collections.Generic;
using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.World;

public partial class ExitTrigger : Area2D
{
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body)
    {
        if (body is not PlayerController)
        {
            return;
        }

        GetNode<GameStateManager>(AutoloadPaths.GameStateManager)
            .TransitionTo<RunSummaryState>(
                new RunSummaryPayload(null, true, new List<string>()));
    }
}
