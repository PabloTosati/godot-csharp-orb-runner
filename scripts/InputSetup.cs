using Godot;

namespace OrbRunner;

/// <summary>
/// Registers the input actions in code, so the controls work in any project copy.
/// Physical keys are used, so WASD stays in the same place on AZERTY or other layouts.
/// </summary>
public static class InputSetup
{
    public static void EnsureActions()
    {
        Add("move_left", Key.A, Key.Left);
        Add("move_right", Key.D, Key.Right);
        Add("move_up", Key.W, Key.Up);
        Add("move_down", Key.S, Key.Down);
        Add("restart", Key.R, Key.Enter);
    }

    private static void Add(string action, params Key[] keys)
    {
        if (InputMap.HasAction(action))
            return;

        InputMap.AddAction(action);
        foreach (var key in keys)
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }
}
