using FocusPomodoro.Models;

namespace FocusPomodoro.Services;

public interface ISettingsService
{
    PomodoroSettings Current { get; }

    event EventHandler? SettingsChanged;

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
