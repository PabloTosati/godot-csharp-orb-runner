using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace OrbRunner.Tests;

/// <summary>
/// Runs inside Godot (headless) and exits with code 0 when every test passes:
///   godot --headless --path . res://tests/TestRunner.tscn
/// Part 1 tests the game rules; part 2 plays the real Main scene.
/// </summary>
public partial class TestRunner : Node
{
    private int _passed;
    private int _failed;

    public override async void _Ready()
    {
        SaveData.FilePath = "user://test_save.cfg";
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SaveData.FilePath));

        try
        {
            RunGameStateTests();
            await RunSceneTests();
        }
        catch (Exception e)
        {
            Fail("unexpected exception", e.ToString());
        }

        GD.Print($"\n{_passed} passed, {_failed} failed");
        GetTree().Quit(_failed == 0 ? 0 : 1);
    }

    // ---------- Helpers ----------

    private void Check(string name, bool ok, string detail = "")
    {
        if (ok)
        {
            _passed++;
            GD.Print($"PASS  {name}");
        }
        else
        {
            Fail(name, detail);
        }
    }

    private void Fail(string name, string detail)
    {
        _failed++;
        GD.PrintErr($"FAIL  {name}  {detail}");
    }

    private async Task PhysicsFrames(int count)
    {
        for (int i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    // ---------- Part 1: rules ----------

    private void RunGameStateTests()
    {
        var state = new GameState(bestScore: 5);
        Check("new round: score 0, 3 lives, 30 s", state.Score == 0 && state.Lives == 3 && state.TimeLeft == 30 && !state.IsOver);

        int changed = 0;
        state.Changed += () => changed++;
        state.AddScore(2);
        Check("AddScore adds points and notifies", state.Score == 2 && changed == 1);

        state.Tick(10);
        Check("Tick counts down", Math.Abs(state.TimeLeft - 20) < 1e-9);

        int ended = 0;
        state.Ended += () => ended++;
        state.Tick(100);
        Check("time up ends the round once, clock stops at 0", state.IsOver && state.TimeLeft == 0 && ended == 1);
        state.Tick(1);
        Check("ended is not raised twice", ended == 1);

        Check("no points after the round ends", !state.AddScore(5) && state.Score == 2);
        Check("best score kept when not beaten", state.BestScore == 5);

        state.Reset();
        state.AddScore(7);
        state.LoseLife();
        state.LoseLife();
        state.LoseLife();
        Check("losing all lives ends the round", state.IsOver && state.Lives == 0);
        Check("best score updated when beaten", state.BestScore == 7);
        Check("no lives lost after the end", !state.LoseLife() && state.Lives == 0);
    }

    // ---------- Part 2: the real game ----------

    private async Task RunSceneTests()
    {
        var main = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<Main>();
        AddChild(main);
        await PhysicsFrames(3);

        var player = main.Player;
        Check("round starts with 3 orbs and 2 enemies",
            main.Orbs.GetChildCount() == 3 && main.Enemies.GetChildCount() == 2,
            $"orbs={main.Orbs.GetChildCount()} enemies={main.Enemies.GetChildCount()}");

        // Keep enemies away while testing the other mechanics.
        foreach (var enemy in main.Enemies.GetChildren().OfType<Enemy>())
            enemy.SetPhysicsProcess(false);

        // Orb collected exactly once, and replaced.
        var orb = main.Orbs.GetChildren().OfType<Orb>().First();
        orb.GlobalPosition = player.GlobalPosition;
        await PhysicsFrames(5);
        Check("touching an orb scores exactly 1 point", main.State.Score == 1, $"score={main.State.Score}");
        Check("collected orb is replaced", main.Orbs.GetChildCount() == 3 && !IsInstanceValid(orb),
            $"orbs={main.Orbs.GetChildCount()}");

        // Walls stop the player.
        foreach (var o in main.Orbs.GetChildren().OfType<Orb>())
            o.GlobalPosition = new Vector2(900, 480); // out of the way
        player.GlobalPosition = new Vector2(80, 300);
        Input.ActionPress("move_left");
        await PhysicsFrames(60);
        Input.ActionRelease("move_left");
        float x = player.GlobalPosition.X;
        Check("left wall stops the player", x >= 33.5f && x <= 35f, $"x={x}");

        player.GlobalPosition = new Vector2(220, 210);
        Input.ActionPress("move_right");
        await PhysicsFrames(60);
        Input.ActionRelease("move_right");
        x = player.GlobalPosition.X;
        Check("pillar stops the player", x >= 243f && x <= 259f, $"x={x}");

        // Movement speed.
        player.GlobalPosition = new Vector2(480, 300);
        Input.ActionPress("move_down");
        await PhysicsFrames(30);
        Input.ActionRelease("move_down");
        float moved = player.GlobalPosition.Y - 300;
        Check("moves at about Speed px per second", Math.Abs(moved - player.Speed * 0.5f) < 12f, $"moved={moved}");

        // Enemy hit: one life per hit, even while overlapping for many frames.
        await PhysicsFrames(70); // let the spawn protection end
        var enemyNode = main.Enemies.GetChildren().OfType<Enemy>().First();
        enemyNode.GlobalPosition = player.GlobalPosition;
        enemyNode.SetPhysicsProcess(true);
        enemyNode.Speed = 0;
        await PhysicsFrames(20);
        Check("enemy contact costs exactly 1 life", main.State.Lives == 2, $"lives={main.State.Lives}");
        Check("player is protected right after a hit", player.IsInvulnerable);
        enemyNode.GlobalPosition = new Vector2(100, 480);

        // End of round.
        int score = main.State.Score;
        main.State.Tick(1000);
        await PhysicsFrames(2);
        Check("time up shows the game over panel", main.Hud.IsGameOverVisible);
        Check("player stops after game over", !player.IsPhysicsProcessing());
        Check("best score is saved", SaveData.LoadBest() == score, $"saved={SaveData.LoadBest()} score={score}");

        // Restart.
        main.StartRound();
        await PhysicsFrames(3);
        Check("restart resets score, lives and time",
            main.State.Score == 0 && main.State.Lives == 3 && main.State.TimeLeft > 29 && !main.State.IsOver);
        Check("restart hides the panel and respawns orbs and enemies",
            !main.Hud.IsGameOverVisible && main.Orbs.GetChildCount() == 3 && main.Enemies.GetChildCount() == 2,
            $"orbs={main.Orbs.GetChildCount()} enemies={main.Enemies.GetChildCount()}");
        Check("player can move again", player.IsPhysicsProcessing());
    }
}
