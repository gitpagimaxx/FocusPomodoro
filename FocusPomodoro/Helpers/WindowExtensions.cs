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

    public static void SetSystemTitleBarVisible(this Window window, bool visible)
    {
        if (window.GetOverlappedPresenter() is not { } presenter)
        {
            return;
        }

        presenter.SetBorderAndTitleBar(visible, visible);
        if (visible)
        {
            presenter.IsMinimizable = false;
            presenter.IsMaximizable = false;
        }
    }

    public static void ApplyTransparentTitleBar(this Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        var titleBar = window.AppWindow.TitleBar;
        var transparent = Microsoft.UI.Colors.Transparent;

        titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
        titleBar.BackgroundColor = transparent;
        titleBar.InactiveBackgroundColor = transparent;
        titleBar.ButtonBackgroundColor = transparent;
        titleBar.ButtonInactiveBackgroundColor = transparent;
        titleBar.ButtonHoverBackgroundColor = transparent;
        titleBar.ButtonPressedBackgroundColor = transparent;
        titleBar.ButtonForegroundColor = transparent;
        titleBar.ButtonInactiveForegroundColor = transparent;
        titleBar.ButtonHoverForegroundColor = transparent;
        titleBar.ButtonPressedForegroundColor = transparent;
    }

    public static double GetRasterizationScale(this Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return (window.Content as FrameworkElement)?.XamlRoot?.RasterizationScale ?? 1.0;
    }

    public static void SetAppIcon(this Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        foreach (var iconPath in GetAppIconPaths())
        {
            if (File.Exists(iconPath))
            {
                window.AppWindow.SetIcon(iconPath);
                return;
            }
        }
    }

    private static IEnumerable<string> GetAppIconPaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Assets", AppIconFileName);

        string? packagePath = null;
        try
        {
            packagePath = Path.Combine(
                Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                "Assets",
                AppIconFileName);
        }
        catch (InvalidOperationException)
        {
        }

        if (!string.IsNullOrEmpty(packagePath))
        {
            yield return packagePath;
        }
    }
}
