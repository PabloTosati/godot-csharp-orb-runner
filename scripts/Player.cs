using Godot;

namespace OrbRunner;

/// <summary>The player: moves with physics, so walls really block it.</summary>
public partial class Player : CharacterBody2D
{
    [Signal]
    public delegate void HitEventHandler();

    [Export] public float Speed { get; set; } = 280f;

    /// <summary>Seconds of protection after spawning or being hit.</summary>
    [Export] public double InvulnerableTime { get; set; } = 1.0;

    public bool IsInvulnerable => _invulnerableLeft > 0;

    private static readonly Color BodyColor = new("4d86f7");
    private static readonly Color EdgeColor = new("a9c4ff");
    private const float HalfSize = 14f;

    private double _invulnerableLeft;

    public override void _PhysicsProcess(double delta)
    {
        // Velocity + MoveAndSlide (not Position +=) so collisions are respected,
        // and _PhysicsProcess so the speed does not depend on the frame rate.
        var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = direction * Speed;
        MoveAndSlide();

        if (_invulnerableLeft > 0)
        {
            _invulnerableLeft -= delta;
            // Blink while protected.
            Modulate = new Color(1, 1, 1, Mathf.PosMod((float)_invulnerableLeft, 0.2f) < 0.1f ? 0.35f : 1f);
        }
        else
        {
            Modulate = Colors.White;
        }
    }

    /// <summary>Called by enemies. Ignored while the player is protected.</summary>
    public void TakeHit()
    {
        if (IsInvulnerable || !IsPhysicsProcessing())
            return;

        _invulnerableLeft = InvulnerableTime;
        EmitSignal(SignalName.Hit);
    }

    public void Respawn(Vector2 position)
    {
        GlobalPosition = position;
        Velocity = Vector2.Zero;
        _invulnerableLeft = InvulnerableTime;
        SetPhysicsProcess(true);
    }

    public void Freeze()
    {
        SetPhysicsProcess(false);
        Modulate = Colors.White;
    }

    public override void _Draw()
    {
        var rect = new Rect2(-HalfSize, -HalfSize, HalfSize * 2, HalfSize * 2);
        DrawRect(rect, BodyColor);
        DrawRect(rect, EdgeColor, false, 2f);
        // Eyes, so the square reads as a character.
        DrawCircle(new Vector2(-5, -3), 2.5f, Colors.White);
        DrawCircle(new Vector2(5, -3), 2.5f, Colors.White);
    }
}
