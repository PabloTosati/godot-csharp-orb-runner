using Godot;

namespace OrbRunner;

/// <summary>
/// A solid rectangle. Change Size in the Inspector and both the collision
/// shape and the drawing follow ([Tool] makes it visible in the editor too).
/// </summary>
[Tool]
public partial class Wall : StaticBody2D
{
    private Vector2 _size = new(100, 20);

    [Export]
    public Vector2 Size
    {
        get => _size;
        set
        {
            _size = value;
            UpdateShape();
            QueueRedraw();
        }
    }

    private static readonly Color FillColor = new("24324f");
    private static readonly Color EdgeColor = new("33456b");

    /// <summary>The wall area in world coordinates.</summary>
    public Rect2 GlobalRect => new(GlobalPosition - Size / 2, Size);

    public override void _Ready() => UpdateShape();

    private void UpdateShape()
    {
        var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collision == null)
            return;

        // Each wall gets its own shape: a shared resource would resize every wall at once.
        collision.Shape = new RectangleShape2D { Size = _size };
    }

    public override void _Draw()
    {
        var rect = new Rect2(-Size / 2, Size);
        DrawRect(rect, FillColor);
        DrawRect(rect, EdgeColor, false, 2f);
    }
}
