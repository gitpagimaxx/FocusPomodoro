using FocusPomodoro.Helpers;
using FocusPomodoro.Models;

namespace FocusPomodoro.Services;

public sealed class SessionHistoryService : ISessionHistoryService
{
    private static readonly TimeSpan SnapshotThrottle = TimeSpan.FromSeconds(15);

    private readonly IPomodoroTimerService _timer;
    private readonly IHistoryStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _attached;
    private PhaseLog? _inProgress;
    private SessionSnapshot? _snapshot;
    private DateTimeOffset _lastSnapshotAt = DateTimeOffset.MinValue;

    public SessionHistoryService(
        IPomodoroTimerService timer,
        IHistoryStore store,
        TimeProvider timeProvider)
    {
        _timer = timer ?? throw new ArgumentNullException(nameof(timer));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public void Attach()
    {
        if (_attached)
        {
            return;
        }

        _attached = true;
        _timer.PhaseTransitioned += OnPhaseTransitioned;
        _timer.Checkpoint += OnCheckpoint;
        _timer.StateChanged += OnStateChanged;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await RunAsync(async () =>
        {
            await _store.InitializeAsync(cancellationToken).ConfigureAwait(false);
            _inProgress = await _store.GetInProgressAsync(cancellationToken).ConfigureAwait(false);
            _snapshot = await _store.LoadSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public Task PersistAsync(CancellationToken cancellationToken = default) =>
        RunAsync(() => SaveSnapshotAsync(force: true, cancellationToken));

    public bool TryGetResumable(out SessionSnapshot snapshot)
    {
        snapshot = _snapshot!;
        return _snapshot is { } current
            && _inProgress is not null
            && current.Remaining > TimeSpan.Zero
            && (current.IsRunning || current.IsPaused);
    }

    public Task StartFreshAsync(CancellationToken cancellationToken = default) =>
        RunAsync(async () =>
        {
            await CloseInProgressAsync(PhaseOutcome.Interrupted, cancellationToken).ConfigureAwait(false);
            _snapshot = new SessionSnapshot
            {
                Phase = PomodoroPhase.Focus,
                Cycle = 1,
                Remaining = TimeSpan.Zero,
                TotalPhaseDuration = TimeSpan.Zero,
                IsRunning = false,
                IsPaused = false,
                UpdatedAt = _timeProvider.GetUtcNow()
            };
            await _store.SaveSnapshotAsync(_snapshot, cancellationToken).ConfigureAwait(false);
            _lastSnapshotAt = _timeProvider.GetUtcNow();
        });

    private void OnPhaseTransitioned(object? sender, PhaseTransition transition) =>
        _ = RunAsync(async () =>
        {
            var outcome = transition.Reason switch
            {
                PhaseEndReason.Completed => PhaseOutcome.Completed,
                PhaseEndReason.Skipped => PhaseOutcome.Skipped,
                _ => PhaseOutcome.Interrupted
            };
            await CloseInProgressAsync(outcome, transition.Elapsed, CancellationToken.None).ConfigureAwait(false);

            var state = _timer.GetState();
            if (outcome == PhaseOutcome.Interrupted && (state.IsRunning || state.IsPaused))
            {
                await OpenIfNeededAsync(state, CancellationToken.None).ConfigureAwait(false);
            }

            await SaveSnapshotAsync(force: true, CancellationToken.None).ConfigureAwait(false);
        });

    private void OnCheckpoint(object? sender, EventArgs e) =>
        _ = RunAsync(async () =>
        {
            var state = _timer.GetState();
            await OpenIfNeededAsync(state, CancellationToken.None).ConfigureAwait(false);
            await SaveSnapshotAsync(force: true, CancellationToken.None).ConfigureAwait(false);
        });

    private void OnStateChanged(object? sender, EventArgs e)
    {
        var state = _timer.GetState();
        if (!state.IsRunning)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();
        if (now - _lastSnapshotAt < SnapshotThrottle)
        {
            return;
        }

        _ = RunAsync(() => SaveSnapshotAsync(force: false, CancellationToken.None));
    }

    private async Task OpenIfNeededAsync(PomodoroSession state, CancellationToken cancellationToken)
    {
        if (_inProgress is not null || (!state.IsRunning && !state.IsPaused))
        {
            return;
        }

        _inProgress = await _store.OpenPhaseAsync(
            state.CurrentPhase,
            state.CurrentCycle,
            state.TotalPhaseDuration,
            _timeProvider.GetUtcNow(),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task CloseInProgressAsync(
        PhaseOutcome outcome,
        CancellationToken cancellationToken) =>
        await CloseInProgressAsync(outcome, elapsed: null, cancellationToken).ConfigureAwait(false);

    private async Task CloseInProgressAsync(
        PhaseOutcome outcome,
        TimeSpan? elapsed,
        CancellationToken cancellationToken)
    {
        if (_inProgress is null)
        {
            return;
        }

        var closedElapsed = elapsed
            ?? PhaseElapsed.FromRemaining(_inProgress.PlannedDuration, _snapshot?.Remaining ?? TimeSpan.Zero);
        if (outcome == PhaseOutcome.Completed)
        {
            closedElapsed = _inProgress.PlannedDuration;
        }

        await _store.ClosePhaseAsync(
            _inProgress.Id,
            _timeProvider.GetUtcNow(),
            closedElapsed,
            outcome,
            cancellationToken).ConfigureAwait(false);
        _inProgress = null;
    }

    private async Task SaveSnapshotAsync(bool force, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        if (!force && now - _lastSnapshotAt < SnapshotThrottle)
        {
            return;
        }

        var state = _timer.GetState();
        _snapshot = SnapshotFrom(state);
        await _store.SaveSnapshotAsync(_snapshot, cancellationToken).ConfigureAwait(false);
        _lastSnapshotAt = now;
    }

    private SessionSnapshot SnapshotFrom(PomodoroSession state)
    {
        var remaining = state.RemainingTime;
        if (state.IsRunning && state.EndTime is { } end)
        {
            remaining = end - _timeProvider.GetUtcNow();
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }
        }

        return new SessionSnapshot
        {
            Phase = state.CurrentPhase,
            Cycle = state.CurrentCycle,
            Remaining = remaining,
            TotalPhaseDuration = state.TotalPhaseDuration,
            IsRunning = state.IsRunning,
            IsPaused = state.IsPaused,
            UpdatedAt = _timeProvider.GetUtcNow()
        };
    }

    private async Task RunAsync(Func<Task> work)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await work().ConfigureAwait(false);
        }
        catch (Exception)
        {
        }
        finally
        {
            _gate.Release();
        }
    }
}
