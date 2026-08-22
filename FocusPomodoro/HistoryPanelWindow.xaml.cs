using FocusPomodoro.Helpers;
using FocusPomodoro.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace FocusPomodoro;

public sealed partial class HistoryPanelWindow : Window
{
    public HistoryViewModel ViewModel { get; }

    public HistoryPanelWindow(HistoryViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        Title = "Histórico";

        if (Content is FrameworkElement root)
        {
            root.Loaded += OnRootLoaded;
        }
    }

    public void ResizeToDefault(double scale)
    {
        var size = WindowLayout.ToPixelSize(
            HistoryPanelLayout.DefaultWidthDips,
            HistoryPanelLayout.DefaultHeightDips,
            scale);
        AppWindow.Resize(new SizeInt32(size.Width, size.Height));
    }

    private async void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement root)
        {
            return;
        }

        root.Loaded -= OnRootLoaded;
        ResizeToDefault(root.XamlRoot?.RasterizationScale ?? 1.0);
        await ViewModel.LoadAsync();
    }
}
