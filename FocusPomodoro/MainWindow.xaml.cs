using FocusPomodoro.Helpers;
using FocusPomodoro.Models;
using FocusPomodoro.Services;
using FocusPomodoro.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace FocusPomodoro;

public sealed partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IWindowService _windowService;
    private SettingsWindow? _settingsWindow;
    private CloseChoiceWindow? _closeChoiceWindow;
    private ContinueChoiceWindow? _continueChoiceWindow;
    private HistoryPanelWindow? _historyWindow;

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
        SystemBackdrop = null;
        Title = ViewModel.Title;

        _windowService.Initialize(this, DragRegion);

        ViewModel.SettingsRequested += OnSettingsRequested;
        ViewModel.HistoryRequested += OnHistoryRequested;
        ViewModel.CloseChoiceRequested += OnCloseChoiceRequested;
        ViewModel.ContinueChoiceRequested += OnContinueChoiceRequested;
        _settingsService.SettingsChanged += OnSettingsChanged;
        Closed += OnClosed;
        AppWindow.Changed += OnAppWindowChanged;

        WindowAppearance.Apply(this, _settingsService.Current);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnHistoryRequested(object? sender, EventArgs e)
    {
        if (_historyWindow is not null)
        {
            _historyWindow.Close();
            return;
        }

        _historyWindow = App.Services.GetRequiredService<HistoryPanelWindow>();
        _historyWindow.Closed += OnHistoryWindowClosed;
        WindowAppearance.Apply(_historyWindow, _settingsService.Current);
        _historyWindow.Activate();
        PositionHistoryWindow();
    }

    private Task<ContinueChoice> OnContinueChoiceRequested(string prompt)
    {
        if (_continueChoiceWindow is not null)
        {
            _continueChoiceWindow.Activate();
            return _continueChoiceWindow.Result;
        }

        _windowService.ShowAndActivate();
        _continueChoiceWindow = new ContinueChoiceWindow(prompt);
        WindowAppearance.Apply(_continueChoiceWindow, _settingsService.Current);
        _continueChoiceWindow.Closed += OnContinueChoiceWindowClosed;
        _continueChoiceWindow.Activate();
        return _continueChoiceWindow.Result;
    }

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

        if (_historyWindow is not null)
        {
            WindowAppearance.Apply(_historyWindow, _settingsService.Current);
        }
    }

    private void OnHistoryWindowClosed(object sender, WindowEventArgs e)
    {
        if (_historyWindow is not null)
        {
            _historyWindow.Closed -= OnHistoryWindowClosed;
        }

        _historyWindow = null;
    }

    private void OnContinueChoiceWindowClosed(object sender, WindowEventArgs e)
    {
        if (_continueChoiceWindow is not null)
        {
            _continueChoiceWindow.Closed -= OnContinueChoiceWindowClosed;
        }

        _continueChoiceWindow = null;
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPositionChange || args.DidSizeChange)
        {
            PositionHistoryWindow();
        }
    }

    private void PositionHistoryWindow()
    {
        if (_historyWindow is null)
        {
            return;
        }

        var scale = (_historyWindow.Content as FrameworkElement)?.XamlRoot?.RasterizationScale
            ?? (Content as FrameworkElement)?.XamlRoot?.RasterizationScale
            ?? 1.0;
        _historyWindow.ResizeToDefault(scale);

        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
        var work = display.WorkArea;
        var owner = new PixelRect(
            AppWindow.Position.X,
            AppWindow.Position.Y,
            AppWindow.Size.Width,
            AppWindow.Size.Height);
        var panelSize = new PixelSize(
            _historyWindow.AppWindow.Size.Width,
            _historyWindow.AppWindow.Size.Height);
        var position = HistoryPanelLayout.Place(
            new PixelRect(work.X, work.Y, work.Width, work.Height),
            owner,
            panelSize);
        _historyWindow.AppWindow.Move(new PointInt32(position.X, position.Y));
    }

    private void OnClosed(object sender, WindowEventArgs e)
    {
        ViewModel.SettingsRequested -= OnSettingsRequested;
        ViewModel.HistoryRequested -= OnHistoryRequested;
        ViewModel.CloseChoiceRequested -= OnCloseChoiceRequested;
        ViewModel.ContinueChoiceRequested -= OnContinueChoiceRequested;
        _settingsService.SettingsChanged -= OnSettingsChanged;
        AppWindow.Changed -= OnAppWindowChanged;
        Closed -= OnClosed;
        _settingsWindow?.Close();
        _closeChoiceWindow?.Close();
        _continueChoiceWindow?.Close();
        _historyWindow?.Close();
    }
}
