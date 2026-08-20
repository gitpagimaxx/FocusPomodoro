using FocusPomodoro.Helpers;
using FocusPomodoro.Services;
using FocusPomodoro.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace FocusPomodoro;

public sealed partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IWindowService _windowService;
    private SettingsWindow? _settingsWindow;
    private CloseChoiceWindow? _closeChoiceWindow;

    public MainViewModel ViewModel { get; }

    public MainWindow(
        MainViewModel viewModel,
        ISettingsService settingsService,
        IWindowService windowService)
    {
        ViewModel = viewModel;
        _settingsService = settingsService;
        _windowService = windowService;
        InitializeComponent();

        SystemBackdrop = new MicaBackdrop();
        Title = ViewModel.Title;

        _windowService.Initialize(this, DragRegion);

        ViewModel.SettingsRequested += OnSettingsRequested;
        ViewModel.CloseChoiceRequested += OnCloseChoiceRequested;
        _settingsService.SettingsChanged += OnSettingsChanged;
        Closed += OnClosed;

        WindowAppearance.Apply(this, _settingsService.Current);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnSettingsRequested(object? sender, EventArgs e)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = App.Services.GetRequiredService<SettingsWindow>();
        _settingsWindow.Closed += OnSettingsWindowClosed;
        WindowAppearance.Apply(_settingsWindow, _settingsService.Current);
        _settingsWindow.Activate();
    }

    private Task<WindowCloseChoice> OnCloseChoiceRequested()
    {
        if (_closeChoiceWindow is not null)
        {
            _closeChoiceWindow.Activate();
            return _closeChoiceWindow.Result;
        }

        _windowService.ShowAndActivate();
        _closeChoiceWindow = new CloseChoiceWindow();
        WindowAppearance.Apply(_closeChoiceWindow, _settingsService.Current);
        _closeChoiceWindow.Closed += OnCloseChoiceWindowClosed;
        _closeChoiceWindow.Activate();
        return _closeChoiceWindow.Result;
    }

    private void OnCloseChoiceWindowClosed(object sender, WindowEventArgs e)
    {
        if (_closeChoiceWindow is not null)
        {
            _closeChoiceWindow.Closed -= OnCloseChoiceWindowClosed;
        }

        _closeChoiceWindow = null;
    }

    private void OnSettingsWindowClosed(object sender, WindowEventArgs e)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Closed -= OnSettingsWindowClosed;
        }

        _settingsWindow = null;
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        WindowAppearance.Apply(this, _settingsService.Current);
        if (_settingsWindow is not null)
        {
            WindowAppearance.Apply(_settingsWindow, _settingsService.Current);
        }
    }

    private void OnClosed(object sender, WindowEventArgs e)
    {
        ViewModel.SettingsRequested -= OnSettingsRequested;
        ViewModel.CloseChoiceRequested -= OnCloseChoiceRequested;
        _settingsService.SettingsChanged -= OnSettingsChanged;
        Closed -= OnClosed;
        _settingsWindow?.Close();
        _closeChoiceWindow?.Close();
    }
}
