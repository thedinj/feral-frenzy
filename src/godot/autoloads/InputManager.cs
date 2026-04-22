using System;
using System.Collections.Generic;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.Autoloads;

public partial class InputManager : Node
{
    // Phase 1: player 0 = keyboard, player 1 = gamepad device 0.
    // Full 1–4 player routing: Phase 2.
    private readonly HashSet<JoyButton> _prevPressedButtons = new HashSet<JoyButton>();
    private readonly HashSet<JoyButton> _currPressedButtons = new HashSet<JoyButton>();

    public override void _Process(double delta)
    {
        UpdateGamepadButtonState();
    }

    public bool IsActionJustPressed(int playerIndex, string action)
    {
        return playerIndex switch
        {
            InputConstants.KeyboardPlayerIndex => Input.IsActionJustPressed(action),
            InputConstants.GamepadPlayerIndex => IsGamepadActionJustPressed(action),
            _ => false,
        };
    }

    public bool IsActionPressed(int playerIndex, string action)
    {
        return playerIndex switch
        {
            InputConstants.KeyboardPlayerIndex => Input.IsActionPressed(action),
            InputConstants.GamepadPlayerIndex => IsGamepadActionPressed(action),
            _ => false,
        };
    }

    public float GetAxis(int playerIndex, string negativeAction, string positiveAction)
    {
        if (playerIndex == InputConstants.KeyboardPlayerIndex)
        {
            return Input.GetAxis(negativeAction, positiveAction);
        }

        if (playerIndex == InputConstants.GamepadPlayerIndex)
        {
            float axis = Input.GetJoyAxis(InputConstants.GamepadDevice, JoyAxis.LeftX);
            if (axis < -InputConstants.GamepadDeadZone)
            {
                return -1f;
            }

            if (axis > InputConstants.GamepadDeadZone)
            {
                return 1f;
            }

            return 0f;
        }

        return 0f;
    }

    public bool IsActionJustPressedFromEvent(InputEvent @event, int playerIndex, string action)
    {
        if (playerIndex == InputConstants.KeyboardPlayerIndex)
        {
            return @event is InputEventKey key && key.Pressed && !key.Echo && key.IsAction(action);
        }

        if (playerIndex == InputConstants.GamepadPlayerIndex
            && @event is InputEventJoypadButton joy
            && joy.Pressed
            && joy.Device == InputConstants.GamepadDevice)
        {
            JoyButton? button = ActionToJoyButton(action);
            return button is not null && joy.ButtonIndex == button.Value;
        }

        return false;
    }

    private static JoyButton? ActionToJoyButton(string action)
    {
        return action switch
        {
            InputActions.Jump => JoyButton.A,
            InputActions.PrimaryAttack => JoyButton.X,
            InputActions.SecondaryAttack => JoyButton.Y,
            InputActions.Slide => JoyButton.B,
            InputActions.MoveLeft => JoyButton.DpadLeft,
            InputActions.MoveRight => JoyButton.DpadRight,
            InputActions.AimUp => JoyButton.DpadUp,
            InputActions.AimDown => JoyButton.DpadDown,
            _ => null,
        };
    }

    private void UpdateGamepadButtonState()
    {
        _prevPressedButtons.Clear();
        foreach (JoyButton b in _currPressedButtons)
        {
            _prevPressedButtons.Add(b);
        }

        _currPressedButtons.Clear();
        foreach (JoyButton b in Enum.GetValues<JoyButton>())
        {
            if ((int)b >= 0 && Input.IsJoyButtonPressed(InputConstants.GamepadDevice, b))
            {
                _currPressedButtons.Add(b);
            }
        }
    }

    private bool IsGamepadActionJustPressed(string action)
    {
        JoyButton? button = ActionToJoyButton(action);
        if (button is not null)
        {
            return _currPressedButtons.Contains(button.Value)
                && !_prevPressedButtons.Contains(button.Value);
        }

        return false;
    }

    private bool IsGamepadActionPressed(string action)
    {
        JoyButton? button = ActionToJoyButton(action);
        if (button is not null)
        {
            return _currPressedButtons.Contains(button.Value);
        }

        return action switch
        {
            InputActions.AimUp =>
                Input.GetJoyAxis(InputConstants.GamepadDevice, JoyAxis.RightY) < -InputConstants.GamepadAimThreshold,
            InputActions.AimDown =>
                Input.GetJoyAxis(InputConstants.GamepadDevice, JoyAxis.RightY) > InputConstants.GamepadAimThreshold,
            _ => false,
        };
    }
}
