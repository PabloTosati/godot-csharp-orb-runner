using System;

namespace OrbRunner;

/// <summary>
/// Rules of a round: score, lives, time and best score.
/// Plain C# with no Godot types, so it is easy to test and reuse.
/// </summary>
public class GameState
{
    public const int StartLives = 3;
    public const double RoundSeconds = 30.0;

    public int Score { get; private set; }
    public int Lives { get; private set; }
    public double TimeLeft { get; private set; }
    public int BestScore { get; private set; }
    public bool IsOver { get; private set; }

    /// <summary>Raised whenever score, lives or time change.</summary>
    public event Action Changed;

    /// <summary>Raised once when the round ends (time is up or no lives left).</summary>
    public event Action Ended;

    public GameState(int bestScore = 0)
    {
        BestScore = Math.Max(0, bestScore);
        Reset();
    }

    public void Reset()
    {
        Score = 0;
        Lives = StartLives;
        TimeLeft = RoundSeconds;
        IsOver = false;
        Changed?.Invoke();
    }

    /// <summary>Adds points. Ignored after the round is over.</summary>
    public bool AddScore(int points)
    {
        if (IsOver || points <= 0)
            return false;

        Score += points;
        Changed?.Invoke();
        return true;
    }

    /// <summary>Removes one life. The round ends at zero lives.</summary>
    public bool LoseLife()
    {
        if (IsOver)
            return false;

        Lives--;
        Changed?.Invoke();
        if (Lives <= 0)
            Finish();
        return true;
    }

    /// <summary>Advances the round clock. Call it every frame with the frame delta.</summary>
    public void Tick(double delta)
    {
        if (IsOver || delta <= 0)
            return;

        TimeLeft = Math.Max(0, TimeLeft - delta);
        Changed?.Invoke();
        if (TimeLeft <= 0)
            Finish();
    }

    private void Finish()
    {
        if (IsOver)
            return;

        IsOver = true;
        if (Score > BestScore)
            BestScore = Score;
        Ended?.Invoke();
    }
}
