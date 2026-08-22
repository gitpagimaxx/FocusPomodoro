namespace FocusPomodoro.Models;

public sealed class PhaseTransition
{
    public PhaseTransition(
        PomodoroPhase completedPhase,
        PomodoroPhase nextPhase,
        int currentCycle,
        PhaseEndReason reason = PhaseEndReason.Completed,
        TimeSpan elapsed = default)
    {
        CompletedPhase = completedPhase;
        NextPhase = nextPhase;
        CurrentCycle = currentCycle;
        Reason = reason;
        Elapsed = elapsed;
    }

    public PomodoroPhase CompletedPhase { get; }
    public PomodoroPhase NextPhase { get; }
    public int CurrentCycle { get; }
    public PhaseEndReason Reason { get; }
    public TimeSpan Elapsed { get; }
}
