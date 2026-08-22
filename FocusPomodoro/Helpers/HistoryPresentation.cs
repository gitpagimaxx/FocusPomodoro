using FocusPomodoro.Models;
using FocusPomodoro.ViewModels;

namespace FocusPomodoro.Helpers;

public static class HistoryPresentation
{
    public static string OutcomeText(PhaseOutcome outcome) => outcome switch
    {
        PhaseOutcome.InProgress => "Em andamento",
        PhaseOutcome.Completed => "Concluída",
        PhaseOutcome.Skipped => "Pulada",
        PhaseOutcome.Interrupted => "Interrompida",
        _ => outcome.ToString()
    };

    public static string ContinuePrompt(TimeSpan remaining) =>
        $"Continuar {PomodoroPresentation.TimeRemainingText(remaining)} restantes?";

    public static string Line(PhaseLog log, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(timeZone);

        var local = TimeZoneInfo.ConvertTime(log.StartedAt, timeZone);
        var duration = log.Outcome == PhaseOutcome.InProgress && log.Elapsed <= TimeSpan.Zero
            ? log.PlannedDuration
            : log.Elapsed;

        return $"{local:HH:mm}  {PomodoroPresentation.CurrentPhaseText(log.Phase)}  {PomodoroPresentation.TimeRemainingText(duration)}  {OutcomeText(log.Outcome)}";
    }

    public static string DayHeader(DailyHistoryGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return $"{group.Date:dd/MM} · {group.CompletedFocusCount} focos · {PomodoroPresentation.TimeRemainingText(group.CompletedFocusElapsed)}";
    }
}
