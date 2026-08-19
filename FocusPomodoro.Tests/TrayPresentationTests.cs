using FocusPomodoro.ViewModels;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class TrayPresentationTests
{
    [Theory]
    [InlineData(false, false, "Iniciar")]
    [InlineData(true, false, "Pausar")]
    [InlineData(false, true, "Retomar")]
    public void TimerActionText_FollowsTimerState(bool isRunning, bool isPaused, string expected)
    {
        Assert.Equal(expected, TrayPresentation.TimerActionText(isRunning, isPaused));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    public void CanRestart_WhenRunningOrPaused(bool isRunning, bool isPaused, bool expected)
    {
        Assert.Equal(expected, TrayPresentation.CanRestart(isRunning, isPaused));
    }

    [Fact]
    public void ToolTip_IncludesPhaseAndRemainingTime()
    {
        var tooltip = TrayPresentation.ToolTip("Foco", "24:50");

        Assert.Equal("FocusPomodoro — Foco 24:50", tooltip);
    }
}
