using FocusPomodoro.Helpers;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class HistoryPanelLayoutTests
{
    [Fact]
    public void Place_PrefersRightOfOwner_WhenItFits()
    {
        var workArea = new PixelRect(0, 0, 1920, 1080);
        var owner = new PixelRect(100, 200, 260, 78);
        var panel = new PixelSize(280, 400);

        var position = HistoryPanelLayout.Place(workArea, owner, panel);

        Assert.Equal(100 + 260 + HistoryPanelLayout.GapPixels, position.X);
        Assert.Equal(200, position.Y);
    }

    [Fact]
    public void Place_UsesLeftOfOwner_WhenRightWouldOverflow()
    {
        var workArea = new PixelRect(0, 0, 800, 600);
        var owner = new PixelRect(600, 100, 180, 78);
        var panel = new PixelSize(280, 400);

        var position = HistoryPanelLayout.Place(workArea, owner, panel);

        Assert.Equal(600 - 280 - HistoryPanelLayout.GapPixels, position.X);
        Assert.Equal(100, position.Y);
    }

    [Fact]
    public void Place_ClampsIntoWorkArea_WhenBothSidesOverflow()
    {
        var workArea = new PixelRect(10, 20, 200, 150);
        var owner = new PixelRect(10, 20, 180, 78);
        var panel = new PixelSize(280, 400);

        var position = HistoryPanelLayout.Place(workArea, owner, panel);

        Assert.Equal(10, position.X);
        Assert.Equal(20, position.Y);
    }
}
