using FocusPomodoro.Models;

namespace FocusPomodoro.Services;

public interface ISessionHistoryService
{
    void Attach();
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task PersistAsync(CancellationToken cancellationToken = default);
    bool TryGetResumable(out SessionSnapshot snapshot);
    Task StartFreshAsync(CancellationToken cancellationToken = default);
}
