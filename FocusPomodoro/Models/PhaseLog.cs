namespace FocusPomodoro.Models;

public sealed class PhaseLog
{
    public long Id { get; init; }
    public PomodoroPhase Phase { get; init; }
    public int Cycle { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public TimeSpan PlannedDuration { get; init; }
    public TimeSpan Elapsed { get; init; }
    public PhaseOutcome Outcome { get; init; }
}
