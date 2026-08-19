using FocusPomodoro.Helpers;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class WindowClosePolicyTests
{
    [Fact]
    public void Decide_WhenExiting_AlwaysExits()
    {
        var decision = WindowClosePolicy.Decide(isExiting: true, minimizeToTrayOnClose: true);

        Assert.Equal(WindowCloseDecision.Exit, decision);
    }

    [Fact]
    public void Decide_WhenMinimizeToTrayOnClose_HidesWithoutAsking()
    {
        var decision = WindowClosePolicy.Decide(isExiting: false, minimizeToTrayOnClose: true);

        Assert.Equal(WindowCloseDecision.HideToTray, decision);
    }

    [Fact]
    public void Decide_WhenMinimizeToTrayOnCloseIsOff_AsksUser()
    {
        var decision = WindowClosePolicy.Decide(isExiting: false, minimizeToTrayOnClose: false);

        Assert.Equal(WindowCloseDecision.AskUser, decision);
    }
}
