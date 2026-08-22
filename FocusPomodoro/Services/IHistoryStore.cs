using FocusPomodoro.Models;

namespace FocusPomodoro.Services;

public interface IHistoryStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<PhaseLog> OpenPhaseAsync(
        PomodoroPhase phase,
        int cycle,
        TimeSpan planned,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken = default);
    Task ClosePhaseAsync(
        long id,
        DateTimeOffset endedAt,
        TimeSpan elapsed,
        PhaseOutcome outcome,
        CancellationToken cancellationToken = default);
    Task<PhaseLog?> GetInProgressAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhaseLog>> GetLogsAsync(CancellationToken cancellationToken = default);
    Task SaveSnapshotAsync(SessionSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<SessionSnapshot?> LoadSnapshotAsync(CancellationToken cancellationToken = default);
}
