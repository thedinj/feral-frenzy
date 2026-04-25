using System;
using System.Collections.Generic;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.Autoloads;

public partial class InputManager : Node
{
    // Player 0 = gamepad device 0
    // Player 1 = keyboard
    // Players 2-3 = gamepads (device 1, 2)
    private const int MaxGamepadCount = 3;

    private readonly Dictionary<int, HashSet<JoyButton>> _prevButtons =
        new Dictionary<int, HashSet<JoyButton>>();

    private readonly Dictionary<int, HashSet<JoyButton>> _currButtons =
        new Dictionary<int, HashSet<JoyButton>>();

    public override void _Ready()
    {
        for (int i = 0; i < MaxGamepadCount; i++)
        {
            _prevButtons[i] = new HashSet<JoyButton>();
            _currButtons[i] = new HashSet<JoyButton>();
        }
    }

    public override void _Process(double delta)
    {
        UpdateGamepadButtonState();
    }

    public bool IsActionJustPressed(int playerIndex, string action)
    {
        if (playerIndex == InputConstants.KeyboardPlayerIndex)
        {
            return Input.IsActionJustPressed(action);
        }

        int device = ToDevice(playerIndex);
        if (!Input.IsJoyKnown(device))
        {
            return false;
        }

        return IsGamepadActionJustPressed(device, action);
    }

    public bool IsActionPressed(int playerIndex, string action)
    {
        if (playerIndex == InputConstants.KeyboardPlayerIndex)
        {
            return Input.IsActionPressed(action);
        }

        int device = ToDevice(playerIndex);
        if (!Input.IsJoyKnown(device))
        {
            return false;
        }

        return IsGamepadActionPressed(device, action);
    }

    public float GetAxis(int playerIndex, string negativeAction, string positiveAction)
    {
        if (playerIndex == InputConstants.KeyboardPlayerIndex)
        {
            return Input.GetAxis(negativeAction, positiveAction);
        }

        int device = ToDevice(playerIndex);
        if (!Input.IsJoyKnown(device))
        {
            return 0f;
        }

        float axis = Input.GetJoyAxis(device, JoyAxis.LeftX);
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

    public bool IsActionJustPressedFromEvent(InputEvent @event, int playerIndex, string action)
    {
        if (playerIndex == InputConstants.KeyboardPlayerIndex)
        {
            return @event is InputEventKey key && key.Pressed && !key.Echo && key.IsAction(action);
        }

        int device = ToDevice(playerIndex);
        if (!Input.IsJoyKnown(device))
        {
            return false;
        }

        if (@event is InputEventJoypadButton joy && joy.Pressed && joy.Device == device)
        {
            if (action == InputActions.Jump && joy.ButtonIndex == JoyButton.RightStick)
            {
                return true;
            }

            JoyButton? button = ActionToJoyButton(action);
            return button is not null && joy.ButtonIndex == button.Value;
        }

        return false;
    }

    public bool IsAnyButtonJustPressedOnDevice(int device)
    {
        if (!_currButtons.TryGetValue(device, out HashSet<JoyButton>? curr))
        {
            return false;
        }

        if (!_prevButtons.TryGetValue(device, out HashSet<JoyButton>? prev))
        {
            return false;
        }

        foreach (JoyButton b in curr)
        {
            if (!prev.Contains(b))
            {
                return true;
            }
        }

        return false;
    }

    public Vector2 GetLeftStickVector(int playerIndex)
    {
        if (playerIndex == InputConstants.KeyboardPlayerIndex)
        {
            return Vector2.Zero;
        }

        int device = ToDevice(playerIndex);
        if (!Input.IsJoyKnown(device))
        {
            return Vector2.Zero;
        }

        var raw = new Vector2(
            Input.GetJoyAxis(device, JoyAxis.LeftX),
            Input.GetJoyAxis(device, JoyAxis.LeftY));

        return raw.Length() < InputConstants.GamepadDeadZone ? Vector2.Zero : raw;
    }

    public Vector2 GetRightStickVector(int playerIndex)
    {
        if (playerIndex == InputConstants.KeyboardPlayerIndex)
        {
            return Vector2.Zero;
        }

        int device = ToDevice(playerIndex);
        if (!Input.IsJoyKnown(device))
        {
            return Vector2.Zero;
        }

        var raw = new Vector2(
            Input.GetJoyAxis(device, JoyAxis.RightX),
            Input.GetJoyAxis(device, JoyAxis.RightY));

        return raw.Length() < InputConstants.GamepadDeadZone ? Vector2.Zero : raw;
    }

    // Maps a player index to a Godot joypad device index, skipping the keyboard slot.
    private static int ToDevice(int playerIndex) =>
        playerIndex < InputConstants.KeyboardPlayerIndex ? playerIndex : playerIndex - 1;

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
        for (int device = 0; device < MaxGamepadCount; device++)
        {
            _prevButtons[device].Clear();
            foreach (JoyButton b in _currButtons[device])
            {
                _prevButtons[device].Add(b);
            }

            _currButtons[device].Clear();

            if (!Input.IsJoyKnown(device))
            {
                continue;
            }

            foreach (JoyButton b in Enum.GetValues<JoyButton>())
            {
                if ((int)b >= 0 && Input.IsJoyButtonPressed(device, b))
                {
                    _currButtons[device].Add(b);
                }
            }
        }
    }

    private bool IsGamepadActionJustPressed(int device, string action)
    {
        JoyButton? button = ActionToJoyButton(action);
        if (button is null)
        {
            return false;
        }

        return _currButtons.TryGetValue(device, out HashSet<JoyButton>? curr)
            && curr.Contains(button.Value)
            && _prevButtons.TryGetValue(device, out HashSet<JoyButton>? prev)
            && !prev.Contains(button.Value);
    }

    private bool IsGamepadActionPressed(int device, string action)
    {
        JoyButton? button = ActionToJoyButton(action);
        if (button is not null)
        {
            return _currButtons.TryGetValue(device, out HashSet<JoyButton>? curr)
                && curr.Contains(button.Value);
        }

        return action switch
        {
            InputActions.AimUp =>
                Input.GetJoyAxis(device, JoyAxis.RightY) < -InputConstants.GamepadAimThreshold,
            InputActions.AimDown =>
                Input.GetJoyAxis(device, JoyAxis.RightY) > InputConstants.GamepadAimThreshold,
            _ => false,
        };
    }
}
