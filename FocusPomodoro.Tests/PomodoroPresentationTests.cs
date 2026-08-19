using FocusPomodoro.Models;
using FocusPomodoro.ViewModels;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class PomodoroPresentationTests
{
    private static readonly TimeSpan FocusDuration = TimeSpan.FromMinutes(25);

    [Fact]
    public void ProgressPercentage_RemainingEqualsTotalDuration_IsZero()
    {
        var progress = PomodoroPresentation.ProgressPercentage(FocusDuration, FocusDuration);

        Assert.Equal(0, progress);
    }

    [Fact]
    public void ProgressPercentage_RemainingIsZero_IsOneHundred()
    {
        var progress = PomodoroPresentation.ProgressPercentage(TimeSpan.Zero, FocusDuration);

        Assert.Equal(100, progress);
    }

    [Fact]
    public void ProgressPercentage_HalfwayThroughPhase_IsFifty()
    {
        var remaining = TimeSpan.FromMinutes(12) + TimeSpan.FromSeconds(30);

        var progress = PomodoroPresentation.ProgressPercentage(remaining, FocusDuration);

        Assert.Equal(50, progress);
    }

    [Fact]
    public void ProgressPercentage_RemainingGreaterThanTotal_IsClampedToZero()
    {
        var remaining = TimeSpan.FromMinutes(30);

        var progress = PomodoroPresentation.ProgressPercentage(remaining, FocusDuration);

        Assert.Equal(0, progress);
    }

    [Fact]
    public void ProgressPercentage_TotalIsZero_IsZero()
    {
        var progress = PomodoroPresentation.ProgressPercentage(TimeSpan.Zero, TimeSpan.Zero);

        Assert.Equal(0, progress);
    }

    [Fact]
    public void ProgressPercentage_TotalIsNegative_IsZero()
    {
        var progress = PomodoroPresentation.ProgressPercentage(
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(-1));

        Assert.Equal(0, progress);
    }

    [Fact]
    public void ProgressPercentage_RemainingIsNegative_IsOneHundredWhenTotalIsPositive()
    {
        var progress = PomodoroPresentation.ProgressPercentage(
            TimeSpan.FromSeconds(-1),
            FocusDuration);

        Assert.Equal(100, progress);
    }

    [Theory]
    [InlineData(PomodoroPhase.Focus, "Foco")]
    [InlineData(PomodoroPhase.ShortBreak, "Pausa curta")]
    [InlineData(PomodoroPhase.LongBreak, "Pausa longa")]
    public void CurrentPhaseText_ReturnsPortugueseLabel(PomodoroPhase phase, string expected)
    {
        Assert.Equal(expected, PomodoroPresentation.CurrentPhaseText(phase));
    }

    [Fact]
    public void CycleText_FormatsCurrentCycleAndCyclesBeforeLongBreak()
    {
        Assert.Equal("Ciclo 1 de 4", PomodoroPresentation.CycleText(1, 4));
        Assert.Equal("Ciclo 3 de 4", PomodoroPresentation.CycleText(3, 4));
        Assert.Equal("Ciclo 2 de 6", PomodoroPresentation.CycleText(2, 6));
    }

    [Theory]
    [InlineData(false, false, "\uE768")]
    [InlineData(true, false, "\uE769")]
    [InlineData(false, true, "\uE768")]
    public void PrimaryActionGlyph_ShowsPauseOnlyWhileRunning(bool isRunning, bool isPaused, string expected)
    {
        Assert.Equal(expected, PomodoroPresentation.PrimaryActionGlyph(isRunning, isPaused));
    }

    [Fact]
    public void TimeRemainingText_FormatsMinutesAndSeconds()
    {
        Assert.Equal("25:00", PomodoroPresentation.TimeRemainingText(TimeSpan.FromMinutes(25)));
        Assert.Equal("00:09", PomodoroPresentation.TimeRemainingText(TimeSpan.FromSeconds(9)));
        Assert.Equal(
            "24:50",
            PomodoroPresentation.TimeRemainingText(TimeSpan.FromMinutes(24) + TimeSpan.FromSeconds(50)));
    }

    [Theory]
    [InlineData(PomodoroPhase.Focus, "#E85D4C", (byte)0xE8, (byte)0x5D, (byte)0x4C)]
    [InlineData(PomodoroPhase.ShortBreak, "#34D399", (byte)0x34, (byte)0xD3, (byte)0x99)]
    [InlineData(PomodoroPhase.LongBreak, "#818CF8", (byte)0x81, (byte)0x8C, (byte)0xF8)]
    public void Accent_ReturnsHexAndRgbForPhase(
        PomodoroPhase phase,
        string expectedHex,
        byte expectedR,
        byte expectedG,
        byte expectedB)
    {
        Assert.Equal(expectedHex, PomodoroPresentation.AccentHex(phase));

        var (r, g, b) = PomodoroPresentation.AccentRgb(phase);
        Assert.Equal(expectedR, r);
        Assert.Equal(expectedG, g);
        Assert.Equal(expectedB, b);
    }
}
