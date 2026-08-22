namespace FocusPomodoro.Models;

public sealed class SessionSnapshot
{
    public PomodoroPhase Phase { get; init; }
    public int Cycle { get; init; }
    public TimeSpan Remaining { get; init; }
    public TimeSpan TotalPhaseDuration { get; init; }
    public bool IsRunning { get; init; }
    public bool IsPaused { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public PomodoroSession ToSession() => new()
    {
        CurrentPhase = Phase,
        CurrentCycle = Cycle,
        RemainingTime = Remaining,
        TotalPhaseDuration = TotalPhaseDuration,
        IsRunning = IsRunning,
        IsPaused = IsPaused
    };
}
