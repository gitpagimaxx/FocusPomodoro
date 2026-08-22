using FocusPomodoro.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace FocusPomodoro;

public sealed partial class ContinueChoiceWindow : Window
{
    private const int WindowWidthDips = 320;
    private const int WindowHeightDips = 220;
    private readonly TaskCompletionSource<ContinueChoice> _completion = new();

    public ContinueChoiceWindow(string prompt)
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        Title = "FocusPomodoro";
        PromptText.Text = prompt;

        if (Content is FrameworkElement root)
        {
            root.Loaded += OnRootLoaded;
        }

        Closed += OnClosed;
    }

    public Task<ContinueChoice> Result => _completion.Task;

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

    private void OnContinueClick(object sender, RoutedEventArgs e) =>
        Complete(ContinueChoice.Continue);

    private void OnStartFreshClick(object sender, RoutedEventArgs e) =>
        Complete(ContinueChoice.StartFresh);

    private void OnClosed(object sender, WindowEventArgs e)
    {
        Closed -= OnClosed;
        _completion.TrySetResult(ContinueChoice.StartFresh);
    }

    private void Complete(ContinueChoice choice)
    {
        _completion.TrySetResult(choice);
        Close();
    }
}
