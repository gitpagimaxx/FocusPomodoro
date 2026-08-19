namespace FocusPomodoro.Models;

public sealed class PomodoroSession
{
    public PomodoroPhase CurrentPhase { get; set; } = PomodoroPhase.Focus;
    public int CurrentCycle { get; set; } = 1;
    public bool IsRunning { get; set; }
    public bool IsPaused { get; set; }
    public TimeSpan RemainingTime { get; set; }
    public TimeSpan TotalPhaseDuration { get; set; }
    public DateTimeOffset? EndTime { get; set; }
}
