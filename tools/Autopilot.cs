using System.Linq;
using Godot;

namespace OrbRunner.Tools;

/// <summary>
/// Plays the game by itself (used to record the README GIF):
/// heads to the nearest orb and steers away from enemies that get close.
/// Run it with: godot --path . res://tools/Autopilot.tscn
/// </summary>
public partial class Autopilot : Node
{
    private Main _main;
    private Vector2 _lastPosition;
    private int _stuckFrames;
    private int _detourFrames;
    private Vector2 _detour;

    public override void _Ready()
    {
        SaveData.FilePath = "user://autopilot_save.cfg";
        _main = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<Main>();
        AddChild(_main);
    }

    public override void _PhysicsProcess(double delta)
    {
        var player = _main.Player;
        var target = _main.Orbs.GetChildren().OfType<Orb>()
            .OrderBy(orb => orb.GlobalPosition.DistanceSquaredTo(player.GlobalPosition))
            .FirstOrDefault();

        var steer = target != null ? (target.GlobalPosition - player.GlobalPosition).Normalized() : Vector2.Zero;

        foreach (var enemy in _main.Enemies.GetChildren().OfType<Enemy>())
        {
            var away = player.GlobalPosition - enemy.GlobalPosition;
            if (away.Length() < 95f)
                steer += away.Normalized() * 1.6f;
        }

        // Stuck against a wall: walk sideways for a moment to go around it.
        _stuckFrames = player.GlobalPosition.DistanceTo(_lastPosition) < 1f && steer != Vector2.Zero ? _stuckFrames + 1 : 0;
        _lastPosition = player.GlobalPosition;
        if (_stuckFrames > 6 && _detourFrames == 0)
        {
            _detour = steer.Orthogonal().Normalized();
            if (GD.Randf() < 0.5f)
                _detour = -_detour;
            _detourFrames = 24;
        }
        if (_detourFrames > 0)
        {
            _detourFrames--;
            steer = _detour;
        }

        Press("move_right", steer.X);
        Press("move_left", -steer.X);
        Press("move_down", steer.Y);
        Press("move_up", -steer.Y);
    }

    private static void Press(string action, float strength)
    {
        if (strength > 0.15f)
            Input.ActionPress(action, Mathf.Min(strength, 1f));
        else
            Input.ActionRelease(action);
    }
}
