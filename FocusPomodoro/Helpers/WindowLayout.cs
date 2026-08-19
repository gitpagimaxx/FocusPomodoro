namespace FocusPomodoro.Helpers;

public readonly record struct PixelRect(int X, int Y, int Width, int Height);

public readonly record struct PixelSize(int Width, int Height);

public readonly record struct PixelPoint(int X, int Y);

public static class WindowLayout
{
    public const int DefaultWidthDips = 260;
    public const int DefaultHeightDips = 150;
    public const int SafeMarginPixels = 16;

    public static PixelSize ToPixelSize(int widthDips, int heightDips, double scale)
    {
        var width = widthDips > 0 ? widthDips : DefaultWidthDips;
        var height = heightDips > 0 ? heightDips : DefaultHeightDips;
        var safeScale = scale > 0 ? scale : 1.0;

        return new PixelSize(
            Math.Max(1, (int)Math.Round(width * safeScale)),
            Math.Max(1, (int)Math.Round(height * safeScale)));
    }

    public static PixelSize ToDips(int widthPixels, int heightPixels, double scale)
    {
        var safeScale = scale > 0 ? scale : 1.0;
        return new PixelSize(
            Math.Max(1, (int)Math.Round(widthPixels / safeScale)),
            Math.Max(1, (int)Math.Round(heightPixels / safeScale)));
    }

    public static bool HasPersistedPosition(int x, int y) => x >= 0 && y >= 0;

    public static PixelPoint BottomRight(
        PixelRect workArea,
        PixelSize windowSize,
        int margin = SafeMarginPixels)
    {
        var x = workArea.X + workArea.Width - windowSize.Width - margin;
        var y = workArea.Y + workArea.Height - windowSize.Height - margin;

        return new PixelPoint(
            Math.Max(workArea.X, x),
            Math.Max(workArea.Y, y));
    }

    public static bool IsVisibleOn(PixelPoint position, PixelSize size, PixelRect workArea)
    {
        var right = position.X + size.Width;
        var bottom = position.Y + size.Height;
        var workRight = workArea.X + workArea.Width;
        var workBottom = workArea.Y + workArea.Height;

        return position.X < workRight
            && position.Y < workBottom
            && right > workArea.X
            && bottom > workArea.Y;
    }
}
