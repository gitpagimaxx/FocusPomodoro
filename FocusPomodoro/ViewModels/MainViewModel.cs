using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusPomodoro.Helpers;
using FocusPomodoro.Models;
using FocusPomodoro.Services;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace FocusPomodoro.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPomodoroTimerService _timer;
    private readonly PomodoroSettings _settings;
    private readonly IWindowService _windowService;
    private readonly ITrayIconService _trayIcon;
    private readonly IUiTicker _ticker;
    private bool _isExiting;
    private bool _isHandlingClose;
    private byte _accentR = 0xE8;
    private byte _accentG = 0x5D;
    private byte _accentB = 0x4C;

    public MainViewModel(
        IPomodoroTimerService timer,
        PomodoroSettings settings,
        IWindowService windowService,
        ITrayIconService trayIcon,
        IUiTicker ticker)
    {
        _timer = timer;
        _settings = settings;
        _windowService = windowService;
        _trayIcon = trayIcon;
        _ticker = ticker;
        _timer.StateChanged += OnStateChanged;
        _windowService.CloseRequested += OnWindowCloseRequested;
        _trayIcon.ShowRequested += OnTrayShowRequested;
        _trayIcon.TimerActionRequested += OnTrayTimerActionRequested;
        _trayIcon.RestartRequested += OnTrayRestartRequested;
        _trayIcon.ResetCycleRequested += OnTrayResetCycleRequested;
        _trayIcon.ExitRequested += OnTrayExitRequested;
        ApplyState(_timer.GetState());
    }

    public event EventHandler? SettingsRequested;

    public event Func<Task<WindowCloseChoice>>? CloseChoiceRequested;

    public void RefreshTray()
    {
        var state = _timer.GetState();
        _trayIcon.Update(
            TrayPresentation.ToolTip(CurrentPhaseText, TimeRemainingText),
            TrayPresentation.TimerActionText(state.IsRunning, state.IsPaused),
            TrayPresentation.CanRestart(state.IsRunning, state.IsPaused));
    }

    [ObservableProperty]
    public partial string Title { get; set; } = "FocusPomodoro";

    [ObservableProperty]
    public partial string CurrentPhaseText { get; set; } = "Foco";

    [ObservableProperty]
    public partial string TimeRemainingText { get; set; } = "25:00";

    [ObservableProperty]
    public partial string CycleText { get; set; } = "1/4";

    [ObservableProperty]
    public partial double ProgressPercentage { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush PhaseAccentBrush { get; set; } =
        new(Color.FromArgb(255, 0xE8, 0x5D, 0x4C));

    [ObservableProperty]
    public partial bool IsStartEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsPauseEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsResumeEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsRestartEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsSkipEnabled { get; set; }

    [ObservableProperty]
    public partial string PrimaryActionGlyph { get; set; } = PomodoroPresentation.PlayGlyph;

    [ObservableProperty]
    public partial string PrimaryActionTooltip { get; set; } = "Iniciar";

    [RelayCommand]
    private void OpenSettings() => SettingsRequested?.Invoke(this, EventArgs.Empty);

    private void HideToTray() => _windowService.Hide();

    [RelayCommand]
    private void ShowWindow()
    {
        _windowService.ShowAndActivate();
        _ticker.Pulse();
    }

    [RelayCommand]
    private async Task ExitAsync() => await ExitCoreAsync();

    [RelayCommand]
    private void ToggleTimer()
    {
        var state = _timer.GetState();
        if (state.IsPaused)
        {
            Resume();
            return;
        }

        if (state.IsRunning)
        {
            Pause();
            return;
        }

        Start();
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start() => _timer.Start();

    private bool CanStart() => IsStartEnabled;

    partial void OnIsStartEnabledChanged(bool value)
    {
        StartCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause() => _timer.Pause();

    private bool CanPause() => IsPauseEnabled;

    partial void OnIsPauseEnabledChanged(bool value)
    {
        PauseCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanResume))]
    private void Resume() => _timer.Resume();

    private bool CanResume() => IsResumeEnabled;

    partial void OnIsResumeEnabledChanged(bool value)
    {
        ResumeCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRestart))]
    private void Restart() => _timer.RestartCurrentPhase();

    private bool CanRestart() => IsRestartEnabled;

    partial void OnIsRestartEnabledChanged(bool value)
    {
        RestartCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void ResetCycle() => _timer.ResetCycle();

    [RelayCommand(CanExecute = nameof(CanSkip))]
    private void Skip() => _timer.SkipToNextPhase();

    private bool CanSkip() => IsSkipEnabled;

    partial void OnIsSkipEnabledChanged(bool value)
    {
        SkipCommand.NotifyCanExecuteChanged();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        var state = _timer.GetState();
        ApplyState(state);
        _trayIcon.Update(
            TrayPresentation.ToolTip(CurrentPhaseText, TimeRemainingText),
            TrayPresentation.TimerActionText(state.IsRunning, state.IsPaused),
            TrayPresentation.CanRestart(state.IsRunning, state.IsPaused));
    }

    private void OnTrayShowRequested(object? sender, EventArgs e) => ShowWindow();

    private void OnTrayTimerActionRequested(object? sender, EventArgs e) => ToggleTimer();

    private void OnTrayRestartRequested(object? sender, EventArgs e)
    {
        if (CanRestart())
        {
            Restart();
        }
    }

    private void OnTrayResetCycleRequested(object? sender, EventArgs e) => ResetCycle();

    private void OnTrayExitRequested(object? sender, EventArgs e) => _ = ExitCoreAsync();

    private async void OnWindowCloseRequested(object? sender, EventArgs e)
    {
        if (_isHandlingClose)
        {
            return;
        }

        _isHandlingClose = true;
        try
        {
            var decision = WindowClosePolicy.Decide(_isExiting, _settings.MinimizeToTrayOnClose);
            switch (decision)
            {
                case WindowCloseDecision.HideToTray:
                    HideToTray();
                    break;
                case WindowCloseDecision.Exit:
                    await ExitCoreAsync();
                    break;
                case WindowCloseDecision.AskUser:
                    var choice = CloseChoiceRequested is null
                        ? WindowCloseChoice.MinimizeToTray
                        : await CloseChoiceRequested.Invoke();
                    await ApplyCloseChoiceAsync(choice);
                    break;
            }
        }
        finally
        {
            _isHandlingClose = false;
        }
    }

    private async Task ApplyCloseChoiceAsync(WindowCloseChoice choice)
    {
        switch (choice)
        {
            case WindowCloseChoice.MinimizeToTray:
                HideToTray();
                break;
            case WindowCloseChoice.Exit:
                await ExitCoreAsync();
                break;
        }
    }

    private async Task ExitCoreAsync()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        PhaseSoundPlayer.Dispose();
        _trayIcon.Dispose();
        _ticker.Stop();
        await _windowService.CloseForExitAsync();
    }

    private void ApplyState(PomodoroSession state)
    {
        CurrentPhaseText = PomodoroPresentation.CurrentPhaseText(state.CurrentPhase);
        TimeRemainingText = PomodoroPresentation.TimeRemainingText(state.RemainingTime);
        CycleText = PomodoroPresentation.CycleText(state.CurrentCycle, _settings.CyclesBeforeLongBreak);
        ProgressPercentage = PomodoroPresentation.ProgressPercentage(
            state.RemainingTime,
            state.TotalPhaseDuration);
        ApplyPhaseAccent(state.CurrentPhase);

        IsStartEnabled = !state.IsRunning && !state.IsPaused;
        IsPauseEnabled = state.IsRunning;
        IsResumeEnabled = state.IsPaused;
        IsRestartEnabled = state.IsRunning || state.IsPaused;
        IsSkipEnabled = state.IsRunning || state.IsPaused;
        PrimaryActionGlyph = PomodoroPresentation.PrimaryActionGlyph(state.IsRunning, state.IsPaused);
        PrimaryActionTooltip = TrayPresentation.TimerActionText(state.IsRunning, state.IsPaused);
    }

    private void ApplyPhaseAccent(PomodoroPhase phase)
    {
        var (r, g, b) = PomodoroPresentation.AccentRgb(phase);
        if (r == _accentR && g == _accentG && b == _accentB)
        {
            return;
        }

        _accentR = r;
        _accentG = g;
        _accentB = b;
        PhaseAccentBrush = new SolidColorBrush(Color.FromArgb(255, r, g, b));
    }
}
