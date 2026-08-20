using FocusPomodoro.Models;
using FocusPomodoro.Services;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class PomodoroTimerServiceTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

    private static (PomodoroTimerService Service, FakeTimeProvider Time, FakeUiTicker Ticker)
        CreateSut(PomodoroSettings? settings = null)
    {
        var time = new FakeTimeProvider(StartTime);
        var ticker = new FakeUiTicker();
        var service = new PomodoroTimerService(settings ?? new PomodoroSettings(), time, ticker);
        return (service, time, ticker);
    }

    private static void CompleteCurrentPhase(
        PomodoroTimerService service,
        FakeTimeProvider time,
        FakeUiTicker ticker)
    {
        var state = service.GetState();
        if (!state.IsRunning)
        {
            service.Start();
            state = service.GetState();
        }

        time.Advance(state.RemainingTime);
        ticker.RaiseTick();
    }

    [Fact]
    public void InitialState_IsFocusCycleOneWithFullDurationAndNotRunning()
    {
        var (service, _, ticker) = CreateSut();

        var state = service.GetState();

        Assert.Equal(PomodoroPhase.Focus, state.CurrentPhase);
        Assert.Equal(1, state.CurrentCycle);
        Assert.Equal(TimeSpan.FromMinutes(25), state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(25), state.TotalPhaseDuration);
        Assert.False(state.IsRunning);
        Assert.False(state.IsPaused);
        Assert.Null(state.EndTime);
        Assert.False(ticker.IsStarted);
    }

    [Fact]
    public void Start_SetsEndTimeToNowPlusFocusDurationAndIsRunning()
    {
        var (service, time, ticker) = CreateSut();

        service.Start();

        var state = service.GetState();
        Assert.True(state.IsRunning);
        Assert.False(state.IsPaused);
        Assert.Equal(time.GetUtcNow() + TimeSpan.FromMinutes(25), state.EndTime);
        Assert.True(ticker.IsStarted);
        Assert.Equal(1, ticker.StartCount);
    }

    [Fact]
    public void Tick_AfterAdvancingTenSeconds_DerivesRemainingFromEndTimeNotDecrement()
    {
        var (service, time, ticker) = CreateSut();
        service.Start();

        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();

        var state = service.GetState();
        Assert.Equal(TimeSpan.FromMinutes(24) + TimeSpan.FromSeconds(50), state.RemainingTime);
        Assert.NotEqual(TimeSpan.FromMinutes(24) + TimeSpan.FromSeconds(59), state.RemainingTime);
    }

    [Fact]
    public void Pause_FreezesRemaining_FurtherAdvanceAndTickDoesNotReduceIt()
    {
        var (service, time, ticker) = CreateSut();
        service.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();

        service.Pause();
        var frozen = service.GetState().RemainingTime;
        time.Advance(TimeSpan.FromSeconds(30));
        ticker.RaiseTick();

        var state = service.GetState();
        Assert.Equal(frozen, state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(24) + TimeSpan.FromSeconds(50), state.RemainingTime);
        Assert.True(state.IsPaused);
        Assert.False(state.IsRunning);
        Assert.Null(state.EndTime);
        Assert.False(ticker.IsStarted);
        Assert.Equal(1, ticker.StopCount);
    }

    [Fact]
    public void Resume_SetsEndTimeToNowPlusFrozenRemaining()
    {
        var (service, time, ticker) = CreateSut();
        service.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();
        service.Pause();
        var frozen = service.GetState().RemainingTime;
        time.Advance(TimeSpan.FromSeconds(30));

        service.Resume();

        var state = service.GetState();
        Assert.True(state.IsRunning);
        Assert.False(state.IsPaused);
        Assert.Equal(time.GetUtcNow() + frozen, state.EndTime);
        Assert.True(ticker.IsStarted);
        Assert.Equal(2, ticker.StartCount);
    }

    [Fact]
    public void CompletingFocus_AdvancesToShortBreakOnSameCycle()
    {
        var (service, time, ticker) = CreateSut();

        CompleteCurrentPhase(service, time, ticker);

        var state = service.GetState();
        Assert.Equal(PomodoroPhase.ShortBreak, state.CurrentPhase);
        Assert.Equal(1, state.CurrentCycle);
        Assert.Equal(TimeSpan.FromMinutes(5), state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(5), state.TotalPhaseDuration);
        Assert.True(ticker.IsStarted);
    }

    [Fact]
    public void CompletingFocusOnCycleFour_AdvancesToLongBreak()
    {
        var (service, time, ticker) = CreateSut();

        CompleteFocusCyclesUntil(service, time, ticker, focusCycleToComplete: 4);

        var state = service.GetState();
        Assert.Equal(PomodoroPhase.LongBreak, state.CurrentPhase);
        Assert.Equal(4, state.CurrentCycle);
        Assert.Equal(TimeSpan.FromMinutes(15), state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(15), state.TotalPhaseDuration);
    }

    [Fact]
    public void CompletingLongBreak_ResetsToFocusCycleOne()
    {
        var (service, time, ticker) = CreateSut();
        CompleteFocusCyclesUntil(service, time, ticker, focusCycleToComplete: 4);

        CompleteCurrentPhase(service, time, ticker);

        var state = service.GetState();
        Assert.Equal(PomodoroPhase.Focus, state.CurrentPhase);
        Assert.Equal(1, state.CurrentCycle);
        Assert.Equal(TimeSpan.FromMinutes(25), state.RemainingTime);
    }

    [Fact]
    public void SkipToNextPhase_FromFocusOne_GoesToShortBreak()
    {
        var (service, _, _) = CreateSut();

        service.SkipToNextPhase();

        var state = service.GetState();
        Assert.Equal(PomodoroPhase.ShortBreak, state.CurrentPhase);
        Assert.Equal(1, state.CurrentCycle);
        Assert.Equal(TimeSpan.FromMinutes(5), state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(5), state.TotalPhaseDuration);
    }

    [Fact]
    public void RestartCurrentPhase_RestoresFullDurationOfCurrentPhase()
    {
        var (service, time, ticker) = CreateSut();
        service.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();

        service.RestartCurrentPhase();

        var state = service.GetState();
        Assert.Equal(PomodoroPhase.Focus, state.CurrentPhase);
        Assert.Equal(TimeSpan.FromMinutes(25), state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(25), state.TotalPhaseDuration);
        Assert.True(state.IsRunning);
        Assert.Equal(time.GetUtcNow() + TimeSpan.FromMinutes(25), state.EndTime);
    }

    [Fact]
    public void GetState_ReturnsCopy_MutatingItDoesNotChangeService()
    {
        var (service, _, _) = CreateSut();

        var copy = service.GetState();
        copy.CurrentCycle = 99;
        copy.RemainingTime = TimeSpan.Zero;
        copy.IsRunning = true;
        copy.CurrentPhase = PomodoroPhase.LongBreak;

        var state = service.GetState();
        Assert.Equal(1, state.CurrentCycle);
        Assert.Equal(TimeSpan.FromMinutes(25), state.RemainingTime);
        Assert.False(state.IsRunning);
        Assert.Equal(PomodoroPhase.Focus, state.CurrentPhase);
    }

    [Fact]
    public void Start_RaisesStateChanged_DoesNotRaisePhaseChanged()
    {
        var (service, _, ticker) = CreateSut();
        var events = new RaisedEvents(service);

        service.Start();

        Assert.Equal(1, events.StateChangedCount);
        Assert.Empty(events.PhaseChanges);
        Assert.True(ticker.IsStarted);
    }

    [Fact]
    public void Tick_RaisesStateChanged_DoesNotRaisePhaseChanged()
    {
        var (service, time, ticker) = CreateSut();
        service.Start();
        var events = new RaisedEvents(service);

        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();

        Assert.Equal(1, events.StateChangedCount);
        Assert.Empty(events.PhaseChanges);
    }

    [Fact]
    public void SkipToNextPhase_RaisesStateChangedAndPhaseChangedWithNewPhase()
    {
        var (service, _, _) = CreateSut();
        var events = new RaisedEvents(service);

        service.SkipToNextPhase();

        Assert.True(events.StateChangedCount >= 1);
        Assert.Equal(new[] { PomodoroPhase.ShortBreak }, events.PhaseChanges);
        Assert.Single(events.PhaseTransitions);
        Assert.Equal(PomodoroPhase.Focus, events.PhaseTransitions[0].CompletedPhase);
        Assert.Equal(PomodoroPhase.ShortBreak, events.PhaseTransitions[0].NextPhase);
    }

    [Fact]
    public void CompletingPhase_RaisesPhaseChangedWithNewPhaseAndStateChanged()
    {
        var (service, time, ticker) = CreateSut();
        service.Start();
        var events = new RaisedEvents(service);

        time.Advance(TimeSpan.FromMinutes(25));
        ticker.RaiseTick();

        Assert.True(events.StateChangedCount >= 1);
        Assert.Equal(new[] { PomodoroPhase.ShortBreak }, events.PhaseChanges);
        Assert.True(ticker.IsStarted);
        Assert.Single(events.PhaseTransitions);
        Assert.Equal(PomodoroPhase.Focus, events.PhaseTransitions[0].CompletedPhase);
        Assert.Equal(PomodoroPhase.ShortBreak, events.PhaseTransitions[0].NextPhase);
    }

    [Fact]
    public void Pause_DoesNotRaisePhaseTransitioned()
    {
        var (service, time, ticker) = CreateSut();
        service.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();
        var events = new RaisedEvents(service);

        service.Pause();

        Assert.Empty(events.PhaseTransitions);
        Assert.Empty(events.PhaseChanges);
    }

    [Fact]
    public void CompletingPhase_WhenAutoStartDisabled_GoesIdleOnNextPhase()
    {
        var settings = new PomodoroSettings { AutoStartNextPhase = false };
        var (service, time, ticker) = CreateSut(settings);

        CompleteCurrentPhase(service, time, ticker);

        var state = service.GetState();
        Assert.Equal(PomodoroPhase.ShortBreak, state.CurrentPhase);
        Assert.Equal(1, state.CurrentCycle);
        Assert.False(state.IsRunning);
        Assert.False(state.IsPaused);
        Assert.Null(state.EndTime);
        Assert.Equal(TimeSpan.FromMinutes(5), state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(5), state.TotalPhaseDuration);
        Assert.False(ticker.IsStarted);
    }

    [Fact]
    public void SkipToNextPhase_WhenAutoStartDisabled_GoesIdleOnNextPhase()
    {
        var settings = new PomodoroSettings { AutoStartNextPhase = false };
        var (service, _, ticker) = CreateSut(settings);

        service.SkipToNextPhase();

        var state = service.GetState();
        Assert.Equal(PomodoroPhase.ShortBreak, state.CurrentPhase);
        Assert.Equal(1, state.CurrentCycle);
        Assert.False(state.IsRunning);
        Assert.False(state.IsPaused);
        Assert.Null(state.EndTime);
        Assert.Equal(TimeSpan.FromMinutes(5), state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(5), state.TotalPhaseDuration);
        Assert.False(ticker.IsStarted);
    }

    [Fact]
    public void RestartCurrentPhase_WhenPaused_StaysPausedWithFullDuration()
    {
        var (service, time, ticker) = CreateSut();
        service.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();
        service.Pause();

        service.RestartCurrentPhase();

        var state = service.GetState();
        Assert.True(state.IsPaused);
        Assert.False(state.IsRunning);
        Assert.Equal(TimeSpan.FromMinutes(25), state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(25), state.TotalPhaseDuration);
        Assert.Null(state.EndTime);
        Assert.False(ticker.IsStarted);
    }

    [Fact]
    public void RestartCurrentPhase_WhenIdle_StaysIdleWithFullDuration()
    {
        var (service, _, ticker) = CreateSut();

        service.RestartCurrentPhase();

        var state = service.GetState();
        Assert.False(state.IsRunning);
        Assert.False(state.IsPaused);
        Assert.Equal(PomodoroPhase.Focus, state.CurrentPhase);
        Assert.Equal(TimeSpan.FromMinutes(25), state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(25), state.TotalPhaseDuration);
        Assert.Null(state.EndTime);
        Assert.False(ticker.IsStarted);
    }

    [Fact]
    public void SkipToNextPhase_WhenAutoStartEnabled_PhaseChangedHandlerSeesRunningNewPhase()
    {
        var (service, time, _) = CreateSut();
        service.Start();
        var observed = false;

        service.PhaseChanged += (_, phase) =>
        {
            observed = true;
            Assert.Equal(PomodoroPhase.ShortBreak, phase);
            var state = service.GetState();
            Assert.Equal(PomodoroPhase.ShortBreak, state.CurrentPhase);
            Assert.True(state.IsRunning);
            Assert.False(state.IsPaused);
            Assert.Equal(TimeSpan.FromMinutes(5), state.RemainingTime);
            Assert.NotNull(state.EndTime);
            Assert.Equal(time.GetUtcNow() + TimeSpan.FromMinutes(5), state.EndTime);
        };

        service.SkipToNextPhase();

        Assert.True(observed);
    }

    [Fact]
    public void SkipToNextPhase_WhenAutoStartDisabled_PhaseChangedHandlerSeesIdleNewPhase()
    {
        var settings = new PomodoroSettings { AutoStartNextPhase = false };
        var (service, _, _) = CreateSut(settings);
        service.Start();
        var observed = false;

        service.PhaseChanged += (_, phase) =>
        {
            observed = true;
            Assert.Equal(PomodoroPhase.ShortBreak, phase);
            var state = service.GetState();
            Assert.Equal(PomodoroPhase.ShortBreak, state.CurrentPhase);
            Assert.False(state.IsRunning);
            Assert.False(state.IsPaused);
            Assert.Null(state.EndTime);
            Assert.Equal(TimeSpan.FromMinutes(5), state.RemainingTime);
        };

        service.SkipToNextPhase();

        Assert.True(observed);
    }

    [Fact]
    public void CompletingPhase_WhenAutoStartEnabled_PhaseChangedHandlerSeesRunningNewPhase()
    {
        var (service, time, ticker) = CreateSut();
        service.Start();
        var observed = false;

        service.PhaseChanged += (_, phase) =>
        {
            observed = true;
            Assert.Equal(PomodoroPhase.ShortBreak, phase);
            var state = service.GetState();
            Assert.Equal(PomodoroPhase.ShortBreak, state.CurrentPhase);
            Assert.True(state.IsRunning);
            Assert.False(state.IsPaused);
            Assert.Equal(TimeSpan.FromMinutes(5), state.RemainingTime);
            Assert.NotNull(state.EndTime);
            Assert.Equal(time.GetUtcNow() + TimeSpan.FromMinutes(5), state.EndTime);
        };

        time.Advance(TimeSpan.FromMinutes(25));
        ticker.RaiseTick();

        Assert.True(observed);
    }

    [Fact]
    public void CompletingPhase_WhenAutoStartDisabled_PhaseChangedHandlerSeesIdleNewPhase()
    {
        var settings = new PomodoroSettings { AutoStartNextPhase = false };
        var (service, time, ticker) = CreateSut(settings);
        service.Start();
        var observed = false;

        service.PhaseChanged += (_, phase) =>
        {
            observed = true;
            Assert.Equal(PomodoroPhase.ShortBreak, phase);
            var state = service.GetState();
            Assert.Equal(PomodoroPhase.ShortBreak, state.CurrentPhase);
            Assert.False(state.IsRunning);
            Assert.False(state.IsPaused);
            Assert.Null(state.EndTime);
            Assert.Equal(TimeSpan.FromMinutes(5), state.RemainingTime);
        };

        time.Advance(TimeSpan.FromMinutes(25));
        ticker.RaiseTick();

        Assert.True(observed);
    }

    [Fact]
    public void Start_WhenPaused_BehavesLikeResume()
    {
        var (service, time, ticker) = CreateSut();
        service.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();
        service.Pause();
        var frozen = service.GetState().RemainingTime;
        time.Advance(TimeSpan.FromSeconds(30));

        service.Start();

        var state = service.GetState();
        Assert.True(state.IsRunning);
        Assert.False(state.IsPaused);
        Assert.Equal(frozen, state.RemainingTime);
        Assert.Equal(time.GetUtcNow() + frozen, state.EndTime);
        Assert.True(ticker.IsStarted);
    }

    [Fact]
    public void ApplySettings_WhenIdle_UpdatesRemainingToNewFocusDuration()
    {
        var settings = new PomodoroSettings();
        var (service, _, _) = CreateSut(settings);
        settings.FocusDurationMinutes = 50;

        service.ApplySettings();

        var state = service.GetState();
        Assert.Equal(TimeSpan.FromMinutes(50), state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(50), state.TotalPhaseDuration);
        Assert.False(state.IsRunning);
        Assert.False(state.IsPaused);
    }

    [Fact]
    public void ApplySettings_WhenRunning_StopsAndResetsToNewDuration()
    {
        var settings = new PomodoroSettings();
        var (service, time, ticker) = CreateSut(settings);
        service.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();
        settings.FocusDurationMinutes = 50;

        service.ApplySettings();

        var state = service.GetState();
        Assert.Equal(TimeSpan.FromMinutes(50), state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(50), state.TotalPhaseDuration);
        Assert.False(state.IsRunning);
        Assert.False(state.IsPaused);
        Assert.Null(state.EndTime);
        Assert.False(ticker.IsStarted);
    }

    [Fact]
    public void ApplySettings_WhenPaused_StopsAndResetsToNewDuration()
    {
        var settings = new PomodoroSettings();
        var (service, time, ticker) = CreateSut(settings);
        service.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();
        service.Pause();
        settings.FocusDurationMinutes = 40;

        service.ApplySettings();

        var state = service.GetState();
        Assert.Equal(TimeSpan.FromMinutes(40), state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(40), state.TotalPhaseDuration);
        Assert.False(state.IsRunning);
        Assert.False(state.IsPaused);
        Assert.Null(state.EndTime);
        Assert.False(ticker.IsStarted);
    }

    [Fact]
    public void ApplySettings_WhenRunning_AndCurrentDurationUnchanged_KeepsRunning()
    {
        var settings = new PomodoroSettings();
        var (service, time, ticker) = CreateSut(settings);
        service.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();
        var remainingBefore = service.GetState().RemainingTime;
        settings.ShortBreakDurationMinutes = 10;
        settings.AlwaysOnTop = true;

        service.ApplySettings();

        var state = service.GetState();
        Assert.Equal(remainingBefore, state.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(25), state.TotalPhaseDuration);
        Assert.True(state.IsRunning);
        Assert.True(ticker.IsStarted);
    }

    [Fact]
    public void ApplySettings_RaisesStateChanged()
    {
        var (service, _, _) = CreateSut();
        var events = new RaisedEvents(service);

        service.ApplySettings();

        Assert.Equal(1, events.StateChangedCount);
    }

    private static void CompleteFocusCyclesUntil(
        PomodoroTimerService service,
        FakeTimeProvider time,
        FakeUiTicker ticker,
        int focusCycleToComplete)
    {
        const int maxIterations = 16;
        for (var i = 0; i < maxIterations; i++)
        {
            var before = service.GetState();
            CompleteCurrentPhase(service, time, ticker);
            var after = service.GetState();

            if (before.CurrentPhase == PomodoroPhase.Focus
                && before.CurrentCycle == focusCycleToComplete
                && after.CurrentPhase != PomodoroPhase.Focus)
            {
                return;
            }
        }

        Assert.Fail(
            $"Did not complete Focus cycle {focusCycleToComplete} within {maxIterations} phase transitions.");
    }

    private sealed class RaisedEvents
    {
        public int StateChangedCount { get; private set; }
        public List<PomodoroPhase> PhaseChanges { get; } = [];
        public List<PhaseTransition> PhaseTransitions { get; } = [];

        public RaisedEvents(PomodoroTimerService service)
        {
            service.StateChanged += (_, _) => StateChangedCount++;
            service.PhaseChanged += (_, phase) => PhaseChanges.Add(phase);
            service.PhaseTransitioned += (_, transition) => PhaseTransitions.Add(transition);
        }
    }
}
