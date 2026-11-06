using Godot;

namespace OrbRunner;

/// <summary>Patrols the arena, bouncing off its edges, and hurts the player on contact.</summary>
public partial class Enemy : Area2D
{
    [Export] public float Speed { get; set; } = 150f;

    public Vector2 Direction { get; set; } = Vector2.Right;

    /// <summary>Area the enemy's center must stay inside.</summary>
    public Rect2 Bounds { get; set; } = new(34, 34, 892, 472);

    private static readonly Color BodyColor = new("f87171");
    private static readonly Color EdgeColor = new("fecaca");
    private float _spin;

    public override void _PhysicsProcess(double delta)
    {
        var position = Position + Direction * Speed * (float)delta;

        // Bounce: flip the direction on the axis that left the bounds.
        var direction = Direction;
        if (position.X < Bounds.Position.X || position.X > Bounds.End.X)
            direction.X = -direction.X;
        if (position.Y < Bounds.Position.Y || position.Y > Bounds.End.Y)
            direction.Y = -direction.Y;
        Direction = direction;
        Position = position.Clamp(Bounds.Position, Bounds.End);

        // Checked every frame (not only on body_entered), so a player who stays
        // inside the enemy after the protection ends still gets hit.
        foreach (var body in GetOverlappingBodies())
        {
            if (body is Player player)
                player.TakeHit();
        }

        _spin += (float)delta * 3f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var points = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            float angle = _spin + i * Mathf.Pi / 2f;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 16f;
        }
        DrawColoredPolygon(points, BodyColor);
        DrawPolyline([.. points, points[0]], EdgeColor, 2f);
    }
}
