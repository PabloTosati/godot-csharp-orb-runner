using Godot;

namespace OrbRunner;

/// <summary>A collectible orb. Emits Collected exactly once.</summary>
public partial class Orb : Area2D
{
    [Signal]
    public delegate void CollectedEventHandler(Orb orb);

    [Export] public int Value { get; set; } = 1;

    private static readonly Color CoreColor = new("ffd166");
    private static readonly Color GlowColor = new(1f, 0.82f, 0.4f, 0.18f);

    private bool _collected;
    private float _time;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        _time = GD.Randf() * Mathf.Tau;
    }

    private void OnBodyEntered(Node2D body)
    {
        // Guard: QueueFree only removes the node at the end of the frame, so this
        // handler could run again before that (for example, if the signal is
        // connected twice) and count the orb twice.
        if (_collected || body is not Player)
            return;

        _collected = true;
        // Physics state can't be changed while Godot is processing collisions, so defer it.
        SetDeferred(Area2D.PropertyName.Monitoring, false);
        EmitSignal(SignalName.Collected, this);
        QueueFree();
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float radius = 9f + Mathf.Sin(_time * 4f) * 1.5f;
        DrawCircle(Vector2.Zero, radius + 7f, GlowColor);
        DrawCircle(Vector2.Zero, radius, CoreColor);
        DrawCircle(new Vector2(-3, -3), 2.5f, new Color(1, 1, 1, 0.8f));
    }
}
