namespace FocusPomodoro.Helpers;

public static class HistoryPanelLayout
{
    public const int GapPixels = 8;
    public const int DefaultWidthDips = 280;
    public const int DefaultHeightDips = 400;

    public static PixelPoint Place(
        PixelRect workArea,
        PixelRect owner,
        PixelSize panel,
        int gap = GapPixels)
    {
        var workRight = workArea.X + workArea.Width;
        var workBottom = workArea.Y + workArea.Height;
        var preferredX = owner.X + owner.Width + gap;
        var x = preferredX + panel.Width <= workRight
            ? preferredX
            : owner.X - panel.Width - gap;
        var y = owner.Y;

        var maxX = Math.Max(workArea.X, workRight - panel.Width);
        var maxY = Math.Max(workArea.Y, workBottom - panel.Height);

        return new PixelPoint(
            Math.Clamp(x, workArea.X, maxX),
            Math.Clamp(y, workArea.Y, maxY));
    }
}
