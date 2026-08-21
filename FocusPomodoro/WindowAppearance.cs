using FocusPomodoro.Helpers;
using FocusPomodoro.Models;
using Microsoft.UI.Xaml;

namespace FocusPomodoro;

internal static class WindowAppearance
{
    public static void Apply(Window window, PomodoroSettings settings)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(settings);

        if (window.Content is FrameworkElement root)
        {
            root.RequestedTheme = settings.AppTheme == AppTheme.Light
                ? ElementTheme.Light
                : ElementTheme.Dark;
        }

        window.SetAlwaysOnTop(settings.AlwaysOnTop);
        window.SetAppIcon();
        window.ApplyDarkFrame();
    }
}
