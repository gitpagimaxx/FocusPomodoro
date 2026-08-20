using FocusPomodoro.Helpers;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class WindowLayoutTests
{
    [Fact]
    public void ToPixelSize_WhenSizeIsMissing_UsesDefaultDipsScaled()
    {
        var size = WindowLayout.ToPixelSize(widthDips: 0, heightDips: 0, scale: 1.0);

        Assert.Equal(260, size.Width);
        Assert.Equal(78, size.Height);
    }

    [Fact]
    public void ToPixelSize_WhenSizeIsMissing_AppliesRasterizationScale()
    {
        var size = WindowLayout.ToPixelSize(widthDips: -1, heightDips: -1, scale: 1.5);

        Assert.Equal(390, size.Width);
        Assert.Equal(117, size.Height);
    }

    [Fact]
    public void ToPixelSize_WhenSizeIsSaved_UsesSavedDipsScaled()
    {
        var size = WindowLayout.ToPixelSize(widthDips: 300, heightDips: 200, scale: 1.25);

        Assert.Equal(375, size.Width);
        Assert.Equal(250, size.Height);
    }

    [Theory]
    [InlineData(-1, -1, false)]
    [InlineData(-1, 80, false)]
    [InlineData(120, -1, false)]
    [InlineData(0, 0, true)]
    [InlineData(120, 80, true)]
    public void HasPersistedPosition_RequiresNonNegativeCoordinates(int x, int y, bool expected)
    {
        Assert.Equal(expected, WindowLayout.HasPersistedPosition(x, y));
    }

    [Fact]
    public void BottomRight_PlacesWindowAboveTaskbarWithSafeMargin()
    {
        var workArea = new PixelRect(0, 0, 1920, 1040);
        var window = new PixelSize(260, 78);

        var position = WindowLayout.BottomRight(workArea, window);

        Assert.Equal(1920 - 260 - 16, position.X);
        Assert.Equal(1040 - 78 - 16, position.Y);
    }

    [Fact]
    public void BottomRight_UsesWorkAreaOrigin_ForSecondaryOffsetMonitor()
    {
        var workArea = new PixelRect(1920, 100, 1600, 900);
        var window = new PixelSize(260, 78);

        var position = WindowLayout.BottomRight(workArea, window);

        Assert.Equal(1920 + 1600 - 260 - 16, position.X);
        Assert.Equal(100 + 900 - 78 - 16, position.Y);
    }

    [Fact]
    public void BottomRight_ClampsToWorkAreaOrigin_WhenWindowIsLargerThanWorkArea()
    {
        var workArea = new PixelRect(10, 20, 200, 100);
        var window = new PixelSize(400, 300);

        var position = WindowLayout.BottomRight(workArea, window);

        Assert.Equal(10, position.X);
        Assert.Equal(20, position.Y);
    }

    [Fact]
    public void IsVisibleOn_WhenFullyInsideWorkArea_IsTrue()
    {
        var visible = WindowLayout.IsVisibleOn(
            new PixelPoint(1600, 800),
            new PixelSize(260, 78),
            new PixelRect(0, 0, 1920, 1040));

        Assert.True(visible);
    }

    [Fact]
    public void IsVisibleOn_WhenCompletelyOffScreen_IsFalse()
    {
        var visible = WindowLayout.IsVisibleOn(
            new PixelPoint(4000, 2000),
            new PixelSize(260, 78),
            new PixelRect(0, 0, 1920, 1040));

        Assert.False(visible);
    }

    [Fact]
    public void ToDips_ConvertsPixelsBackUsingScale()
    {
        var dips = WindowLayout.ToDips(widthPixels: 390, heightPixels: 117, scale: 1.5);

        Assert.Equal(260, dips.Width);
        Assert.Equal(78, dips.Height);
    }
}
