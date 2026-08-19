namespace FocusPomodoro.Models;

public sealed class PomodoroSettings
{
    public int FocusDurationMinutes { get; set; } = 25;
    public int ShortBreakDurationMinutes { get; set; } = 5;
    public int LongBreakDurationMinutes { get; set; } = 15;
    public int CyclesBeforeLongBreak { get; set; } = 4;
    public bool AutoStartNextPhase { get; set; } = true;
    public bool AlwaysOnTop { get; set; }
    public bool SoundEnabled { get; set; } = true;
    public bool NotificationsEnabled { get; set; } = true;
    public AppTheme AppTheme { get; set; } = AppTheme.Dark;
    public int WindowPositionX { get; set; } = -1;
    public int WindowPositionY { get; set; } = -1;
    public int WindowWidth { get; set; } = 260;
    public int WindowHeight { get; set; } = 150;
    public bool MinimizeToTrayOnClose { get; set; }

    public static PomodoroSettings CreateDefault() => new();

    public PomodoroSettings Clone()
    {
        var clone = new PomodoroSettings();
        clone.CopyFrom(this);
        return clone;
    }

    public void CopyFrom(PomodoroSettings source)
    {
        ArgumentNullException.ThrowIfNull(source);

        FocusDurationMinutes = source.FocusDurationMinutes;
        ShortBreakDurationMinutes = source.ShortBreakDurationMinutes;
        LongBreakDurationMinutes = source.LongBreakDurationMinutes;
        CyclesBeforeLongBreak = source.CyclesBeforeLongBreak;
        AutoStartNextPhase = source.AutoStartNextPhase;
        AlwaysOnTop = source.AlwaysOnTop;
        SoundEnabled = source.SoundEnabled;
        NotificationsEnabled = source.NotificationsEnabled;
        AppTheme = source.AppTheme;
        WindowPositionX = source.WindowPositionX;
        WindowPositionY = source.WindowPositionY;
        WindowWidth = source.WindowWidth;
        WindowHeight = source.WindowHeight;
        MinimizeToTrayOnClose = source.MinimizeToTrayOnClose;
    }

    public void ResetToDefaults() => CopyFrom(CreateDefault());
}
