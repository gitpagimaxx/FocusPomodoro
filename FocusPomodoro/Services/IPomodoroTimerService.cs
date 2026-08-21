using FocusPomodoro.Models;

namespace FocusPomodoro.Services;

public interface IPomodoroTimerService
{
    event EventHandler? StateChanged;
    event EventHandler<PomodoroPhase>? PhaseChanged;
    event EventHandler<PhaseTransition>? PhaseTransitioned;

    PomodoroSession GetState();
    void Start();
    void Pause();
    void Resume();
    void RestartCurrentPhase();
    void ResetCycle();
    void SkipToNextPhase();
    void ApplySettings();
}
