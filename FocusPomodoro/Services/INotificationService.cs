using FocusPomodoro.Models;

namespace FocusPomodoro.Services;

public interface INotificationService
{
    void Attach();
    void NotifyPhaseCompleted(PhaseTransition transition);
}
