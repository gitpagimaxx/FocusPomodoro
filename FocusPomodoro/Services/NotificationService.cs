using FocusPomodoro.Helpers;
using FocusPomodoro.Models;

namespace FocusPomodoro.Services;

public sealed class NotificationService : INotificationService
{
    private readonly IPomodoroTimerService _timer;
    private readonly PomodoroSettings _settings;
    private readonly Action<string, string> _showToast;
    private bool _attached;

    public NotificationService(
        IPomodoroTimerService timer,
        PomodoroSettings settings,
        Action<string, string> showToast)
    {
        _timer = timer ?? throw new ArgumentNullException(nameof(timer));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _showToast = showToast ?? throw new ArgumentNullException(nameof(showToast));
        Attach();
    }

    public void Attach()
    {
        if (_attached)
        {
            return;
        }

        _attached = true;
        _timer.PhaseTransitioned += OnPhaseTransitioned;
    }

    public void NotifyPhaseCompleted(PhaseTransition transition)
    {
        if (!_settings.NotificationsEnabled)
        {
            return;
        }

        var message = PhaseNotificationCatalog.MessageFor(transition);
        if (message is null)
        {
            return;
        }

        _showToast(PhaseNotificationCatalog.Title, message);
    }

    private void OnPhaseTransitioned(object? sender, PhaseTransition transition) =>
        NotifyPhaseCompleted(transition);
}
