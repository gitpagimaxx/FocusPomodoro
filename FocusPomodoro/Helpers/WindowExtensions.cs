using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace FocusPomodoro.Helpers;

public static class WindowExtensions
{
    private const string AppIconFileName = "AppIcon.ico";
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const int DwmwcpDoNotRound = 1;
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const int GclpHBrush = -10;
    private const int WsThickFrame = 0x00040000;
    private const int WsExDlgModalFrame = 0x00000001;
    private const int WsExWindowEdge = 0x00000100;
    private const int WsExClientEdge = 0x00000200;
    private const int WsExStaticEdge = 0x00020000;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const uint DarkColorBgr = 0x001E1C1C;
    private const uint LightTextBgr = 0x00F2F2F2;
    private static readonly Windows.UI.Color FrameColor = Windows.UI.Color.FromArgb(255, 0x1C, 0x1C, 0x1E);
    private static nint _darkWindowBrush;

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
        titleBar.BackgroundColor = FrameColor;
        titleBar.InactiveBackgroundColor = FrameColor;
        titleBar.ButtonBackgroundColor = FrameColor;
        titleBar.ButtonInactiveBackgroundColor = FrameColor;
        titleBar.ButtonHoverBackgroundColor = FrameColor;
        titleBar.ButtonPressedBackgroundColor = FrameColor;
        titleBar.ButtonForegroundColor = transparent;
        titleBar.ButtonInactiveForegroundColor = transparent;
        titleBar.ButtonHoverForegroundColor = transparent;
        titleBar.ButtonPressedForegroundColor = transparent;
        ApplyDarkFrame(window);
    }

    public static void HideSystemBorder(this Window window) => ApplyDarkFrame(window);

    public static void ApplyDarkFrame(this Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        var hwnd = WindowNative.GetWindowHandle(window);
        if (hwnd == 0)
        {
            return;
        }

        var dark = 1;
        _ = DwmSetWindowAttributeInt(hwnd, DwmwaUseImmersiveDarkModeBefore20H1, ref dark, sizeof(int));
        _ = DwmSetWindowAttributeInt(hwnd, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int));

        var corners = DwmwcpDoNotRound;
        _ = DwmSetWindowAttributeInt(hwnd, DwmwaWindowCornerPreference, ref corners, sizeof(int));

        var frameColor = unchecked((int)DarkColorBgr);
        var textColor = unchecked((int)LightTextBgr);
        _ = DwmSetWindowAttributeInt(hwnd, DwmwaBorderColor, ref frameColor, sizeof(int));
        _ = DwmSetWindowAttributeInt(hwnd, DwmwaCaptionColor, ref frameColor, sizeof(int));
        _ = DwmSetWindowAttributeInt(hwnd, DwmwaTextColor, ref textColor, sizeof(int));
        ApplyDarkWindowBrush(hwnd);

        var style = GetWindowLongValue(hwnd, GwlStyle);
        var stripped = style & ~WsThickFrame;
        var exStyle = GetWindowLongValue(hwnd, GwlExStyle);
        var strippedEx = exStyle
            & ~WsExDlgModalFrame
            & ~WsExWindowEdge
            & ~WsExClientEdge
            & ~WsExStaticEdge;
        if (stripped != style || strippedEx != exStyle)
        {
            SetWindowLongValue(hwnd, GwlStyle, stripped);
            SetWindowLongValue(hwnd, GwlExStyle, strippedEx);
            _ = SetWindowPos(
                hwnd,
                0,
                0,
                0,
                0,
                0,
                SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
        }
    }

    private static void ApplyDarkWindowBrush(nint hwnd)
    {
        if (_darkWindowBrush == 0)
        {
            _darkWindowBrush = CreateSolidBrush(DarkColorBgr);
        }

        if (nint.Size == 8)
        {
            _ = SetClassLongPtr(hwnd, GclpHBrush, _darkWindowBrush);
            return;
        }

        _ = SetClassLong(hwnd, GclpHBrush, (int)_darkWindowBrush);
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

    private static nint GetWindowLongValue(nint hwnd, int index) =>
        nint.Size == 8 ? GetWindowLongPtr(hwnd, index) : GetWindowLong(hwnd, index);

    private static void SetWindowLongValue(nint hwnd, int index, nint value)
    {
        if (nint.Size == 8)
        {
            _ = SetWindowLongPtr(hwnd, index, value);
            return;
        }

        _ = SetWindowLong(hwnd, index, (int)value);
    }

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
    private static extern int DwmSetWindowAttributeInt(nint hwnd, int attribute, ref int attributeValue, int attributeSize);

    [DllImport("gdi32.dll")]
    private static extern nint CreateSolidBrush(uint color);

    [DllImport("user32.dll", EntryPoint = "SetClassLongW")]
    private static extern int SetClassLong(nint hwnd, int index, int value);

    [DllImport("user32.dll", EntryPoint = "SetClassLongPtrW")]
    private static extern nint SetClassLongPtr(nint hwnd, int index, nint value);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(nint hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(nint hwnd, int index, int value);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        nint hwnd,
        nint insertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);
}
