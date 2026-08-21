using FocusPomodoro.Helpers;
using FocusPomodoro.Models;
using FocusPomodoro.Services;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class NotificationServiceTests
{
    [Fact]
    public void CompletingFocus_ShowsFocusCompletedToast()
    {
        var (timer, time, ticker, settings) = CreateTimer();
        var toasts = new List<(string Title, string Message)>();
        _ = new NotificationService(timer, settings, (title, message) => toasts.Add((title, message)));

        CompleteCurrentPhase(timer, time, ticker);

        Assert.Equal(
            new[] { (PhaseNotificationCatalog.Title, PhaseNotificationCatalog.FocusCompleted) },
            toasts);
    }

    [Fact]
    public void CompletingShortBreak_ShowsReturnToFocusToast()
    {
        var (timer, time, ticker, settings) = CreateTimer();
        CompleteCurrentPhase(timer, time, ticker);
        var toasts = new List<(string Title, string Message)>();
        _ = new NotificationService(timer, settings, (title, message) => toasts.Add((title, message)));

        CompleteCurrentPhase(timer, time, ticker);

        Assert.Equal(
            new[] { (PhaseNotificationCatalog.Title, PhaseNotificationCatalog.ShortBreakCompleted) },
            toasts);
    }

    [Fact]
    public void CompletingFourthFocus_ShowsLongBreakToast()
    {
        var (timer, time, ticker, settings) = CreateTimer();
        CompleteFocusCyclesUntil(timer, time, ticker, 3);
        CompleteCurrentPhase(timer, time, ticker);
        Assert.Equal(PomodoroPhase.Focus, timer.GetState().CurrentPhase);
        Assert.Equal(4, timer.GetState().CurrentCycle);
        var toasts = new List<(string Title, string Message)>();
        _ = new NotificationService(timer, settings, (title, message) => toasts.Add((title, message)));

        CompleteCurrentPhase(timer, time, ticker);

        Assert.Equal(PomodoroPhase.LongBreak, timer.GetState().CurrentPhase);
        Assert.Equal(
            new[] { (PhaseNotificationCatalog.Title, PhaseNotificationCatalog.FourthFocusCompleted) },
            toasts);
    }

    [Fact]
    public void CompletingLongBreak_ShowsNewCycleToast()
    {
        var (timer, time, ticker, settings) = CreateTimer();
        CompleteFocusCyclesUntil(timer, time, ticker, 4);
        var toasts = new List<(string Title, string Message)>();
        _ = new NotificationService(timer, settings, (title, message) => toasts.Add((title, message)));

        CompleteCurrentPhase(timer, time, ticker);

        Assert.Equal(
            new[] { (PhaseNotificationCatalog.Title, PhaseNotificationCatalog.LongBreakCompleted) },
            toasts);
    }

    [Fact]
    public void SkipToNextPhase_ShowsToastForCompletedPhase()
    {
        var (timer, _, _, settings) = CreateTimer();
        var toasts = new List<(string Title, string Message)>();
        _ = new NotificationService(timer, settings, (title, message) => toasts.Add((title, message)));

        timer.SkipToNextPhase();

        Assert.Equal(
            new[] { (PhaseNotificationCatalog.Title, PhaseNotificationCatalog.FocusCompleted) },
            toasts);
    }

    [Fact]
    public void ResetCycle_DoesNotShowToast()
    {
        var (timer, _, _, settings) = CreateTimer();
        timer.SkipToNextPhase();
        var toasts = new List<(string Title, string Message)>();
        _ = new NotificationService(timer, settings, (title, message) => toasts.Add((title, message)));

        timer.ResetCycle();

        Assert.Empty(toasts);
        Assert.Equal(PomodoroPhase.Focus, timer.GetState().CurrentPhase);
        Assert.Equal(1, timer.GetState().CurrentCycle);
    }

    [Fact]
    public void Pause_DoesNotShowToast()
    {
        var (timer, time, ticker, settings) = CreateTimer();
        var toasts = new List<(string Title, string Message)>();
        _ = new NotificationService(timer, settings, (title, message) => toasts.Add((title, message)));
        timer.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();

        timer.Pause();

        Assert.Empty(toasts);
        Assert.True(timer.GetState().IsPaused);
    }

    [Fact]
    public void NotificationsDisabled_DoesNotShowToast()
    {
        var (timer, time, ticker, settings) = CreateTimer();
        settings.NotificationsEnabled = false;
        var toasts = new List<(string Title, string Message)>();
        _ = new NotificationService(timer, settings, (title, message) => toasts.Add((title, message)));

        CompleteCurrentPhase(timer, time, ticker);
        timer.SkipToNextPhase();

        Assert.Empty(toasts);
    }

    [Fact]
    public void Attach_CalledTwice_DoesNotDuplicateToasts()
    {
        var (timer, time, ticker, settings) = CreateTimer();
        var toasts = new List<(string Title, string Message)>();
        var service = new NotificationService(timer, settings, (title, message) => toasts.Add((title, message)));

        service.Attach();
        CompleteCurrentPhase(timer, time, ticker);

        Assert.Single(toasts);
    }

    private static (PomodoroTimerService Timer, FakeTimeProvider Time, FakeUiTicker Ticker, PomodoroSettings Settings)
        CreateTimer()
    {
        var settings = new PomodoroSettings();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero));
        var ticker = new FakeUiTicker();
        var timer = new PomodoroTimerService(settings, time, ticker);
        return (timer, time, ticker, settings);
    }

    private static void CompleteCurrentPhase(
        PomodoroTimerService timer,
        FakeTimeProvider time,
        FakeUiTicker ticker)
    {
        var state = timer.GetState();
        if (!state.IsRunning)
        {
            timer.Start();
            state = timer.GetState();
        }

        time.Advance(state.RemainingTime);
        ticker.RaiseTick();
    }

    private static void CompleteFocusCyclesUntil(
        PomodoroTimerService timer,
        FakeTimeProvider time,
        FakeUiTicker ticker,
        int focusCycleToComplete)
    {
        for (var i = 0; i < 16; i++)
        {
            var before = timer.GetState();
            CompleteCurrentPhase(timer, time, ticker);
            var after = timer.GetState();
            if (before.CurrentPhase == PomodoroPhase.Focus
                && before.CurrentCycle == focusCycleToComplete
                && after.CurrentPhase != PomodoroPhase.Focus)
            {
                return;
            }
        }

        Assert.Fail($"Did not complete Focus cycle {focusCycleToComplete}.");
    }
}
