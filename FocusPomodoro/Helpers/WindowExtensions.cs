using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace FocusPomodoro.Helpers;

public static class WindowExtensions
{
    private const string AppIconFileName = "AppIcon.ico";

    public static OverlappedPresenter? GetOverlappedPresenter(this Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return window.AppWindow.Presenter as OverlappedPresenter;
    }

    public static void SetAlwaysOnTop(this Window window, bool enabled)
    {
        if (window.GetOverlappedPresenter() is { } presenter)
        {
            presenter.IsAlwaysOnTop = enabled;
        }
    }

    public static void SetResizable(this Window window, bool enabled)
    {
        if (window.GetOverlappedPresenter() is { } presenter)
        {
            presenter.IsResizable = enabled;
        }
    }

    public static void SetMinimizable(this Window window, bool enabled)
    {
        if (window.GetOverlappedPresenter() is { } presenter)
        {
            presenter.IsMinimizable = enabled;
        }
    }

    public static void SetMaximizable(this Window window, bool enabled)
    {
        if (window.GetOverlappedPresenter() is { } presenter)
        {
            presenter.IsMaximizable = enabled;
        }
    }

    public static double GetRasterizationScale(this Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return (window.Content as FrameworkElement)?.XamlRoot?.RasterizationScale ?? 1.0;
    }

    public static void SetAppIcon(this Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", AppIconFileName);
        if (File.Exists(iconPath))
        {
            window.AppWindow.SetIcon(iconPath);
        }
    }
}
