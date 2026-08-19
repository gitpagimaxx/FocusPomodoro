using FocusPomodoro.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace FocusPomodoro;

public sealed partial class SettingsWindow : Window
{
    private const int WindowWidthDips = 380;
    private const int WindowHeightDips = 640;

    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        SystemBackdrop = new MicaBackdrop();
        Title = "Configurações";

        ViewModel.CloseRequested += OnCloseRequested;

        if (Content is FrameworkElement root)
        {
            root.Loaded += OnRootLoaded;
        }

        Closed += OnClosed;
    }

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

    private void OnCloseRequested(object? sender, EventArgs e) => Close();

    private void OnClosed(object sender, WindowEventArgs e)
    {
        ViewModel.CloseRequested -= OnCloseRequested;
        Closed -= OnClosed;
    }
}
