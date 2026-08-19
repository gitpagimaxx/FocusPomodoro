using FocusPomodoro.Models;
using FocusPomodoro.Services;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class SoundServiceTests
{
    [Fact]
    public void CompletingPhase_PlaysSoundOnce()
    {
        var (timer, time, ticker, settings) = CreateTimer();
        var plays = 0;
        _ = new SoundService(timer, settings, () => plays++);

        CompleteCurrentPhase(timer, time, ticker);

        Assert.Equal(1, plays);
    }

    [Fact]
    public void SkipToNextPhase_PlaysSound()
    {
        var (timer, _, _, settings) = CreateTimer();
        var plays = 0;
        _ = new SoundService(timer, settings, () => plays++);

        timer.SkipToNextPhase();

        Assert.Equal(1, plays);
    }

    [Fact]
    public void Pause_DoesNotPlaySound()
    {
        var (timer, time, ticker, settings) = CreateTimer();
        var plays = 0;
        _ = new SoundService(timer, settings, () => plays++);
        timer.Start();
        time.Advance(TimeSpan.FromSeconds(10));
        ticker.RaiseTick();

        timer.Pause();

        Assert.Equal(0, plays);
    }

    [Fact]
    public void SoundDisabled_DoesNotPlay()
    {
        var (timer, time, ticker, settings) = CreateTimer();
        settings.SoundEnabled = false;
        var plays = 0;
        _ = new SoundService(timer, settings, () => plays++);

        CompleteCurrentPhase(timer, time, ticker);
        timer.SkipToNextPhase();

        Assert.Equal(0, plays);
    }

    [Fact]
    public void Attach_CalledTwice_DoesNotDuplicateSound()
    {
        var (timer, time, ticker, settings) = CreateTimer();
        var plays = 0;
        var service = new SoundService(timer, settings, () => plays++);

        service.Attach();
        CompleteCurrentPhase(timer, time, ticker);

        Assert.Equal(1, plays);
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
}
