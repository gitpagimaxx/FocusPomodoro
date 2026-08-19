using FocusPomodoro.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace FocusPomodoro;

public sealed partial class CloseChoiceWindow : Window
{
    private const int WindowWidthDips = 320;
    private const int WindowHeightDips = 260;
    private readonly TaskCompletionSource<WindowCloseChoice> _completion = new();

    public CloseChoiceWindow()
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        Title = "FocusPomodoro";

        if (Content is FrameworkElement root)
        {
            root.Loaded += OnRootLoaded;
        }

        Closed += OnClosed;
    }

    public Task<WindowCloseChoice> Result => _completion.Task;

    private void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement root)
        {
            return;
        }

        root.Loaded -= OnRootLoaded;
        var scale = root.XamlRoot?.RasterizationScale ?? 1.0;
        AppWindow.Resize(new SizeInt32(
            (int)Math.Round(WindowWidthDips * scale),
            (int)Math.Round(WindowHeightDips * scale)));
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) =>
        Complete(WindowCloseChoice.MinimizeToTray);

    private void OnExitClick(object sender, RoutedEventArgs e) =>
        Complete(WindowCloseChoice.Exit);

    private void OnCancelClick(object sender, RoutedEventArgs e) =>
        Complete(WindowCloseChoice.Cancel);

    private void OnClosed(object sender, WindowEventArgs e)
    {
        Closed -= OnClosed;
        _completion.TrySetResult(WindowCloseChoice.Cancel);
    }

    private void Complete(WindowCloseChoice choice)
    {
        _completion.TrySetResult(choice);
        Close();
    }
}
