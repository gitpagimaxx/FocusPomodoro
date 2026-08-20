using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace FocusPomodoro.Helpers;

internal static class WindowDrag
{
    private const int WmNcLButtonDown = 0x00A1;
    private const int HtCaption = 0x0002;

    public static void TryBegin(Window window, PointerRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(args);

        var relative = window.Content as UIElement;
        if (relative is null || !args.GetCurrentPoint(relative).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (IsInteractive(args.OriginalSource as DependencyObject))
        {
            return;
        }

        var hwnd = WindowNative.GetWindowHandle(window);
        _ = ReleaseCapture();
        _ = SendMessage(hwnd, WmNcLButtonDown, HtCaption, 0);
    }

    public static bool IsInteractive(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is ButtonBase)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);
}
