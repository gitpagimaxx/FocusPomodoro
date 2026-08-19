using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusPomodoro.Models;
using FocusPomodoro.Services;

namespace FocusPomodoro.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IPomodoroTimerService _timer;

    public SettingsViewModel(ISettingsService settingsService, IPomodoroTimerService timer)
    {
        _settingsService = settingsService;
        _timer = timer;
        CopyFrom(_settingsService.Current);
    }

    public event EventHandler? CloseRequested;

    [ObservableProperty]
    public partial int FocusDurationMinutes { get; set; }

    [ObservableProperty]
    public partial int ShortBreakDurationMinutes { get; set; }

    [ObservableProperty]
    public partial int LongBreakDurationMinutes { get; set; }

    [ObservableProperty]
    public partial int CyclesBeforeLongBreak { get; set; }

    [ObservableProperty]
    public partial bool AutoStartNextPhase { get; set; }

    [ObservableProperty]
    public partial bool AlwaysOnTop { get; set; }

    [ObservableProperty]
    public partial bool SoundEnabled { get; set; }

    [ObservableProperty]
    public partial bool NotificationsEnabled { get; set; }

    [ObservableProperty]
    public partial AppTheme AppTheme { get; set; }

    [ObservableProperty]
    public partial bool MinimizeToTrayOnClose { get; set; }

    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; }

    partial void OnIsDarkThemeChanged(bool value)
    {
        AppTheme = value ? AppTheme.Dark : AppTheme.Light;
    }

    partial void OnAppThemeChanged(AppTheme value)
    {
        var isDark = value == AppTheme.Dark;
        if (IsDarkTheme != isDark)
        {
            IsDarkTheme = isDark;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ApplyEditsTo(_settingsService.Current);
        await _settingsService.SaveAsync();
        _timer.ApplySettings();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void RestoreDefaults() => CopyFrom(PomodoroSettings.CreateDefault());

    private void CopyFrom(PomodoroSettings source)
    {
        FocusDurationMinutes = source.FocusDurationMinutes;
        ShortBreakDurationMinutes = source.ShortBreakDurationMinutes;
        LongBreakDurationMinutes = source.LongBreakDurationMinutes;
        CyclesBeforeLongBreak = source.CyclesBeforeLongBreak;
        AutoStartNextPhase = source.AutoStartNextPhase;
        AlwaysOnTop = source.AlwaysOnTop;
        SoundEnabled = source.SoundEnabled;
        NotificationsEnabled = source.NotificationsEnabled;
        AppTheme = source.AppTheme;
        MinimizeToTrayOnClose = source.MinimizeToTrayOnClose;
        IsDarkTheme = source.AppTheme == AppTheme.Dark;
    }

    private void ApplyEditsTo(PomodoroSettings target)
    {
        target.FocusDurationMinutes = Math.Clamp(FocusDurationMinutes, 1, 180);
        target.ShortBreakDurationMinutes = Math.Clamp(ShortBreakDurationMinutes, 1, 60);
        target.LongBreakDurationMinutes = Math.Clamp(LongBreakDurationMinutes, 1, 60);
        target.CyclesBeforeLongBreak = Math.Clamp(CyclesBeforeLongBreak, 1, 20);
        target.AutoStartNextPhase = AutoStartNextPhase;
        target.AlwaysOnTop = AlwaysOnTop;
        target.SoundEnabled = SoundEnabled;
        target.NotificationsEnabled = NotificationsEnabled;
        target.AppTheme = AppTheme;
        target.MinimizeToTrayOnClose = MinimizeToTrayOnClose;
    }
}
