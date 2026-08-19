namespace FocusPomodoro.Models;

public sealed class PhaseTransition
{
    public PhaseTransition(PomodoroPhase completedPhase, PomodoroPhase nextPhase, int currentCycle)
    {
        CompletedPhase = completedPhase;
        NextPhase = nextPhase;
        CurrentCycle = currentCycle;
    }

    public PomodoroPhase CompletedPhase { get; }
    public PomodoroPhase NextPhase { get; }
    public int CurrentCycle { get; }
}
