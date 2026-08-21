namespace FocusPomodoro.Services;

public interface ITrayIconService : IDisposable
{
    event EventHandler? ShowRequested;
    event EventHandler? TimerActionRequested;
    event EventHandler? RestartRequested;
    event EventHandler? ResetCycleRequested;
    event EventHandler? ExitRequested;

    void Initialize();

    void Update(string tooltip, string timerActionText, bool canRestart);
}
