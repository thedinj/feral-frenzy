using System;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.Characters;

public partial class PlayerController : CharacterBody2D
{
    [Export]
    public FFCharacterDefinition? Definition { get; set; }

    [Export]
    public int PlayerIndex { get; set; } = 0;

    private const float Gravity = 600f;

    // Initialized in _Ready — Godot does not call _Ready during construction
    private InputManager _input = null!;

    public override void _Ready()
    {
        if (Definition is null)
        {
            throw new InvalidOperationException(
                $"{nameof(PlayerController)} on '{Name}': Definition export is not assigned.");
        }

        _input = GetNode<InputManager>("/root/InputManager");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        if (!IsOnFloor())
        {
            velocity.Y += Gravity * (float)delta;
        }

        if (_input.IsActionJustPressed(PlayerIndex, InputActions.Jump) && IsOnFloor())
        {
            velocity.Y = Definition!.JumpVelocity;
        }

        float dir = _input.GetAxis(PlayerIndex, InputActions.MoveLeft, InputActions.MoveRight);
        velocity.X = dir * Definition!.MoveSpeed;

        Velocity = velocity;
        MoveAndSlide();
    }
}
