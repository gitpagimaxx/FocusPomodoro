using FocusPomodoro.Models;
using FocusPomodoro.Services;

namespace FocusPomodoro.Tests;

internal sealed class FakeHistoryStore : IHistoryStore
{
    private long _nextId = 1;

    public List<PhaseLog> Logs { get; } = [];
    public SessionSnapshot? Snapshot { get; private set; }
    public int SaveSnapshotCount { get; private set; }
    public int InitializeCount { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        InitializeCount++;
        return Task.CompletedTask;
    }

    public Task<PhaseLog> OpenPhaseAsync(
        PomodoroPhase phase,
        int cycle,
        TimeSpan planned,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken = default)
    {
        var log = new PhaseLog
        {
            Id = _nextId++,
            Phase = phase,
            Cycle = cycle,
            StartedAt = startedAt,
            PlannedDuration = planned,
            Elapsed = TimeSpan.Zero,
            Outcome = PhaseOutcome.InProgress
        };
        Logs.Add(log);
        return Task.FromResult(log);
    }

    public Task ClosePhaseAsync(
        long id,
        DateTimeOffset endedAt,
        TimeSpan elapsed,
        PhaseOutcome outcome,
        CancellationToken cancellationToken = default)
    {
        var index = Logs.FindIndex(log => log.Id == id);
        if (index >= 0)
        {
            var existing = Logs[index];
            Logs[index] = new PhaseLog
            {
                Id = existing.Id,
                Phase = existing.Phase,
                Cycle = existing.Cycle,
                StartedAt = existing.StartedAt,
                EndedAt = endedAt,
                PlannedDuration = existing.PlannedDuration,
                Elapsed = elapsed,
                Outcome = outcome
            };
        }

        return Task.CompletedTask;
    }

    public Task<PhaseLog?> GetInProgressAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Logs.LastOrDefault(log => log.Outcome == PhaseOutcome.InProgress));

    public Task<IReadOnlyList<PhaseLog>> GetLogsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PhaseLog>>(Logs.ToArray());

    public Task SaveSnapshotAsync(SessionSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        Snapshot = snapshot;
        SaveSnapshotCount++;
        return Task.CompletedTask;
    }

    public Task<SessionSnapshot?> LoadSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot);
}
