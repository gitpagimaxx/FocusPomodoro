using FocusPomodoro.Models;

namespace FocusPomodoro.ViewModels;

public static class PomodoroPresentation
{
    public const string PlayGlyph = "\uE768";
    public const string PauseGlyph = "\uE769";

    public static double ProgressPercentage(TimeSpan remainingTime, TimeSpan totalPhaseDuration)
    {
        if (totalPhaseDuration <= TimeSpan.Zero)
        {
            return 0;
        }

        var elapsed = totalPhaseDuration - remainingTime;
        var progress = elapsed / totalPhaseDuration * 100;
        return Math.Clamp(progress, 0, 100);
    }

    public static string CurrentPhaseText(PomodoroPhase phase) => phase switch
    {
        PomodoroPhase.Focus => "Foco",
        PomodoroPhase.ShortBreak => "Pausa curta",
        PomodoroPhase.LongBreak => "Pausa longa",
        _ => phase.ToString()
    };

    public static string CycleText(int currentCycle, int cyclesBeforeLongBreak) =>
        $"Ciclo {currentCycle} de {cyclesBeforeLongBreak}";

    public static string TimeRemainingText(TimeSpan remainingTime) =>
        $"{(int)remainingTime.TotalMinutes:00}:{remainingTime.Seconds:00}";

    public static string PrimaryActionGlyph(bool isRunning, bool isPaused) =>
        isRunning && !isPaused ? PauseGlyph : PlayGlyph;

    public static string AccentHex(PomodoroPhase phase) => phase switch
    {
        PomodoroPhase.ShortBreak => "#34D399",
        PomodoroPhase.LongBreak => "#818CF8",
        _ => "#E85D4C"
    };

    public static (byte R, byte G, byte B) AccentRgb(PomodoroPhase phase)
    {
        var hex = AccentHex(phase);
        return (
            Convert.ToByte(hex[1..3], 16),
            Convert.ToByte(hex[3..5], 16),
            Convert.ToByte(hex[5..7], 16));
    }
}
