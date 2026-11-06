using System.Collections.Generic;
using System.Linq;
using Godot;

namespace OrbRunner;

/// <summary>Runs a round: spawns orbs and enemies, connects the signals and ends the game.</summary>
public partial class Main : Node2D
{
    [Export] public PackedScene OrbScene { get; set; }
    [Export] public PackedScene EnemyScene { get; set; }
    [Export] public int OrbsOnScreen { get; set; } = 3;
    [Export] public int EnemyCount { get; set; } = 2;

    public GameState State { get; private set; }
    public Player Player { get; private set; }
    public Hud Hud { get; private set; }
    public Node2D Orbs { get; private set; }
    public Node2D Enemies { get; private set; }

    /// <summary>Inside of the arena walls.</summary>
    public static readonly Rect2 Arena = new(20, 20, 920, 500);
    public static readonly Vector2 PlayerStart = new(480, 300);

    private readonly List<Rect2> _blocked = new();
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        InputSetup.EnsureActions();

        Player = GetNode<Player>("Player");
        Hud = GetNode<Hud>("Hud");
        Orbs = GetNode<Node2D>("Orbs");
        Enemies = GetNode<Node2D>("Enemies");

        foreach (var wall in GetNode("Walls").GetChildren().OfType<Wall>())
            _blocked.Add(wall.GlobalRect.Grow(18));

        State = new GameState(SaveData.LoadBest());
        State.Changed += () => Hud.UpdateStats(State.Score, State.TimeLeft, State.Lives);
        State.Ended += OnRoundEnded;

        Player.Hit += () => State.LoseLife();
        Hud.RestartRequested += StartRound;

        StartRound();
    }

    public override void _Process(double delta) => State.Tick(delta);

    public override void _UnhandledInput(InputEvent @event)
    {
        if (State.IsOver && @event.IsActionPressed("restart"))
            StartRound();
    }

    public void StartRound()
    {
        foreach (var node in Orbs.GetChildren().Concat(Enemies.GetChildren()))
            node.QueueFree();

        State.Reset();
        Hud.HideGameOver();
        Hud.ShowHint();
        Player.Respawn(PlayerStart);

        for (int i = 0; i < OrbsOnScreen; i++)
            SpawnOrb();

        var starts = new[] { (new Vector2(160, 110), new Vector2(1f, 0.6f)), (new Vector2(800, 440), new Vector2(-1f, -0.45f)) };
        for (int i = 0; i < EnemyCount; i++)
        {
            var (position, direction) = starts[i % starts.Length];
            var enemy = EnemyScene.Instantiate<Enemy>();
            enemy.Position = position;
            enemy.Direction = direction.Normalized();
            enemy.Speed += i * 25f;
            Enemies.AddChild(enemy);
        }
    }

    /// <summary>Places an orb in a free spot, away from walls and from the player.</summary>
    public void SpawnOrb()
    {
        var orb = OrbScene.Instantiate<Orb>();
        orb.Position = FindFreeSpot();
        orb.Collected += OnOrbCollected;
        Orbs.AddChild(orb);
    }

    private Vector2 FindFreeSpot()
    {
        var area = Arena.Grow(-30);
        for (int attempt = 0; attempt < 100; attempt++)
        {
            var point = new Vector2(
                _rng.RandfRange(area.Position.X, area.End.X),
                _rng.RandfRange(area.Position.Y, area.End.Y));

            bool blocked = _blocked.Any(rect => rect.HasPoint(point));
            bool nearPlayer = point.DistanceTo(Player.GlobalPosition) < 90;
            bool nearOrb = Orbs.GetChildren().OfType<Orb>()
                .Any(orb => !orb.IsQueuedForDeletion() && orb.Position.DistanceTo(point) < 80);
            if (!blocked && !nearPlayer && !nearOrb)
                return point;
        }
        return Arena.GetCenter();
    }

    private void OnOrbCollected(Orb orb)
    {
        State.AddScore(orb.Value);
        // The signal comes from a physics callback: adding a new Area2D right now
        // would fail ("Can't change this state while flushing queries"), so defer it.
        if (!State.IsOver)
            CallDeferred(MethodName.SpawnOrb);
    }

    private void OnRoundEnded()
    {
        int previousBest = SaveData.LoadBest();
        bool newRecord = State.BestScore > previousBest;
        if (newRecord)
            SaveData.SaveBest(State.BestScore);

        Player.Freeze();
        foreach (var enemy in Enemies.GetChildren().OfType<Enemy>())
            enemy.SetPhysicsProcess(false);

        Hud.ShowGameOver(State.Score, State.BestScore, newRecord);
    }
}
