using FocusPomodoro.Models;
using FocusPomodoro.Services;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class SessionHistoryServiceTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Start_OpensInProgressLogAndSavesSnapshot()
    {
        var (history, timer, store, _) = CreateSut();
        history.Attach();

        timer.Start();

        var log = Assert.Single(store.Logs);
        Assert.Equal(PhaseOutcome.InProgress, log.Outcome);
        Assert.Equal(PomodoroPhase.Focus, log.Phase);
        Assert.NotNull(store.Snapshot);
        Assert.True(store.Snapshot.IsRunning);
    }

    [Fact]
    public async Task Tick_DoesNotWriteExtraSnapshotUntilFifteenSeconds()
    {
        var (history, timer, store, time, ticker) = CreateSutWithTicker();
        history.Attach();
        timer.Start();
        var savesAfterStart = store.SaveSnapshotCount;

        time.Advance(TimeSpan.FromSeconds(1));
        ticker.RaiseTick();

        Assert.Equal(savesAfterStart, store.SaveSnapshotCount);

        time.Advance(TimeSpan.FromSeconds(15));
        ticker.RaiseTick();

        Assert.True(store.SaveSnapshotCount > savesAfterStart);
    }

    [Fact]
    public async Task CompletingPhase_ClosesCompletedAndOpensNextWhenAutoStart()
    {
        var (history, timer, store, time, ticker) = CreateSutWithTicker();
        history.Attach();
        timer.Start();

        time.Advance(TimeSpan.FromMinutes(25));
        ticker.RaiseTick();

        Assert.Equal(2, store.Logs.Count);
        Assert.Equal(PhaseOutcome.Completed, store.Logs[0].Outcome);
        Assert.Equal(TimeSpan.FromMinutes(25), store.Logs[0].Elapsed);
        Assert.Equal(PhaseOutcome.InProgress, store.Logs[1].Outcome);
        Assert.Equal(PomodoroPhase.ShortBreak, store.Logs[1].Phase);
    }

    [Fact]
    public async Task Skip_ClosesAsSkippedWithElapsed()
    {
        var (history, timer, store, time, ticker) = CreateSutWithTicker();
        history.Attach();
        timer.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();

        timer.SkipToNextPhase();

        Assert.Equal(PhaseOutcome.Skipped, store.Logs[0].Outcome);
        Assert.Equal(TimeSpan.FromSeconds(10), store.Logs[0].Elapsed);
        Assert.Equal(PhaseOutcome.InProgress, store.Logs[1].Outcome);
    }

    [Fact]
    public async Task RestartWhileRunning_InterruptsAndOpensNewAttempt()
    {
        var (history, timer, store, _) = CreateSut();
        history.Attach();
        timer.Start();

        timer.RestartCurrentPhase();

        Assert.Equal(2, store.Logs.Count);
        Assert.Equal(PhaseOutcome.Interrupted, store.Logs[0].Outcome);
        Assert.Equal(PhaseOutcome.InProgress, store.Logs[1].Outcome);
        Assert.Equal(PomodoroPhase.Focus, store.Logs[1].Phase);
    }

    [Fact]
    public async Task ResetWhileRunning_InterruptsAndDoesNotOpenNewLog()
    {
        var (history, timer, store, _) = CreateSut();
        history.Attach();
        timer.Start();

        timer.ResetCycle();

        Assert.Single(store.Logs);
        Assert.Equal(PhaseOutcome.Interrupted, store.Logs[0].Outcome);
        Assert.NotNull(store.Snapshot);
        Assert.False(store.Snapshot.IsRunning);
        Assert.False(store.Snapshot.IsPaused);
    }

    [Fact]
    public async Task PersistAsync_WhileRunning_KeepsInProgressAndFreezesRemaining()
    {
        var (history, timer, store, time, ticker) = CreateSutWithTicker();
        history.Attach();
        timer.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();

        await history.PersistAsync();

        Assert.Equal(PhaseOutcome.InProgress, store.Logs[0].Outcome);
        Assert.NotNull(store.Snapshot);
        Assert.True(store.Snapshot.IsRunning);
        Assert.Equal(TimeSpan.FromMinutes(25) - TimeSpan.FromSeconds(10), store.Snapshot.Remaining);
    }

    [Fact]
    public async Task TryGetResumable_WhenRunningSnapshotAndInProgress_IsTrue()
    {
        var (history, timer, _, _) = CreateSut();
        history.Attach();
        timer.Start();
        await history.PersistAsync();

        Assert.True(history.TryGetResumable(out var snapshot));
        Assert.True(snapshot.IsRunning);
        Assert.True(snapshot.Remaining > TimeSpan.Zero);
    }

    [Fact]
    public async Task StartFreshAsync_InterruptsOpenPhase()
    {
        var (history, timer, store, time, ticker) = CreateSutWithTicker();
        history.Attach();
        timer.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();
        await history.PersistAsync();

        await history.StartFreshAsync();

        Assert.Equal(PhaseOutcome.Interrupted, store.Logs[0].Outcome);
        Assert.Equal(TimeSpan.FromSeconds(10), store.Logs[0].Elapsed);
        Assert.False(history.TryGetResumable(out _));
    }

    [Fact]
    public void TryGetResumable_WhenIdle_IsFalse()
    {
        var (history, _, _, _) = CreateSut();
        history.Attach();

        Assert.False(history.TryGetResumable(out _));
    }

    private static (SessionHistoryService History, PomodoroTimerService Timer, FakeHistoryStore Store, FakeTimeProvider Time)
        CreateSut()
    {
        var time = new FakeTimeProvider(StartTime);
        var ticker = new FakeUiTicker();
        var timer = new PomodoroTimerService(new PomodoroSettings(), time, ticker);
        var store = new FakeHistoryStore();
        var history = new SessionHistoryService(timer, store, time);
        return (history, timer, store, time);
    }

    private static (SessionHistoryService History, PomodoroTimerService Timer, FakeHistoryStore Store, FakeTimeProvider Time, FakeUiTicker Ticker)
        CreateSutWithTicker()
    {
        var time = new FakeTimeProvider(StartTime);
        var ticker = new FakeUiTicker();
        var timer = new PomodoroTimerService(new PomodoroSettings(), time, ticker);
        var store = new FakeHistoryStore();
        var history = new SessionHistoryService(timer, store, time);
        return (history, timer, store, time, ticker);
    }
}
