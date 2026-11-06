using Godot;

namespace OrbRunner;

/// <summary>Score, time and lives at the top, and the game over panel.</summary>
public partial class Hud : CanvasLayer
{
    [Signal]
    public delegate void RestartRequestedEventHandler();

    private Label _scoreLabel;
    private Label _timeLabel;
    private Label _livesLabel;
    private Control _gameOverPanel;
    private Label _finalScoreLabel;
    private Label _bestLabel;
    private Label _hintLabel;

    public bool IsGameOverVisible => _gameOverPanel.Visible;

    public override void _Ready()
    {
        // GetNode in _Ready (not in the constructor or field initializers),
        // when the child nodes already exist.
        _scoreLabel = GetNode<Label>("TopBar/Margin/Row/ScoreLabel");
        _timeLabel = GetNode<Label>("TopBar/Margin/Row/TimeLabel");
        _livesLabel = GetNode<Label>("TopBar/Margin/Row/LivesLabel");
        _gameOverPanel = GetNode<Control>("GameOverPanel");
        _finalScoreLabel = GetNode<Label>("GameOverPanel/Margin/Column/FinalScoreLabel");
        _bestLabel = GetNode<Label>("GameOverPanel/Margin/Column/BestLabel");
        _hintLabel = GetNode<Label>("HintLabel");

        GetNode<Button>("GameOverPanel/Margin/Column/RestartButton").Pressed +=
            () => EmitSignal(SignalName.RestartRequested);
    }

    public void UpdateStats(int score, double timeLeft, int lives)
    {
        _scoreLabel.Text = $"Score  {score}";
        _timeLabel.Text = $"Time  {Mathf.CeilToInt(timeLeft)}";
        _livesLabel.Text = "Lives  " + new string('♥', Mathf.Max(lives, 0));
    }

    public void ShowGameOver(int score, int best, bool newRecord)
    {
        _finalScoreLabel.Text = $"Score: {score}";
        _bestLabel.Text = newRecord ? $"New record: {best}!" : $"Best: {best}";
        _gameOverPanel.Visible = true;
        GetNode<Button>("GameOverPanel/Margin/Column/RestartButton").GrabFocus();
    }

    public void HideGameOver() => _gameOverPanel.Visible = false;

    /// <summary>Shows the controls for a few seconds at the start of a round.</summary>
    public void ShowHint()
    {
        _hintLabel.Modulate = Colors.White;
        var tween = CreateTween();
        tween.TweenInterval(2.5);
        tween.TweenProperty(_hintLabel, "modulate:a", 0.0, 0.8);
    }
}
